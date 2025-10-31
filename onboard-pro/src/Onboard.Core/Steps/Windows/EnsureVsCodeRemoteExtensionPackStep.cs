namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Ensures Visual Studio Code has the Remote Development extension pack installed.
/// </summary>
public class EnsureVsCodeRemoteExtensionPackStep : IOnboardingStep
{
    private const string ExtensionId = "ms-vscode-remote.vscode-remote-extensionpack";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private string? codeCliPath;
    private bool codeCliPathResolved;

    public EnsureVsCodeRemoteExtensionPackStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install VS Code Remote Development extension pack";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await RunCodeCliAsync("--list-extensions").ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return true;
        }

        foreach (string line in EnumerateLines(result.StandardOutput))
        {
            if (string.Equals(line.Trim(), ExtensionId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public async Task ExecuteAsync()
    {
        var result = await RunCodeCliAsync($"--install-extension {ExtensionId}").ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "Failed to install VS Code Remote Development extension pack via the code CLI."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("VS Code Remote Development extension pack installed.");
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.IndexOf(' ', StringComparison.Ordinal) >= 0 ? $"\"{value}\"" : value;
    }

    private static IEnumerable<string> EnumerateLines(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            yield break;
        }

        int start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char character = input[i];
            if (character == '\r' || character == '\n')
            {
                if (i > start)
                {
                    yield return input[start..i];
                }

                if (character == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
        }

        if (start < input.Length)
        {
            yield return input[start..];
        }
    }

    private async Task<ProcessResult> RunCodeCliAsync(string arguments)
    {
        string? cliPath = await ResolveCodeCliPathAsync().ConfigureAwait(false);
        string executable = cliPath is null ? "code" : QuoteIfNeeded(cliPath);
        string invocationArguments = string.IsNullOrWhiteSpace(arguments) ? string.Empty : $" {arguments}";
        string command = $"/c {executable}{invocationArguments}";

        return await processRunner.RunAsync("cmd.exe", command).ConfigureAwait(false);
    }

    private async Task<string?> ResolveCodeCliPathAsync()
    {
        if (codeCliPathResolved)
        {
            return codeCliPath;
        }

        codeCliPathResolved = true;
        var result = await processRunner.RunAsync("where", "code.cmd").ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return null;
        }

        foreach (string line in EnumerateLines(result.StandardOutput))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                codeCliPath = line.Trim();
                break;
            }
        }

        return codeCliPath;
    }
}
