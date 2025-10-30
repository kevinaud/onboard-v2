namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Ensures Visual Studio Code has the Remote Development extension pack installed.
/// </summary>
public class EnsureVsCodeRemoteExtensionPackStep : IOnboardingStep
{
    private const string ExtensionId = "ms-vscode-remote.vscode-remote-extensionpack";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public EnsureVsCodeRemoteExtensionPackStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install VS Code Remote Development extension pack";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("code", "--list-extensions").ConfigureAwait(false);
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
        var result = await processRunner.RunAsync("code", $"--install-extension {ExtensionId}").ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "Failed to install VS Code Remote Development extension pack via the code CLI."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("VS Code Remote Development extension pack installed.");
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
}
