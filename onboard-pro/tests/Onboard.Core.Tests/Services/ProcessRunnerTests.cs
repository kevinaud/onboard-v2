namespace Onboard.Core.Tests.Services;

using System;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging.Abstractions;

using Onboard.Core.Services;
using Onboard.Core.Tests.TestDoubles;

[TestFixture]
public class ProcessRunnerTests
{
    private string? originalDebianFrontend;

    [SetUp]
    public void SetUp()
    {
        originalDebianFrontend = Environment.GetEnvironmentVariable("DEBIAN_FRONTEND");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", originalDebianFrontend);
    }

    [Test]
    public async Task RunAsync_OnLinux_InsertsDebianFrontendWhenMissing()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Ignore("Linux-specific behaviour");
        }

        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", null);
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);

        var result = await runner.RunAsync("env", string.Empty);

        Assert.That(result.StandardOutput, Does.Contain("DEBIAN_FRONTEND=noninteractive"));
    }

    [Test]
    public async Task RunAsync_OnLinux_PreservesExistingDebianFrontend()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Ignore("Linux-specific behaviour");
        }

        Environment.SetEnvironmentVariable("DEBIAN_FRONTEND", "dialog");
        var runner = new ProcessRunner(NullLogger<ProcessRunner>.Instance);

        var result = await runner.RunAsync("env", string.Empty);

        Assert.That(result.StandardOutput, Does.Contain("DEBIAN_FRONTEND=dialog"));
    }

    [Test]
    public async Task RunAsync_LogsCommandLifecycle()
    {
        var logger = new InMemoryLogger<ProcessRunner>();
        var runner = new ProcessRunner(logger);

        var result = await runner.RunAsync("dotnet", "--version");

        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(logger.Entries, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(logger.Entries[0].Message, Does.Contain("Executing command dotnet --version"));
        Assert.That(logger.Entries[^1].Message, Does.Contain("StdOut:"));
    }
}
