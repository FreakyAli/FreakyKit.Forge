using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class NullableHandlingTests : AnalyzerTestBase
{
    [Fact]
    public void NullableInt_ToInt_NoFKF200_EmitsFKF201()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest   { public int  Value { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF200");
        AssertContainsDiagnostic(source, "FKF201");
    }

    [Fact]
    public void Int_ToNullableInt_NoFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int  Value { get; set; } }
                public class Dest   { public int? Value { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void IncompatibleTypes_StillEmitsFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int    Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void NullableInt_InConstructor_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest
                {
                    public int Value { get; }
                    public Dest(int value) { Value = value; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Nullable<int> should satisfy int constructor param
        AssertNotContainsDiagnostic(source, "FKF501");
        AssertNotContainsDiagnostic(source, "FKF502");
    }
}
