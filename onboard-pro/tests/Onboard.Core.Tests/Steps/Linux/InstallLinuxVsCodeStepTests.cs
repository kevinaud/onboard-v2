namespace Onboard.Core.Tests.Steps.Linux;

using System;
using System.Threading.Tasks;
using Moq;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Linux;

[TestFixture]
public class InstallLinuxVsCodeStepTests
{
  private Mock<IProcessRunner> processRunner = null!;
  private Mock<IUserInteraction> userInteraction = null!;

  [SetUp]
  public void SetUp()
  {
    processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
    userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenCodeCliExists_ReturnsFalse()
  {
    processRunner
      .Setup(runner => runner.RunAsync("which", "code"))
      .ReturnsAsync(new ProcessResult(0, "/usr/bin/code", string.Empty));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.False);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenCodeCliMissing_ReturnsTrue()
  {
    processRunner
      .Setup(runner => runner.RunAsync("which", "code"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenCommandsSucceed_InvokesDownloadInstallAndCleanup()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "curl",
          "-L \"https://update.code.visualstudio.com/latest/linux-deb-x64/stable\" -o \"/tmp/vscode.deb\""
        )
      )
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
    processRunner
      .Setup(runner => runner.RunAsync("sudo", "apt-get install -y \"/tmp/vscode.deb\""))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
    processRunner
      .Setup(runner => runner.RunAsync("rm", "-f \"/tmp/vscode.deb\""))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
    userInteraction.Setup(ui => ui.WriteSuccess("Visual Studio Code installed via apt."));

    var step = CreateStep();
    await step.ExecuteAsync().ConfigureAwait(false);

    processRunner.VerifyAll();
    userInteraction.VerifyAll();
  }

  [Test]
  public void ExecuteAsync_WhenDownloadFails_ThrowsInvalidOperationException()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "curl",
          "-L \"https://update.code.visualstudio.com/latest/linux-deb-x64/stable\" -o \"/tmp/vscode.deb\""
        )
      )
      .ReturnsAsync(new ProcessResult(1, string.Empty, "curl error"));

    var step = CreateStep();

    Assert.That(
      async () => await step.ExecuteAsync().ConfigureAwait(false),
      Throws.TypeOf<InvalidOperationException>()
    );
    processRunner.VerifyAll();
  }

  [Test]
  public void ExecuteAsync_WhenInstallFails_ThrowsAndPerformsCleanup()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "curl",
          "-L \"https://update.code.visualstudio.com/latest/linux-deb-x64/stable\" -o \"/tmp/vscode.deb\""
        )
      )
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
    processRunner
      .Setup(runner => runner.RunAsync("sudo", "apt-get install -y \"/tmp/vscode.deb\""))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "apt error"));
    processRunner
      .Setup(runner => runner.RunAsync("rm", "-f \"/tmp/vscode.deb\""))
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    var step = CreateStep();

    Assert.That(
      async () => await step.ExecuteAsync().ConfigureAwait(false),
      Throws.TypeOf<InvalidOperationException>()
    );
    processRunner.VerifyAll();
  }

  private InstallLinuxVsCodeStep CreateStep()
  {
    return new InstallLinuxVsCodeStep(processRunner.Object, userInteraction.Object);
  }
}
