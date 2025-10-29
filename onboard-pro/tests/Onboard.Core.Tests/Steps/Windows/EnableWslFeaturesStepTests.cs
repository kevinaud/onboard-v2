namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Windows;

[TestFixture]
public class EnableWslFeaturesStepTests
{
    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;
    private OnboardingConfiguration configuration = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
        configuration = new OnboardingConfiguration();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenWslListFails_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -v", false))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenUbuntuDistributionMissing_ReturnsTrue()
    {
        string listOutput = "  NAME            STATE           VERSION\r\n* docker-desktop   Running         2\r\n  Ubuntu-20.04     Stopped         2\r\n";
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -v", false))
            .ReturnsAsync(new ProcessResult(0, listOutput, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-d docker-desktop cat /etc/os-release", false))
            .ReturnsAsync(new ProcessResult(0, "ID=alpine\nVERSION_ID=3.17", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-d Ubuntu-20.04 cat /etc/os-release", false))
            .ReturnsAsync(new ProcessResult(0, "ID=ubuntu\nVERSION_ID=\"20.04\"", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenWslReady_ReturnsFalse()
    {
        string listOutput = "  NAME            STATE           VERSION\r\n* Ubuntu-22.04    Stopped         2\r\n";
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -v", false))
            .ReturnsAsync(new ProcessResult(0, listOutput, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-d Ubuntu-22.04 cat /etc/os-release", false))
            .ReturnsAsync(new ProcessResult(0, "ID=ubuntu\nVERSION_ID=\"22.04\"", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWslNotReady_PrintsGuidance()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -v", false))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

        var messages = new List<string>();
        userInteraction.Setup(ui => ui.WriteWarning(It.IsAny<string>())).Callback<string>(messages.Add);
        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>())).Callback<string>(messages.Add);

        configuration = configuration with { WslDistroName = "ContosoLinux", WslDistroImage = "ContosoLinux" };

        var step = CreateStep();
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await step.ExecuteAsync().ConfigureAwait(false));

        Assert.That(messages.Any(message => message.Contains("Manual WSL setup", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(messages.Any(message => message.Contains("administrator", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(messages.Any(message => message.Contains("ContosoLinux", StringComparison.OrdinalIgnoreCase)), Is.True);

        Assert.That(messages.Any(message => message.Contains("wsl --install -d ContosoLinux", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(exception?.Message, Does.Contain("WSL prerequisites are missing"));
        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    private EnableWslFeaturesStep CreateStep()
    {
        return new EnableWslFeaturesStep(processRunner.Object, userInteraction.Object, configuration);
    }
}
