namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;
using Moq;

[TestFixture]
public class ConfigureDockerDesktopWslIntegrationStepTests
{
  private const string AppDataPath = "C:/AppData";
  private const string SettingsDirectory = "C:/AppData/Docker";
  private const string SettingsPath = "C:/AppData/Docker/settings-store.json";
  private const string TargetDistro = "Ubuntu-22.04";

  private Mock<IProcessRunner> processRunner = null!;
  private Mock<IUserInteraction> userInteraction = null!;
  private Mock<IFileSystem> fileSystem = null!;
  private OnboardingConfiguration configuration = null!;

  [SetUp]
  public void SetUp()
  {
    processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
    userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
    fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
    configuration = new OnboardingConfiguration { ActiveWslDistroName = TargetDistro };
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenDistroAlreadyIntegrated_ReturnsFalse()
  {
    var settings = new JsonObject { ["IntegratedWslDistros"] = new JsonArray(TargetDistro) };

    fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(true);
    fileSystem.Setup(fs => fs.ReadAllText(SettingsPath)).Returns(settings.ToJsonString());

    var step = CreateStep();
    bool shouldRun = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(shouldRun, Is.False);
    fileSystem.VerifyAll();
  }

  [Test]
  public async Task ShouldExecuteAsync_WhenSettingsMissing_ReturnsTrue()
  {
    fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);

    var step = CreateStep();
    bool shouldRun = await step.ShouldExecuteAsync().ConfigureAwait(false);

    Assert.That(shouldRun, Is.True);
    fileSystem.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenIntegrationAdded_WritesSettingsAndRestarts()
  {
    fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);
    fileSystem.Setup(fs => fs.CreateDirectory(SettingsDirectory));
    fileSystem
      .Setup(fs =>
        fs.WriteAllText(It.Is<string>(p => p.StartsWith(SettingsPath, StringComparison.Ordinal)), It.IsAny<string>())
      )
      .Callback<string, string>(
        (_, content) =>
        {
          using var document = JsonDocument.Parse(content);
          var distros = document.RootElement.GetProperty("IntegratedWslDistros");
          Assert.That(distros.GetArrayLength(), Is.EqualTo(1));
          Assert.That(distros[0].GetString(), Is.EqualTo(TargetDistro));
        }
      );
    fileSystem.Setup(fs => fs.MoveFile(It.IsAny<string>(), SettingsPath, true));
    fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()));

    userInteraction.Setup(ui => ui.WriteNormal("Restarting Docker Desktop to apply updated WSL integration..."));
    userInteraction.Setup(ui => ui.WriteSuccess("Docker Desktop WSL integration updated."));

    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "powershell",
          "-NoProfile -Command \"Start-Process -FilePath 'Docker Desktop' -Verb RunAs -ArgumentList '--shutdown'\"",
          false,
          true
        )
      )
      .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));

    var step = CreateStep();
    await step.ShouldExecuteAsync().ConfigureAwait(false);
    await step.ExecuteAsync().ConfigureAwait(false);

    processRunner.VerifyAll();
    userInteraction.VerifyAll();
    fileSystem.Verify(fs => fs.MoveFile(It.IsAny<string>(), SettingsPath, true), Times.Once);
    fileSystem.Verify(fs => fs.DeleteFile(It.IsAny<string>()), Times.Once);
  }

  [Test]
  public async Task ExecuteAsync_WhenParseFails_WarnsAndSkips()
  {
    fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(true);
    fileSystem.Setup(fs => fs.ReadAllText(SettingsPath)).Throws(new JsonException("invalid"));

    userInteraction.Setup(ui =>
      ui.WriteWarning(It.Is<string>(message => message.Contains("parse", StringComparison.OrdinalIgnoreCase)))
    );

    var step = CreateStep();
    await step.ShouldExecuteAsync().ConfigureAwait(false);
    await step.ExecuteAsync().ConfigureAwait(false);

    userInteraction.VerifyAll();
    fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    processRunner.Verify(
      runner => runner.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
      Times.Never
    );
  }

  [Test]
  public async Task ExecuteAsync_WhenAppDataMissing_Warns()
  {
    var step = new ConfigureDockerDesktopWslIntegrationStep(
      processRunner.Object,
      userInteraction.Object,
      fileSystem.Object,
      configuration,
      () => null
    );

    userInteraction.Setup(ui =>
      ui.WriteWarning("Unable to locate the AppData folder. Configure Docker Desktop's WSL integration manually.")
    );

    await step.ShouldExecuteAsync().ConfigureAwait(false);
    await step.ExecuteAsync().ConfigureAwait(false);

    userInteraction.VerifyAll();
  }

  [Test]
  public async Task ExecuteAsync_WhenRestartFails_Warns()
  {
    fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);
    fileSystem.Setup(fs => fs.CreateDirectory(SettingsDirectory));
    fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
    fileSystem.Setup(fs => fs.MoveFile(It.IsAny<string>(), SettingsPath, true));
    fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()));

    userInteraction.Setup(ui => ui.WriteNormal("Restarting Docker Desktop to apply updated WSL integration..."));
    userInteraction.Setup(ui =>
      ui.WriteWarning(
        "Docker Desktop restart was requested but may not have completed. Restart Docker Desktop manually if required."
      )
    );

    processRunner
      .Setup(runner =>
        runner.RunAsync(
          "powershell",
          "-NoProfile -Command \"Start-Process -FilePath 'Docker Desktop' -Verb RunAs -ArgumentList '--shutdown'\"",
          false,
          true
        )
      )
      .ReturnsAsync(new ProcessResult(1, string.Empty, "error"));

    var step = CreateStep();
    await step.ShouldExecuteAsync().ConfigureAwait(false);
    await step.ExecuteAsync().ConfigureAwait(false);

    userInteraction.VerifyAll();
  }

  private ConfigureDockerDesktopWslIntegrationStep CreateStep()
  {
    return new ConfigureDockerDesktopWslIntegrationStep(
      processRunner.Object,
      userInteraction.Object,
      fileSystem.Object,
      configuration,
      () => AppDataPath
    );
  }
}
