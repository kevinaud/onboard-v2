namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.Threading.Tasks;

using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;

using Moq;

[TestFixture]
public class EnsureVsCodeRemoteExtensionPackStepTests
{
    private const string CodeCliPath = @"C:\VSCode\code.cmd";

    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;

    [SetUp]
    public void SetUp()
    {
        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenExtensionAlreadyInstalled_ReturnsFalse()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, CodeCliPath, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, "ms-vscode-remote.vscode-remote-extensionpack\r\nms-dotnettools.csharp", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenExtensionMissing_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, CodeCliPath, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, "ms-dotnettools.csharp", string.Empty));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenListExtensionsFails_ReturnsTrue()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, CodeCliPath, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "code CLI not found"));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenInstallSucceeds_WritesSuccess()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, CodeCliPath, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--install-extension", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess("VS Code Remote Development extension pack installed."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenInstallFails_Throws()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(0, CodeCliPath, string.Empty));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--install-extension", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "install failed"));

        var step = CreateStep();

        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
        processRunner.VerifyAll();
    }

    private EnsureVsCodeRemoteExtensionPackStep CreateStep()
    {
        return new EnsureVsCodeRemoteExtensionPackStep(processRunner.Object, userInteraction.Object);
    }
}
