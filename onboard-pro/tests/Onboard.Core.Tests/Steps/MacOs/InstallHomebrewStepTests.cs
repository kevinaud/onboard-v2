namespace Onboard.Core.Tests.Steps.MacOs;

using Moq;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.MacOs;

[TestFixture]
public class InstallHomebrewStepTests
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
  public async Task ShouldExecuteAsync_WhenHomebrewIsInstalled_ReturnsFalse()
  {
    processRunner
      .Setup(runner => runner.RunAsync("which", "brew"))
      .ReturnsAsync(new ProcessResult(0, "/opt/homebrew/bin/brew", string.Empty));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.False);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenHomebrewMissing_ReturnsTrue()
  {
    processRunner
      .Setup(runner => runner.RunAsync("which", "brew"))
      .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));

    var step = CreateStep();
    bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(result, Is.True);
    processRunner.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenInstallerSucceeds_WritesSuccess()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "/bin/bash",
          "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
        )
      )
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    userInteraction.Setup(ui => ui.WriteSuccess("Homebrew installed."));

    var step = CreateStep();
    await step.ExecuteAsync().ConfigureAwait(false);

    processRunner.VerifyAll();
    userInteraction.VerifyAll();
  }

  [Test]
  public void ExecuteAsync_WhenInstallerFails_Throws()
  {
    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "/bin/bash",
          "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
        )
      )
      .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

    var step = CreateStep();
    Assert.That(
      async () => await step.ExecuteAsync().ConfigureAwait(false),
      Throws.TypeOf<InvalidOperationException>()
    );
    processRunner.VerifyAll();
  }

  private InstallHomebrewStep CreateStep()
  {
    return new InstallHomebrewStep(processRunner.Object, userInteraction.Object);
  }
}
