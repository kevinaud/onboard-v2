// <copyright file="ConsoleUserInteraction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using Microsoft.Extensions.Logging;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Concrete implementation of IUserInteraction using System.Console.
/// </summary>
public class ConsoleUserInteraction : IUserInteraction
{
    private readonly ILogger<ConsoleUserInteraction> logger;
    private readonly ExecutionOptions executionOptions;

    public ConsoleUserInteraction(ILogger<ConsoleUserInteraction> logger, ExecutionOptions executionOptions)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.executionOptions = executionOptions ?? throw new ArgumentNullException(nameof(executionOptions));
    }

    public void WriteLine(string message)
    {
        this.LogTranscript(LogLevel.Information, "INFO", message);
        Console.WriteLine(message);
    }

    public void WriteHeader(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"═══ {message} ═══");
        Console.WriteLine();
        Console.ResetColor();
        this.LogTranscript(LogLevel.Information, "HEADER", message);
    }

    public void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
        this.LogTranscript(LogLevel.Information, "SUCCESS", message);
    }

    public void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
        this.LogTranscript(LogLevel.Warning, "WARNING", message);
    }

    public void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
        this.LogTranscript(LogLevel.Error, "ERROR", message);
    }

    public string Prompt(string message)
    {
        Console.Write(message);
        this.LogTranscript(LogLevel.Information, "PROMPT", message);
        string response = Console.ReadLine() ?? string.Empty;
        this.LogTranscript(LogLevel.Information, "PROMPT_RESPONSE", response);
        return response;
    }

    public void WriteDebug(string message)
    {
        this.LogTranscript(LogLevel.Debug, "DEBUG", message);

        if (!this.executionOptions.IsVerbose)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[DEBUG] {message}");
        Console.ResetColor();
    }

    private void LogTranscript(LogLevel level, string category, string message)
    {
        this.logger.Log(level, "{Category}: {Message}", category, message);
    }
}
