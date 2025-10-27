using Onboard.Core.Abstractions;

namespace Onboard.Core.Steps.Shared;

/// <summary>
/// Configures global Git user identity (name and email).
/// </summary>
public class ConfigureGitUserStep : IOnboardingStep
{
    private readonly IProcessRunner _processRunner;
    private readonly IUserInteraction _ui;

    public string Description => "Configure Git user identity";

    public ConfigureGitUserStep(IProcessRunner processRunner, IUserInteraction ui)
    {
        _processRunner = processRunner;
        _ui = ui;
    }

    public async Task<bool> ShouldExecuteAsync()
    {
        // Check if git config user.name is set
        var nameResult = await _processRunner.RunAsync("git", "config --global user.name");
        var emailResult = await _processRunner.RunAsync("git", "config --global user.email");

        // Need to execute if either name or email is not configured
        return !nameResult.IsSuccess || string.IsNullOrWhiteSpace(nameResult.StandardOutput) ||
               !emailResult.IsSuccess || string.IsNullOrWhiteSpace(emailResult.StandardOutput);
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteLine("");
        _ui.WriteLine("Git requires a user identity for commits.");
        
        var name = _ui.Prompt("Please enter your full name for Git commits: ");
        while (string.IsNullOrWhiteSpace(name))
        {
            _ui.WriteWarning("Name cannot be empty.");
            name = _ui.Prompt("Please enter your full name for Git commits: ");
        }

        var email = _ui.Prompt("Please enter your email for Git commits: ");
        while (string.IsNullOrWhiteSpace(email))
        {
            _ui.WriteWarning("Email cannot be empty.");
            email = _ui.Prompt("Please enter your email for Git commits: ");
        }

        var nameSetResult = await _processRunner.RunAsync("git", $"config --global user.name \"{name}\"");
        if (!nameSetResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set git user.name: {nameSetResult.StandardError}");
        }

        var emailSetResult = await _processRunner.RunAsync("git", $"config --global user.email \"{email}\"");
        if (!emailSetResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set git user.email: {emailSetResult.StandardError}");
        }

        _ui.WriteSuccess($"Git user configured as '{name} <{email}>'.");
    }
}
