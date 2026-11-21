using System.Globalization;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace D3Fragments.EFCore;

// D3DbContextを継承しないDbContextで、保存時にUpdatedAt・Versionを更新する。UseD3Fragmentsが登録する
internal sealed class D3SaveChangesInterceptor : SaveChangesInterceptor
{
    public static readonly D3SaveChangesInterceptor Instance = new();

    private D3SaveChangesInterceptor()
    {
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MarkModifiedEntitiesAsUpdated(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MarkModifiedEntitiesAsUpdated(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void MarkModifiedEntitiesAsUpdated(DbContext? context)
    {
        // D3DbContextは自身で更新するため、二重に更新しないよう対象外とする
        if (context is null or D3DbContext)
        {
            return;
        }

        if (!D3FragmentsModelConfigurationBuilderExtensions.AreConventionsApplied(context.Model))
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.CurrentCulture,
                ExceptionMessages.InvalidOperationException_ConventionsNotApplied,
                context.GetType()));
        }

        AggregateUpdateTracker.MarkModifiedEntitiesAsUpdated(context);
    }
}
