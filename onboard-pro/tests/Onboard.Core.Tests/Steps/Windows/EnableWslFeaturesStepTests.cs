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
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenTargetDistributionMissing_ReturnsTrue()
  {
    string listOutput = "docker-desktop\r\nUbuntu-20.04\r\n";
    processRunner
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(0, listOutput, string.Empty));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenTargetDistributionPresent_ReturnsFalse()
  {
    string listOutput = "\ufeffUbuntu-22.04\r\n";
    processRunner
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(0, listOutput, string.Empty));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.False);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenWslCannotListDistributions_ShowsInstallGuidance()
  {
    processRunner
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "WSL not installed"));

    var messages = new List<string>();
    userInteraction.Setup(ui => ui.WriteWarning(It.IsAny<string>())).Callback<string>(messages.Add);
    userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>())).Callback<string>(messages.Add);

    configuration = configuration with { WslDistroName = "ContosoLinux", WslDistroImage = "ContosoLinux" };

    var step = CreateStep();
    await step.ShouldExecuteAsync().ConfigureAwait(false);
    var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await step.ExecuteAsync().ConfigureAwait(false)
    );

    Assert.That(
      messages.Any(message => message.Contains("Manual WSL setup", StringComparison.OrdinalIgnoreCase)),
      Is.True
    );
    Assert.That(
      messages.Any(message => message.Contains("administrator", StringComparison.OrdinalIgnoreCase)),
      Is.True
    );
    Assert.That(messages.Any(message => message.Contains("ContosoLinux", StringComparison.OrdinalIgnoreCase)), Is.True);
    Assert.That(
      messages.Any(message => message.Contains("wsl --install -d ContosoLinux", StringComparison.OrdinalIgnoreCase)),
      Is.True
    );
    Assert.That(exception?.Message, Does.Contain("WSL could not enumerate distributions"));
    processRunner.VerifyAll();
  }

  [Test]
  public void ExecuteAsync_WhenNoDistributionsFound_PromptsForInstall()
  {
    processRunner
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    var warnings = new List<string>();
    var normals = new List<string>();
    userInteraction.Setup(ui => ui.WriteWarning(It.IsAny<string>())).Callback<string>(warnings.Add);
    userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>())).Callback<string>(normals.Add);

    var step = CreateStep();
    Assert.That(
      async () =>
      {
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);
      },
      Throws.InvalidOperationException.With.Message.Contains("Install 'Ubuntu-22.04'")
    );

    Assert.That(warnings.Any(), Is.True);
    Assert.That(normals.Any(message => message.Contains("wsl --install", StringComparison.OrdinalIgnoreCase)), Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public void ExecuteAsync_WhenOtherDistributionsPresent_AllowsSelectionAndSuggestsRename()
  {
    string listOutput = "docker-desktop\r\nUbuntuDev\r\n";
    processRunner
      .Setup(runner => runner.RunAsync("wsl.exe", "-l -q"))
      .ReturnsAsync(new ProcessResult(0, listOutput, string.Empty));

    var warnings = new List<string>();
    var normals = new List<string>();
    userInteraction.Setup(ui => ui.WriteWarning(It.IsAny<string>())).Callback<string>(warnings.Add);
    userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>())).Callback<string>(normals.Add);
    userInteraction.Setup(ui => ui.Ask(It.IsAny<string>(), It.IsAny<string>())).Returns("UbuntuDev");

    var step = CreateStep();

    Assert.That(
      async () =>
      {
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);
      },
      Throws.InvalidOperationException.With.Message.Contains("Rename the selected distribution")
    );

    Assert.That(
      normals.Any(message => message.Contains("wsl.exe --rename", StringComparison.OrdinalIgnoreCase)),
      Is.True
    );
    processRunner.VerifyAll();
    userInteraction.Verify(ui => ui.Ask(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
  }

  private EnableWslFeaturesStep CreateStep()
  {
    return new EnableWslFeaturesStep(processRunner.Object, userInteraction.Object, configuration);
  }
}
