using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Dictionary Mapping diagnostics (FKF700–FKF702).
/// Tests dictionary source/destination validation and null handling policies.
/// </summary>
public sealed class DictionaryDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF700_DictionaryKeyTypeNotString_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;

            namespace TestNs
            {
                public class Config { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Config MapFromDict(Dictionary<int, object> source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF700");
        AssertDiagnosticWithSeverity(source, "FKF700", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF701_UnsupportedDictionaryValueType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;

            namespace TestNs
            {
                public class ComplexType { }
                public class Config { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Config MapFromDict(Dictionary<string, ComplexType> source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF701");
        AssertDiagnosticWithSeverity(source, "FKF701", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF702_ReturnNullOnNonNullableType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;

            namespace TestNs
            {
                public class Config
                {
                    [ForgeDictionary(MissingKey = MissingKeyPolicy.ReturnNull)]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(MissingKey = MissingKeyPolicy.ReturnNull)]
                    public static partial Config MapFromDict(Dictionary<string, object> source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF702");
        AssertDiagnosticWithSeverity(source, "FKF702", DiagnosticSeverity.Error);
    }
}
