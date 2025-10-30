// <copyright file="ConfigureGitUserStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Shared;

using Onboard.Core.Abstractions;

/// <summary>
/// Configures global Git user identity (name and email).
/// </summary>
public class ConfigureGitUserStep : IOnboardingStep, IInteractiveOnboardingStep
{
    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction ui;

    public string Description => "Configure Git user identity";

    public ConfigureGitUserStep(IProcessRunner processRunner, IUserInteraction ui)
    {
        this.processRunner = processRunner;
        this.ui = ui;
    }

    public async Task<bool> ShouldExecuteAsync()
    {
        // Check if git config user.name is set
        var nameResult = await processRunner.RunAsync("git", "config --global user.name").ConfigureAwait(false);
        var emailResult = await processRunner.RunAsync("git", "config --global user.email").ConfigureAwait(false);

        // Need to execute if either name or email is not configured
        return !nameResult.IsSuccess || string.IsNullOrWhiteSpace(nameResult.StandardOutput) ||
               !emailResult.IsSuccess || string.IsNullOrWhiteSpace(emailResult.StandardOutput);
    }

    public async Task ExecuteAsync()
    {
        this.ui.WriteNormal(string.Empty);
        this.ui.WriteNormal("Git requires a user identity for commits.");

        string name = this.ui.Ask("Please enter your full name for Git commits:");
        while (string.IsNullOrWhiteSpace(name))
        {
            this.ui.WriteWarning("Name cannot be empty.");
            name = this.ui.Ask("Please enter your full name for Git commits:");
        }

        string email = this.ui.Ask("Please enter your email for Git commits:");
        while (string.IsNullOrWhiteSpace(email))
        {
            this.ui.WriteWarning("Email cannot be empty.");
            email = this.ui.Ask("Please enter your email for Git commits:");
        }

        var nameSetResult = await processRunner.RunAsync("git", $"config --global user.name \"{name}\"").ConfigureAwait(false);
        if (!nameSetResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set git user.name: {nameSetResult.StandardError}");
        }

        var emailSetResult = await processRunner.RunAsync("git", $"config --global user.email \"{email}\"").ConfigureAwait(false);
        if (!emailSetResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to set git user.email: {emailSetResult.StandardError}");
        }

        this.ui.WriteSuccess($"Git user configured as '{name} <{email}>'.");
    }
}
