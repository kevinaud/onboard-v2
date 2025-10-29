namespace Onboard.Core.Tests.Steps.Windows;

using NUnit.Framework;

using Onboard.Core.Steps.Windows;

[TestFixture]
public class FindColumnBreakTests
{
    [Test]
    public void TryExtractDistributionName_WithRealWslLineWithAsterisk_ExtractsCorrectName()
    {
        // The EXACT line from WSL output: "* Ubuntu-22.04    Stopped         2"
        string rawLine = "* Ubuntu-22.04    Stopped         2";

        // Call TryExtractDistributionName through the public test method
        string? result = EnableWslFeaturesStep.TryExtractDistributionNameForTesting(rawLine);

        Assert.That(result, Is.EqualTo("Ubuntu-22.04"));
    }

    [Test]
    public void TryExtractDistributionName_WithRealWslLineWithoutAsterisk_ExtractsCorrectName()
    {
        // A line without the default marker: "  Ubuntu-20.04    Stopped         2"
        string rawLine = "  Ubuntu-20.04    Stopped         2";

        string? result = EnableWslFeaturesStep.TryExtractDistributionNameForTesting(rawLine);

        Assert.That(result, Is.EqualTo("Ubuntu-20.04"));
    }

    [Test]
    public void TryExtractDistributionName_WithHeaderLine_ReturnsNull()
    {
        string rawLine = "  NAME            STATE           VERSION";

        string? result = EnableWslFeaturesStep.TryExtractDistributionNameForTesting(rawLine);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryExtractDistributionName_WithNameContainingSpaces_ExtractsFullName()
    {
        // Distribution name with spaces: "* Dev Ubuntu 22    Running         2"
        // The challenge: need to find where the name ends and STATE column begins
        string rawLine = "* Dev Ubuntu 22    Running         2";

        string? result = EnableWslFeaturesStep.TryExtractDistributionNameForTesting(rawLine);

        // This should extract "Dev Ubuntu 22" (note single spaces within name, double spaces before STATE)
        Assert.That(result, Is.EqualTo("Dev Ubuntu 22"));
    }
}
