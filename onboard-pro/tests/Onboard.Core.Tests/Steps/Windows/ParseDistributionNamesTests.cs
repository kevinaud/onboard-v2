namespace Onboard.Core.Tests.Steps.Windows;

using System.Linq;

using NUnit.Framework;

using Onboard.Core.Steps.Windows;

[TestFixture]
public class ParseDistributionNamesTests
{
    [Test]
    public void ParseDistributionNames_WithSimpleQuietOutput_ExtractsDistributionName()
    {
        // Output from wsl -l -q (simple list, one name per line)
        string output = "Ubuntu-22.04\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First(), Is.EqualTo("Ubuntu-22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithBom_ExtractsDistributionName()
    {
        string output = "\ufeffUbuntu-22.04\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First(), Is.EqualTo("Ubuntu-22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithMultipleDistributions_ExtractsAll()
    {
        string output = "Ubuntu-22.04\r\ndocker-desktop\r\nUbuntu-20.04\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Does.Contain("Ubuntu-22.04"));
        Assert.That(result, Does.Contain("docker-desktop"));
        Assert.That(result, Does.Contain("Ubuntu-20.04"));
    }

    [Test]
    public void ParseDistributionNames_WithSpacesInName_ExtractsFullName()
    {
        string output = "Dev Ubuntu 22.04\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First(), Is.EqualTo("Dev Ubuntu 22.04"));
    }

    [Test]
    public void ParseDistributionNames_WithEmptyOutput_ReturnsEmpty()
    {
        string output = "\r\n";

        var result = EnableWslFeaturesStep.ParseDistributionNamesForTesting(output);

        Assert.That(result, Is.Empty);
    }
}
