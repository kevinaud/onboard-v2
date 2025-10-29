namespace Onboard.Console.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

using Spectre.Console;

/// <summary>
/// Spectre.Console-based implementation of <see cref="IUserInteraction"/>.
/// </summary>
public sealed class SpectreUserInteraction : IUserInteraction
{
    private static string EscapeMarkup(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : Markup.Escape(value);
    }

    private readonly IAnsiConsole console;
    private readonly ILogger<SpectreUserInteraction> logger;
    private readonly ExecutionOptions executionOptions;

    public SpectreUserInteraction(IAnsiConsole console, ILogger<SpectreUserInteraction> logger, ExecutionOptions executionOptions)
    {
        this.console = console ?? throw new ArgumentNullException(nameof(console));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.executionOptions = executionOptions ?? throw new ArgumentNullException(nameof(executionOptions));
    }

    public void WriteNormal(string message)
    {
        this.LogTranscript(LogLevel.Information, "INFO", message);

        if (string.IsNullOrEmpty(message))
        {
            this.console.WriteLine();
            return;
        }

        this.console.MarkupLine(EscapeMarkup(message));
    }

    public void WriteSuccess(string message)
    {
        this.LogTranscript(LogLevel.Information, "SUCCESS", message);
        this.console.MarkupLine($"[green]✓ {EscapeMarkup(message)}[/]");
    }

    public void WriteWarning(string message)
    {
        this.LogTranscript(LogLevel.Warning, "WARNING", message);
        this.console.MarkupLine($"[yellow]⚠ {EscapeMarkup(message)}[/]");
    }

    public void WriteError(string message)
    {
        this.LogTranscript(LogLevel.Error, "ERROR", message);
        this.console.MarkupLine($"[red]✗ {EscapeMarkup(message)}[/]");
    }

    public void WriteDebug(string message)
    {
        this.LogTranscript(LogLevel.Debug, "DEBUG", message);

        if (!this.executionOptions.IsVerbose)
        {
            return;
        }

        this.console.MarkupLine($"[grey][[DEBUG]] {EscapeMarkup(message)}[/]");
    }

    public void ShowWelcomeBanner(PlatformFacts platformFacts)
    {
        if (platformFacts is null)
        {
            throw new ArgumentNullException(nameof(platformFacts));
        }

        this.LogTranscript(LogLevel.Information, "BANNER", $"Launching onboarding for {platformFacts.OS} ({platformFacts.Arch})");

        var rule = new Rule($"[bold cyan]Onboard Pro[/] — {EscapeMarkup(platformFacts.OS.ToString())} ({EscapeMarkup(platformFacts.Arch.ToString())})");

        this.console.WriteLine();
        this.console.Write(rule);
        this.console.WriteLine();
    }

    public void ShowSummary(IReadOnlyCollection<StepResult> results)
    {
        if (results is null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        this.LogTranscript(LogLevel.Information, "SUMMARY", $"Rendering summary for {results.Count} step(s).");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Step[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Details[/]");

        foreach (var result in results)
        {
            string statusMarkup = result.Status switch
            {
                StepStatus.Executed => "[green]Executed[/]",
                StepStatus.Skipped => "[yellow]Skipped[/]",
                StepStatus.Failed => "[red]Failed[/]",
                _ => EscapeMarkup(result.Status.ToString()),
            };

            string details = result.Status switch
            {
                StepStatus.Executed => string.Empty,
                StepStatus.Skipped => EscapeMarkup(result.SkipReason ?? string.Empty),
                StepStatus.Failed => EscapeMarkup(result.Exception?.Message ?? string.Empty),
                _ => string.Empty,
            };

            this.LogTranscript(LogLevel.Information, "SUMMARY_ITEM", $"{result.StepName}: {result.Status}");
            table.AddRow(EscapeMarkup(result.StepName), statusMarkup, details);
        }

        this.console.WriteLine();
        this.console.Write(table);
        this.console.WriteLine();
    }

    public Task RunStatusAsync(string statusMessage, Func<IStatusContext, Task> action, CancellationToken cancellationToken = default)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        this.LogTranscript(LogLevel.Information, "STATUS", statusMessage);

        return this.console
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(statusMessage, ctx => action(new SpectreStatusContext(this, ctx, cancellationToken)));
    }

    public string Ask(string prompt, string? defaultValue = null)
    {
        if (prompt is null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        this.LogTranscript(LogLevel.Information, "PROMPT", prompt);

        var textPrompt = new TextPrompt<string>(EscapeMarkup(prompt))
            .AllowEmpty();

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt = textPrompt.DefaultValue(defaultValue);
        }

        string response = this.console.Prompt(textPrompt);

        if (string.IsNullOrEmpty(response) && !string.IsNullOrEmpty(defaultValue))
        {
            response = defaultValue;
        }

        this.LogTranscript(LogLevel.Information, "PROMPT_RESPONSE", response);
        return response;
    }

    public bool Confirm(string prompt, bool defaultValue = false)
    {
        if (prompt is null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        this.LogTranscript(LogLevel.Information, "PROMPT", prompt);

        var confirmationPrompt = new ConfirmationPrompt(EscapeMarkup(prompt))
        {
            DefaultValue = defaultValue,
        };

        bool result = this.console.Prompt(confirmationPrompt);
        this.LogTranscript(LogLevel.Information, "PROMPT_RESPONSE", result ? "Yes" : "No");
        return result;
    }

    private void LogTranscript(LogLevel level, string category, string message)
    {
        this.logger.Log(level, "{Category}: {Message}", category, message);
    }

    private sealed class SpectreStatusContext : IStatusContext
    {
        private readonly SpectreUserInteraction interaction;
        private readonly StatusContext context;
        private readonly CancellationToken cancellationToken;

        public SpectreStatusContext(SpectreUserInteraction interaction, StatusContext context, CancellationToken cancellationToken)
        {
            this.interaction = interaction;
            this.context = context;
            this.cancellationToken = cancellationToken;
        }

        public void UpdateStatus(string status)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.LogTranscript(LogLevel.Information, "STATUS_UPDATE", status);
            this.context.Status(status);
        }

        public void WriteNormal(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteNormal(message);
            this.context.Refresh();
        }

        public void WriteSuccess(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteSuccess(message);
            this.context.Refresh();
        }

        public void WriteWarning(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteWarning(message);
            this.context.Refresh();
        }

        public void WriteError(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteError(message);
            this.context.Refresh();
        }

        public void WriteDebug(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteDebug(message);
            this.context.Refresh();
        }
    }
}
