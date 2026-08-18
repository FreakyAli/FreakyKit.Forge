using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Nested & Collections diagnostics (FKF300–FKF315).
/// Tests nested forging, circular references, collection mapping, and reference sharing.
/// </summary>
public sealed class NestedCollectionsDiagnosticsTests : DiagnosticsTestBase
{
    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF300_NestedForgeDisabled_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address { }
                public class AddressDto { }

                public class Source { public Address Address { get; set; } = new(); }
                public class Dest { public AddressDto Address { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    public static partial AddressDto MapAddress(Address source);

                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF300");
        AssertDiagnosticWithSeverity(source, "FKF300", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF301_CircularNestedForge_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public Address Address { get; set; } = new(); }
                public class Address { public Person Contact { get; set; } = new(); }

                public class PersonDto { public AddressDto Address { get; set; } = new(); }
                public class AddressDto { public PersonDto Contact { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto MapPerson(Person source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto MapAddress(Address source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF301");
        AssertDiagnosticWithSeverity(source, "FKF301", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF310_CollectionMapping_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;

            namespace TestNs
            {
                public class Item { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                public class Source { public List<Item> Items { get; set; } = new(); }
                public class Dest { public List<ItemDto> Items { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    public static partial ItemDto MapItem(Item source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF310");
        AssertDiagnosticWithSeverity(source, "FKF310", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF311_CollectionReferenceShared_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;

            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest { public List<string> Tags { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(ShareReference = ForgePolicy.True)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF311");
        AssertDiagnosticWithSeverity(source, "FKF311", DiagnosticSeverity.Info);
    }

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF314_NullFallbackOnValueType_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public struct Address { public string City { get; set; } }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public Address Home { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF314");
        AssertDiagnosticWithSeverity(source, "FKF314", DiagnosticSeverity.Warning);
    }

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF315_IgnoreIfNullAndNullFallback_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address { }
                public class Dest
                {
                    [ForgeMap("Home", IgnoreIfNull = true, NullFallback = NullFallback.DefaultConstruct)]
                    public Address Home { get; set; } = new();
                }

                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF315");
        AssertDiagnosticWithSeverity(source, "FKF315", DiagnosticSeverity.Error);
    }
}
