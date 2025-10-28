namespace Onboard.Core.Tests.Services;

using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Onboard.Core.Models;
using Onboard.Core.Services;
using Onboard.Core.Tests.TestDoubles;

[TestFixture]
public class ConsoleUserInteractionTests
{
    private TextWriter originalOut = null!;
    private TextReader originalIn = null!;

    [SetUp]
    public void SetUp()
    {
        originalOut = Console.Out;
        originalIn = Console.In;
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetOut(originalOut);
        Console.SetIn(originalIn);
    }

    [Test]
    public void WriteLine_LogsInfoCategory()
    {
        var logger = new InMemoryLogger<ConsoleUserInteraction>();
        var interaction = new ConsoleUserInteraction(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        using var writer = new StringWriter();
        Console.SetOut(writer);

        interaction.WriteLine("hello world");

        Assert.That(logger.Entries, Has.Count.EqualTo(1));
        Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Information));
        Assert.That(logger.Entries[0].Message, Is.EqualTo("INFO: hello world"));
    }

    [Test]
    public void WriteWarning_LogsWarningLevel()
    {
        var logger = new InMemoryLogger<ConsoleUserInteraction>();
        var interaction = new ConsoleUserInteraction(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        using var writer = new StringWriter();
        Console.SetOut(writer);

        interaction.WriteWarning("careful");

        Assert.That(logger.Entries, Has.Count.EqualTo(1));
        Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
        Assert.That(logger.Entries[0].Message, Is.EqualTo("WARNING: careful"));
    }

    [Test]
    public void Prompt_LogsPromptAndResponse()
    {
        var logger = new InMemoryLogger<ConsoleUserInteraction>();
        var interaction = new ConsoleUserInteraction(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetIn(new StringReader("yes\n"));

        string response = interaction.Prompt("Proceed? ");

        Assert.That(response, Is.EqualTo("yes"));
        Assert.That(logger.Entries, Has.Count.EqualTo(2));
        Assert.That(logger.Entries[0].Message, Is.EqualTo("PROMPT: Proceed? "));
        Assert.That(logger.Entries[1].Message, Is.EqualTo("PROMPT_RESPONSE: yes"));
    }

    [Test]
    public void WriteDebug_WhenVerboseDisabled_DoesNotWriteToConsole()
    {
        var logger = new InMemoryLogger<ConsoleUserInteraction>();
        var interaction = new ConsoleUserInteraction(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        using var writer = new StringWriter();
        Console.SetOut(writer);

        interaction.WriteDebug("details");

        Assert.Multiple(() =>
        {
            Assert.That(writer.ToString(), Is.EqualTo(string.Empty));
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(logger.Entries[0].Message, Is.EqualTo("DEBUG: details"));
        });
    }

    [Test]
    public void WriteDebug_WhenVerboseEnabled_WritesDecoratedMessage()
    {
        var logger = new InMemoryLogger<ConsoleUserInteraction>();
        var interaction = new ConsoleUserInteraction(logger, new ExecutionOptions(IsDryRun: false, IsVerbose: true));

        using var writer = new StringWriter();
        Console.SetOut(writer);

        interaction.WriteDebug("verbose info");

        Assert.Multiple(() =>
        {
            Assert.That(writer.ToString(), Is.EqualTo("[DEBUG] verbose info" + Environment.NewLine));
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(logger.Entries[0].Message, Is.EqualTo("DEBUG: verbose info"));
        });
    }
}
