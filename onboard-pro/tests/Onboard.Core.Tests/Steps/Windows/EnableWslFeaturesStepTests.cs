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
    public async Task ShouldExecuteAsync_WhenWslStatusFails_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "--status"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenUbuntuDistributionMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "--status"))
            .ReturnsAsync(new ProcessResult(0, "Default State", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
            .ReturnsAsync(new ProcessResult(0, "Ubuntu\r\n", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenWslReady_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "--status"))
            .ReturnsAsync(new ProcessResult(0, "WSL is installed", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
            .ReturnsAsync(new ProcessResult(0, $"{configuration.WslDistroName}\r\n", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWslNotReady_PrintsGuidance()
    {
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "--status"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

        var messages = new List<string>();
        userInteraction.Setup(ui => ui.WriteHeader(It.IsAny<string>())).Callback<string>(messages.Add);
        userInteraction.Setup(ui => ui.WriteWarning(It.IsAny<string>())).Callback<string>(messages.Add);
        userInteraction.Setup(ui => ui.WriteLine(It.IsAny<string>())).Callback<string>(messages.Add);

        configuration = configuration with { WslDistroName = "ContosoLinux", WslDistroImage = "ContosoLinux" };

        var step = CreateStep();
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);

        Assert.That(messages.Any(message => message.Contains("Manual WSL setup", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(messages.Any(message => message.Contains("administrator", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(messages.Any(message => message.Contains("ContosoLinux", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(messages.Any(message => message.Contains("wsl --install -d ContosoLinux", StringComparison.OrdinalIgnoreCase)), Is.True);
        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    private EnableWslFeaturesStep CreateStep()
    {
        return new EnableWslFeaturesStep(processRunner.Object, userInteraction.Object, configuration);
    }
}
