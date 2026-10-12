using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.SelfTypeArgumentAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class SelfTypeArgumentAnalyzerTests
{
    private const string Usings = """
        using System;
        using D3Fragments;

        """;

    [Fact]
    public async Task TypeSafeEnumWithOtherSelfType_ReportsDiagnosticWithMessage()
    {
        const string source = Usings + """
            public sealed class UserType : TypeSafeEnum<UserType, int>
            {
                private UserType(int key) : base(key) { }
            }

            public sealed class {|#0:AdminType|} : TypeSafeEnum<UserType, int>
            {
                private AdminType(int key) : base(key) { }
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0007)
            .WithLocation(0)
            .WithArguments("AdminType", "TypeSafeEnum<TSelf, TKey>", "UserType"));
    }

    [Fact]
    public async Task ValueObjectWithOtherSelfType_ReportsDiagnostic()
    {
        const string source = Usings + """
            public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
            {
                private Email(string value) : base(value) { }
                public static Email Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }

            public sealed class {|DFR0007:Phone|} : ValueObjectBase<Email, string>
            {
                private Phone(string value) : base(value) { }
            }

            public sealed record {|DFR0007:Code|}(string Value) : IValueObject<Email, string>
            {
                public static Email Create(string value) => Email.Create(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task SelfOrTypeParameter_ReportsNothing()
    {
        const string source = Usings + """
            public abstract class OrderedEnum<T> : TypeSafeEnum<T, int> where T : OrderedEnum<T>
            {
                protected OrderedEnum(int key) : base(key) { }
            }

            public sealed class Priority : OrderedEnum<Priority>
            {
                private Priority(int key) : base(key) { }
            }

            public sealed class Tagged<T> : TypeSafeEnum<Tagged<T>, int>
            {
                private Tagged(int key) : base(key) { }
            }

            public readonly record struct Code(string Value) : IValueObject<Code, string>
            {
                public static Code Create(string value) => new(value);
                public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
            }
            """;

        await Verifier.VerifyAsync(source);
    }
}
