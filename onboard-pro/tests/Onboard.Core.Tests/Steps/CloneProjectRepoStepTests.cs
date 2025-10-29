namespace Onboard.Core.Tests.Steps;

using System.IO;

using Moq;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Shared;

[TestFixture]
public class CloneProjectRepoStepTests
{
    private const string HomeDirectory = "/home/tester";
    private const string DefaultWorkspace = HomeDirectory + "/projects";
    private const string RepositoryPath = DefaultWorkspace + "/mental-health-app-frontend";

    private Mock<IProcessRunner> processRunner = null!;
    private Mock<IUserInteraction> userInteraction = null!;
    private Mock<IFileSystem> fileSystem = null!;
    private PlatformFacts platformFacts = null!;
    private string? originalWorkspaceEnv;

    [SetUp]
    public void SetUp()
    {
        originalWorkspaceEnv = Environment.GetEnvironmentVariable("ONBOARD_WORKSPACE_DIR");
        Environment.SetEnvironmentVariable("ONBOARD_WORKSPACE_DIR", null);

        processRunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        userInteraction = new Mock<IUserInteraction>(MockBehavior.Strict);
        fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        platformFacts = new PlatformFacts(OperatingSystem.Linux, Architecture.X64, IsWsl: false, HomeDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("ONBOARD_WORKSPACE_DIR", originalWorkspaceEnv);
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenRepositoryMissing_ReturnsTrue()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(false);

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenRepositoryExistsAndIsGit_ReturnsTrue()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(RepositoryPath, ".git"))).Returns(true);

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.True);
        fileSystem.VerifyAll();
    }

    [Test]
    public async Task ShouldExecuteAsync_WhenDirectoryIsNotGitRepo_ReturnsFalseAndWarns()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(RepositoryPath, ".git"))).Returns(false);
        userInteraction.Setup(ui => ui.WriteWarning(It.Is<string>(msg => msg.Contains(RepositoryPath))));

        var step = CreateStep();
        bool result = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(result, Is.False);
        fileSystem.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenRepositoryMissing_ClonesAndReportsSuccess()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(DefaultWorkspace)).Returns(false);
        userInteraction.Setup(ui => ui.WriteNormal($"Creating workspace directory at {DefaultWorkspace}"));
        fileSystem.Setup(fs => fs.CreateDirectory(DefaultWorkspace));
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(false);
        processRunner
            .Setup(pr => pr.RunAsync("git", $"clone https://github.com/psps-mental-health-app/mental-health-app-frontend.git \"{RepositoryPath}\"", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess($"Repository cloned to {RepositoryPath}."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        fileSystem.VerifyAll();
        userInteraction.VerifyAll();
        processRunner.VerifyAll();
    }

    [Test]
    public void ExecuteAsync_WhenCloneFails_Throws()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(DefaultWorkspace)).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(false);
        processRunner
            .Setup(pr => pr.RunAsync("git", $"clone https://github.com/psps-mental-health-app/mental-health-app-frontend.git \"{RepositoryPath}\"", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "unable to access"));

        var step = CreateStep();
        Assert.That(async () => await step.ExecuteAsync().ConfigureAwait(false), Throws.TypeOf<InvalidOperationException>());

        fileSystem.VerifyAll();
        processRunner.VerifyAll();
    }

    [Test]
    public async Task ExecuteAsync_WhenRepositoryExists_PullsLatestChanges()
    {
        fileSystem.Setup(fs => fs.DirectoryExists(DefaultWorkspace)).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(RepositoryPath)).Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(Path.Combine(RepositoryPath, ".git"))).Returns(true);
        processRunner
            .Setup(pr => pr.RunAsync("git", $"-C \"{RepositoryPath}\" pull --ff-only", It.IsAny<bool>()))
            .ReturnsAsync(new ProcessResult(0, string.Empty, string.Empty));
        userInteraction.Setup(ui => ui.WriteSuccess("Repository updated to the latest changes."));

        var step = CreateStep();
        await step.ExecuteAsync().ConfigureAwait(false);

        processRunner.VerifyAll();
        userInteraction.VerifyAll();
    }

    [Test]
    public async Task ResolvePaths_WithCustomWorkspaceEnv_UsesExpandedPath()
    {
        Environment.SetEnvironmentVariable("ONBOARD_WORKSPACE_DIR", "~/custom-workspace");
        string customWorkspace = Path.Combine(HomeDirectory, "custom-workspace");
        string customRepo = Path.Combine(customWorkspace, "mental-health-app-frontend");

        fileSystem.Setup(fs => fs.DirectoryExists(customRepo)).Returns(false);

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.True);
        fileSystem.Verify(fs => fs.DirectoryExists(customRepo), Times.Once);
    }

    [Test]
    public async Task ResolvePaths_WhenWorkspaceEnvIsWindowsPath_ConvertsToWsl()
    {
        Environment.SetEnvironmentVariable("ONBOARD_WORKSPACE_DIR", "C:/Workspace");
        platformFacts = new PlatformFacts(OperatingSystem.Linux, Architecture.X64, IsWsl: true, HomeDirectory);
        string expectedWorkspace = "/mnt/c/Workspace";
        string expectedRepo = expectedWorkspace + "/mental-health-app-frontend";

        fileSystem.Setup(fs => fs.DirectoryExists(expectedRepo)).Returns(false);

        var step = CreateStep();
        bool shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);

        Assert.That(shouldExecute, Is.True);
        fileSystem.Verify(fs => fs.DirectoryExists(expectedRepo), Times.Once);
    }

    private CloneProjectRepoStep CreateStep()
    {
        return new CloneProjectRepoStep(processRunner.Object, userInteraction.Object, fileSystem.Object, platformFacts);
    }
}
