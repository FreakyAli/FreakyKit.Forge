using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class FlatteningTests : AnalyzerTestBase
{
    [Fact]
    public void Flattening_Enabled_SuppressesFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void Flattening_Disabled_EmitsFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void Flattening_NoMatchingNested_StillEmitsFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressZipCode { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // "AddressZipCode" can't be flattened because Address has no "ZipCode" property
        AssertContainsDiagnostic(source, "FKF100");
    }
}
