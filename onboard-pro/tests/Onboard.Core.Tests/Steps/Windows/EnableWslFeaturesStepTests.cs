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
    public async Task ShouldExecuteAsync_WhenAnyFeatureDisabled_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", true))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "Feature not enabled"));
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.Verify(runner => runner.RunAsync("dism.exe", It.IsAny<string>(), true), Times.Exactly(2));
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenUbuntuDistributionMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -q", false))
            .ReturnsAsync(new ProcessResult(0, "Ubuntu\r\n", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.Verify(runner => runner.RunAsync("dism.exe", It.IsAny<string>(), true), Times.Exactly(2));
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenWslReady_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("wsl.exe", "-l -q", false))
            .ReturnsAsync(new ProcessResult(0, $"{configuration.WslDistroName}\r\n", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.Verify(runner => runner.RunAsync("dism.exe", It.IsAny<string>(), true), Times.Exactly(2));
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenWslNotReady_PrintsGuidance()
    {
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", true))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));
        processRunner
            .Setup(runner => runner.RunAsync("dism.exe", "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", true))
            .ReturnsAsync(new ProcessResult(0, "State : Enabled", string.Empty));

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
        Assert.That(messages.Any(message => message.Contains("Microsoft-Windows-Subsystem-Linux", StringComparison.OrdinalIgnoreCase)), Is.True);
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
