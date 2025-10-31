namespace Onboard.Core.Tests.Steps.Windows;

using System;
using System.IO;
using System.Threading.Tasks;

using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Windows;

using Moq;

[TestFixture]
public class EnsureVsCodeRemoteExtensionPackStepTests
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
    public async Task ShouldExecuteAsync_WhenExtensionAlreadyInstalled_ReturnsFalse()
    {
        string cliPath = CreateCodeCliExecutable(out Action cleanup);

        try
        {
            processRunner
                .Setup(runner => runner.RunAsync("where", "code.cmd"))
                .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
            processRunner
                .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal) && args.Contains(cliPath, StringComparison.Ordinal))))
                .ReturnsAsync(new ProcessResult(0, "ms-vscode-remote.vscode-remote-extensionpack\r\nms-dotnettools.csharp", string.Empty));

            var step = CreateStep();
            bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

            Assert.That(result, Is.False);
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
            processRunner.VerifyAll();
        }
        finally
        {
            cleanup();
        }
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenExtensionMissing_ReturnsTrue()
    {
        string cliPath = CreateCodeCliExecutable(out Action cleanup);

        try
        {
            processRunner
                .Setup(runner => runner.RunAsync("where", "code.cmd"))
                .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
            processRunner
                .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal) && args.Contains(cliPath, StringComparison.Ordinal))))
                .ReturnsAsync(new ProcessResult(0, "ms-dotnettools.csharp", string.Empty));

            var step = CreateStep();
            bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

            Assert.That(result, Is.True);
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
            processRunner.VerifyAll();
        }
        finally
        {
            cleanup();
        }
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenListExtensionsFails_ReturnsTrue()
    {
        string cliPath = CreateCodeCliExecutable(out Action cleanup);

        try
        {
            processRunner
                .Setup(runner => runner.RunAsync("where", "code.cmd"))
                .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
            processRunner
                .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--list-extensions", StringComparison.Ordinal) && args.Contains(cliPath, StringComparison.Ordinal))))
                .ReturnsAsync(new ProcessResult(1, string.Empty, "code CLI not found"));

            var step = CreateStep();
            bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

            Assert.That(result, Is.True);
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
            processRunner.VerifyAll();
        }
        finally
        {
            cleanup();
        }
    }

    [Test]
    public async Task ExecuteAsync_WhenInstallSucceeds_WritesSuccess()
    {
        string cliPath = CreateCodeCliExecutable(out Action cleanup);

        try
        {
            processRunner
                .Setup(runner => runner.RunAsync("where", "code.cmd"))
                .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
            processRunner
                .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--install-extension", StringComparison.Ordinal) && args.Contains(cliPath, StringComparison.Ordinal))))
                .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
            userInteraction.Setup(ui => ui.WriteSuccess("VS Code Remote Development extension pack installed."));

            var step = CreateStep();
            await step.ExecuteAsync().ConfigureAwait(false);

            processRunner.VerifyAll();
            userInteraction.VerifyAll();
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
        }
        finally
        {
            cleanup();
        }
    }

    [Test]
    public void ExecuteAsync_WhenInstallFails_Throws()
    {
        string cliPath = CreateCodeCliExecutable(out Action cleanup);

        try
        {
            processRunner
                .Setup(runner => runner.RunAsync("where", "code.cmd"))
                .ReturnsAsync(new ProcessResult(0, cliPath, string.Empty));
            processRunner
                .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("--install-extension", StringComparison.Ordinal) && args.Contains(cliPath, StringComparison.Ordinal))))
                .ReturnsAsync(new ProcessResult(1, string.Empty, "install failed"));

            var step = CreateStep();

            Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());
            processRunner.VerifyAll();
            Assert.That(configuration.VsCodeCliPath, Is.EqualTo(cliPath));
        }
        finally
        {
            cleanup();
        }
    }

    [Test]
    public async Task RunCodeCliAsync_WhenCliCannotBeResolved_FallsBackToCodeOnPath()
    {
        processRunner
            .Setup(runner => runner.RunAsync("where", "code.cmd"))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not found"));
        processRunner
            .Setup(runner => runner.RunAsync("cmd.exe", It.Is<string>(args => args.Contains("/c code --list-extensions", StringComparison.Ordinal))))
            .ReturnsAsync(new ProcessResult(0, "ms-dotnettools.csharp", string.Empty));

        configuration.VsCodeCliPath = null;

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        Assert.That(configuration.VsCodeCliPath, Is.Null);
        processRunner.VerifyAll();
    }

    private static string CreateCodeCliExecutable(out Action cleanup)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"code-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "code.cmd");
        File.WriteAllText(path, string.Empty);

        cleanup = () =>
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        };

        return path;
    }

    private EnsureVsCodeRemoteExtensionPackStep CreateStep()
    {
        return new EnsureVsCodeRemoteExtensionPackStep(processRunner.Object, userInteraction.Object, configuration);
    }
}
