using D3Fragments.Analyzers.CodeFixes;
using D3Fragments.Analyzers.Rules;
using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.TypeSafeEnumAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class TypeSafeEnumAnalyzerTests
{
    private const string Usings = """
        using System;
        using D3Fragments;

        """;

    [Fact]
    public async Task NonPrivateConstructor_ReportsDfr0006WithMessage()
    {
        const string source = Usings + """
            public class UserType : TypeSafeEnum<UserType, int>
            {
                public static readonly UserType Normal = new(0, "Normal");

                protected {|#0:UserType|}(int key, string name) : base(key, name) { }
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0006)
            .WithLocation(0)
            .WithArguments("UserType.UserType(int, string)", "UserType"));
    }

    [Fact]
    public async Task Constructors_ReportsOnlyNonPrivate()
    {
        const string source = Usings + """
            public sealed class UserType : TypeSafeEnum<UserType, int>
            {
                public static readonly UserType Normal = new(0, "Normal");

                static UserType() { }
                public {|DFR0006:UserType|}(int key) : base(key) { }
                internal {|DFR0006:UserType|}(int key, string name, int order) : base(key, name) { }
                private UserType(int key, string name) : base(key, name) { }
            }

            // プライマリ コンストラクターは常に public
            public sealed class {|DFR0006:Color|}(string key) : TypeSafeEnum<Color, string>(key)
            {
                public static readonly Color Red = new("red");
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task NestedDerivedClasses_MustBePrivate()
    {
        const string source = Usings + """
            public abstract class Shape : TypeSafeEnum<Shape, string>
            {
                public static readonly Shape Circle = new CircleShape();
                public static readonly Shape Square = new SquareShape();

                private Shape(string key) : base(key) { }

                private sealed class CircleShape() : Shape("circle") { }
                public sealed class {|DFR0006:SquareShape|}() : Shape("square") { }
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task GenericIntermediateBase_IsNotTypeSafeEnumType()
    {
        const string source = Usings + """
            public abstract class OrderedEnum<T> : TypeSafeEnum<T, int> where T : OrderedEnum<T>
            {
                protected OrderedEnum(int key) : base(key) { }
            }

            public sealed class Priority : OrderedEnum<Priority>
            {
                public static readonly Priority High = new(1);

                private Priority(int key) : base(key) { }
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task ValuesNotDefinedAsStaticReadonlyFields_ReportsDfr0008()
    {
        const string source = Usings + """
            public sealed class UserType : TypeSafeEnum<UserType, int>
            {
                public static readonly UserType Normal = new(0, "Normal");
                public static UserType Admin => {|#0:new(1, "Admin")|};
                public static UserType Guest { get; } = {|DFR0008:new(2, "Guest")|};
                public static UserType {|DFR0008:Owner|} = new(3, "Owner");
                public static UserType? {|DFR0008:Current|};
                public static readonly UserType Default = Normal;

                private UserType(int key, string name) : base(key, name) { }

                public static UserType Create(int key) => {|DFR0008:new UserType(key, key.ToString())|};
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0008).WithLocation(0).WithArguments("UserType"));
    }

    [Fact]
    public async Task NestedDerivedValueInStaticReadonlyField_ReportsNothing()
    {
        const string source = Usings + """
            public abstract class Shape : TypeSafeEnum<Shape, string>
            {
                public static readonly Shape Circle = new CircleShape();

                private Shape(string key) : base(key) { }

                private sealed class CircleShape() : Shape("circle") { }
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task CodeFix_MakesConstructorsPrivate()
    {
        const string source = Usings + """
            public class UserType : TypeSafeEnum<UserType, int>
            {
                public static readonly UserType Normal = new(0, "Normal");

                /// <summary>Doc.</summary>
                protected {|DFR0006:UserType|}(int key, string name) : base(key, name) { }

                public {|DFR0006:UserType|}(int key) : base(key) { }
            }
            """;
        const string fixedSource = Usings + """
            public class UserType : TypeSafeEnum<UserType, int>
            {
                public static readonly UserType Normal = new(0, "Normal");

                /// <summary>Doc.</summary>
                private UserType(int key, string name) : base(key, name) { }

                private UserType(int key) : base(key) { }
            }
            """;

        await CodeFixVerifier<TypeSafeEnumAnalyzer, TypeSafeEnumConstructorCodeFixProvider>.VerifyAsync(source, fixedSource);
    }
}
