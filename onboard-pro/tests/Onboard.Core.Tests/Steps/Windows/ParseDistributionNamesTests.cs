namespace Onboard.Core.Tests.Steps.Windows;

using System.Linq;

using NUnit.Framework;

using Onboard.Core.Steps.Windows;

[TestFixture]
public class ParseDistributionNamesTests
{
    [Test]
    public void ParseDistributionNames_WithRealWindowsOutput_ExtractsOnlyDistributionName()
    {
        // This is the EXACT output from the user's Windows 11 sandbox
        string realOutput = "  NAME            STATE           VERSION\r\n* Ubuntu-22.04    Stopped         2\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(realOutput);

        Assert.That(result, Has.Count.EqualTo(1), $"Expected 1 distribution, got {result.Count}. Distributions: [{string.Join(", ", result.Select(r => $"\"{r}\""))}]");
        Assert.That(result.First(), Is.EqualTo("Ubuntu-22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithBomAndDefaultMarker_ExtractsDistributionName()
    {
        string output = "\ufeff  NAME            STATE           VERSION\r\n* Ubuntu-22.04 (Default)    Stopped         2\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First(), Is.EqualTo("Ubuntu-22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithMultipleDistributions_ExtractsAll()
    {
        string output = "  NAME            STATE           VERSION\r\n* Ubuntu-22.04    Stopped         2\r\n  docker-desktop    Running         2\r\n  Ubuntu-20.04    Stopped         2\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Does.Contain("Ubuntu-22.04"));
        Assert.That(result, Does.Contain("docker-desktop"));
        Assert.That(result, Does.Contain("Ubuntu-20.04"));
    }

    [Test]
    public void ParseDistributionNames_WithSpacesInName_ExtractsFullName()
    {
        string output = "  NAME            STATE           VERSION\r\n* Dev Ubuntu 22.04    Running         2\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First(), Is.EqualTo("Dev Ubuntu 22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithOnlyHeader_ReturnsEmpty()
    {
        string output = "  NAME            STATE           VERSION\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Is.Empty);
    }
}
