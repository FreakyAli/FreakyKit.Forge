using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class CollectionMappingAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public void Collection_SameElementType_NoFKF200()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest   { public List<string> Tags { get; set; } = new(); }

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
    public void Collection_ListToArray_NoFKF200()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public int[] Values { get; set; } = System.Array.Empty<int>(); }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF200");
    }
}
