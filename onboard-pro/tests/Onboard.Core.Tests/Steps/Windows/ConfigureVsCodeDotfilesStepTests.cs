namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Steps.Windows;

using Moq;

[TestFixture]
public class ConfigureVsCodeDotfilesStepTests
{
    private const string SettingsPath = "C:/Users/Test/AppData/Roaming/Code/User/settings.json";

    private Mock<IFileSystem> fileSystem = null!;
    private Mock<IUserInteraction> userInteraction = null!;

    [SetUp]
    public void SetUp()
    {
        fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenRepositoryAlreadyConfigured_ReturnsFalse()
    {
        fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(SettingsPath)).Returns("{\"dotfiles.repository\":\"someone/dots\"}");

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.False);
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenSettingsMissing_ReturnsTrue()
    {
        fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.True);
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenUserProvidesCustomRepository_WritesSettings()
    {
        var responses = new Queue<string>(new[] { "CUSTOM", "someone/dots", "C:/custom" });

        fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);
        fileSystem.Setup(fs => fs.CreateDirectory(Path.GetDirectoryName(SettingsPath)!));
        fileSystem.Setup(fs => fs.WriteAllText(SettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) =>
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                Assert.That(root.GetProperty("dotfiles.repository").GetString(), Is.EqualTo("someone/dots"));
                Assert.That(root.GetProperty("dotfiles.targetPath").GetString(), Is.EqualTo("C:/custom"));
            });

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
        string? markdownPrompt = null;
        userInteraction.Setup(ui => ui.WriteMarkdown(It.IsAny<string>()))
            .Callback<string>(value => markdownPrompt = value);
        userInteraction.Setup(ui => ui.WriteSuccess("VS Code dotfiles configuration updated."));
        userInteraction.Setup(ui => ui.Ask(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(() => responses.Dequeue());

        var step = CreateStep();
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);

        fileSystem.Verify(fs => fs.WriteAllText(SettingsPath, It.IsAny<string>()), Times.Once);
        Assert.That(markdownPrompt, Is.Not.Null);
        Assert.That(markdownPrompt, Does.Contain("VS Code dotfiles configuration needs your input"));
        userInteraction.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenUserAcceptsDefaultRepository_WritesSettings()
    {
        var responses = new Queue<string>(new[] { "DEFAULT" });

        fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(SettingsPath)).Returns("{\"editor.fontSize\": 14}");
        fileSystem.Setup(fs => fs.CreateDirectory(Path.GetDirectoryName(SettingsPath)!));
        fileSystem.Setup(fs => fs.WriteAllText(SettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) =>
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                Assert.That(root.GetProperty("dotfiles.repository").GetString(), Is.EqualTo("kevinaud/dotfiles"));
                Assert.That(root.TryGetProperty("dotfiles.targetPath", out JsonElement _), Is.False);
                Assert.That(root.GetProperty("editor.fontSize").GetInt32(), Is.EqualTo(14));
            });

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
        userInteraction.Setup(ui => ui.WriteMarkdown(It.Is<string>(value => value.Contains("`kevinaud/dotfiles`", StringComparison.Ordinal))));
        userInteraction.Setup(ui => ui.WriteSuccess("VS Code dotfiles configuration updated."));
        userInteraction.Setup(ui => ui.Ask(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(() => responses.Dequeue());

        var step = CreateStep();
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);

        fileSystem.Verify(fs => fs.WriteAllText(SettingsPath, It.IsAny<string>()), Times.Once);
        userInteraction.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenUserSkips_DoesNotWriteFile()
    {
        var responses = new Queue<string>(new[] { "SKIP" });

        fileSystem.Setup(fs => fs.FileExists(SettingsPath)).Returns(false);

        userInteraction.Setup(ui => ui.WriteNormal(It.IsAny<string>()));
        userInteraction.Setup(ui => ui.WriteMarkdown(It.IsAny<string>()));
        userInteraction.Setup(ui => ui.WriteWarning("Skipping VS Code dotfiles configuration at user request."));
        userInteraction.Setup(ui => ui.Ask(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(() => responses.Dequeue());

        var step = CreateStep();
        await step.ShouldExecuteAsync().ConfigureAwait(false);
        await step.ExecuteAsync().ConfigureAwait(false);

        fileSystem.Verify(fs => fs.WriteAllText(SettingsPath, It.IsAny<string>()), Times.Never);
        fileSystem.Verify(fs => fs.CreateDirectory(It.IsAny<string>()), Times.Never);
        userInteraction.VerifyAll();
    }

    private ConfigureVsCodeDotfilesStep CreateStep()
    {
        return new ConfigureVsCodeDotfilesStep(userInteraction.Object, fileSystem.Object, () => SettingsPath);
    }
}
