using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D3Fragments.Analyzers.Rules;

// DFR0012: 保存・トランザクション操作 / DFR0013: エンティティに対する Update・Attach / DFR0014: リポジトリでの変更追跡の無効化
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PersistenceApiAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> DbContextSaveMethods = ImmutableHashSet.Create("SaveChanges", "SaveChangesAsync");

    private static readonly ImmutableHashSet<string> DatabaseTransactionMethods = ImmutableHashSet.Create(
        "BeginTransaction", "BeginTransactionAsync", "CommitTransaction", "CommitTransactionAsync", "RollbackTransaction", "RollbackTransactionAsync");

    private static readonly ImmutableHashSet<string> TransactionMethods = ImmutableHashSet.Create("Commit", "CommitAsync", "Rollback", "RollbackAsync");

    private static readonly ImmutableHashSet<string> UpdateMethods = ImmutableHashSet.Create("Update", "UpdateRange", "Attach", "AttachRange");

    private static readonly ImmutableHashSet<string> NoTrackingMethods = ImmutableHashSet.Create("AsNoTracking", "AsNoTrackingWithIdentityResolution");

    // 保存・トランザクション操作を許可するモジュールレイヤー
    private static readonly ImmutableHashSet<ModuleLayerType> PersistenceControlLayers = ImmutableHashSet.Create(
        ModuleLayerType.Application_PipelineBehavior, ModuleLayerType.Infrastructure_DbContext, ModuleLayerType.Infrastructure_UnitOfWork);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0012, DiagnosticDescriptors.DFR0013, DiagnosticDescriptors.DFR0014);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            var architectureLayers = new ArchitectureLayerResolver(knownTypes);
            if (architectureLayers.Resolve(start.Compilation.Assembly) == ArchitectureLayerType.None)
            {
                return;
            }

            var moduleLayers = new ModuleLayerResolver(knownTypes, architectureLayers);
            start.RegisterOperationAction(c => AnalyzeInvocation(c, knownTypes, moduleLayers), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes knownTypes, ModuleLayerResolver moduleLayers)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        var from = context.ContainingSymbol as INamedTypeSymbol ?? context.ContainingSymbol.ContainingType;
        if (from is null)
        {
            return;
        }

        var fromLayer = moduleLayers.Resolve(from);
        var location = invocation.Syntax.GetLocation();
        var methodName = method.ContainingType.Name + "." + method.Name;

        if (IsPersistenceControl(method, knownTypes) && (fromLayer is not { } layer || !PersistenceControlLayers.Contains(layer)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0012, location, from.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), methodName));
        }

        if (UpdateMethods.Contains(method.Name) && FindUpdatedEntity(invocation, knownTypes) is { } entity)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0013, location, methodName, entity.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }

        if (fromLayer == ModuleLayerType.Infrastructure_RepositoryImplementation && DisablesTracking(invocation, knownTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0014, location, from.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), method.Name));
        }
    }

    // 拡張メソッドは第1引数の型、それ以外は宣言している型をレシーバーの型とする
    private static ITypeSymbol ReceiverType(IMethodSymbol method) =>
        method.IsExtensionMethod && method.Parameters.Length > 0 ? method.Parameters[0].Type : method.ContainingType;

    private static bool IsPersistenceControl(IMethodSymbol method, KnownTypes knownTypes)
    {
        var receiver = ReceiverType(method);
        return (!method.IsStatic && receiver.Implements(knownTypes.IUnitOfWork))
            || (DbContextSaveMethods.Contains(method.Name) && receiver.InheritsFrom(knownTypes.DbContext))
            || (DatabaseTransactionMethods.Contains(method.Name) && receiver.InheritsFrom(knownTypes.DatabaseFacade))
            || (TransactionMethods.Contains(method.Name) && receiver.Implements(knownTypes.IDbContextTransaction));
    }

    // Update・Attach の対象がエンティティ（EntityBase<TId> の派生型）の場合、その型を返す
    private static ITypeSymbol? FindUpdatedEntity(IInvocationOperation invocation, KnownTypes knownTypes)
    {
        var method = invocation.TargetMethod;
        var receiver = ReceiverType(method);
        IEnumerable<ITypeSymbol> targets;
        if (receiver.FindBaseType(knownTypes.DbSet) is { } dbSet)
        {
            targets = [dbSet.TypeArguments[0]];
        }
        else if (receiver.InheritsFrom(knownTypes.DbContext))
        {
            // 非ジェネリックのオーバーロード（object 型の引数）は、実引数の静的な型で判定する
            targets = method.IsGenericMethod
                ? method.TypeArguments
                : invocation.Arguments.SelectMany(a => ArgumentTypes(a.Value));
        }
        else
        {
            return null;
        }

        return targets.FirstOrDefault(t => t.InheritsFrom(knownTypes.EntityBase));
    }

    private static IEnumerable<ITypeSymbol> ArgumentTypes(IOperation value)
    {
        var operand = value is IConversionOperation conversion ? conversion.Operand : value;
        if (operand is IArrayCreationOperation { Initializer: { } initializer })
        {
            return initializer.ElementValues.SelectMany(ArgumentTypes);
        }

        return operand.Type switch
        {
            IArrayTypeSymbol array => [array.ElementType],
            INamedTypeSymbol named when named.AllInterfaces.FirstOrDefault(IsEnumerableOfT) is { } enumerable => [named, enumerable.TypeArguments[0]],
            { } type => [type],
            null => [],
        };
    }

    private static bool IsEnumerableOfT(INamedTypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;

    private static bool DisablesTracking(IInvocationOperation invocation, KnownTypes knownTypes)
    {
        var method = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, knownTypes.EntityFrameworkQueryableExtensions))
        {
            return false;
        }

        if (NoTrackingMethods.Contains(method.Name))
        {
            return true;
        }

        // AsTracking(QueryTrackingBehavior) は定数で TrackAll（0）以外を指定した場合のみ対象とする
        return method.Name == "AsTracking"
            && invocation.Arguments.Any(a => a.Value.Type?.TypeKind == TypeKind.Enum && a.Value.ConstantValue is { HasValue: true, Value: not 0 });
    }
}
