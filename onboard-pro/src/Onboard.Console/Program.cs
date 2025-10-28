// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Onboard.Console.Orchestrators;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Services;
using Onboard.Core.Steps.MacOs;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Ubuntu;
using Onboard.Core.Steps.Windows;
using Onboard.Core.Steps.WslGuest;

using OS = Onboard.Core.Models.OperatingSystem;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Parse the supported command-line options
        if (!CommandLineOptionsParser.TryParse(args, out var commandLineOptions, out string? parseError))
        {
            new ConsoleUserInteraction().WriteError(parseError ?? "Invalid command-line arguments.");
            return;
        }

        var executionOptions = new ExecutionOptions(commandLineOptions.IsDryRun);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register all singleton services
                services.AddSingleton<IProcessRunner, ProcessRunner>();
                services.AddSingleton<IUserInteraction, ConsoleUserInteraction>();
                services.AddSingleton<IPlatformDetector, PlatformDetector>();
                services.AddSingleton<IFileSystem, FileSystem>();
                services.AddSingleton(executionOptions);

                // Register PlatformFacts by invoking the detector once at startup
                services.AddSingleton(provider =>
                    provider.GetRequiredService<IPlatformDetector>().Detect());

                // Register all Orchestrators
                services.AddTransient<WindowsOrchestrator>();
                services.AddTransient<MacOsOrchestrator>();
                services.AddTransient<UbuntuOrchestrator>();
                services.AddTransient<WslGuestOrchestrator>();

                // Register all Onboarding Steps
                // Shared
                services.AddTransient<ConfigureGitUserStep>();
                services.AddTransient<CloneProjectRepoStep>();

                // Platform-Aware
                services.AddTransient<InstallVsCodeStep>();

                // Windows-Specific
                services.AddTransient<EnableWslFeaturesStep>();
                services.AddTransient<InstallGitForWindowsStep>();
                services.AddTransient<InstallDockerDesktopStep>();

                // macOS
                services.AddTransient<InstallHomebrewStep>();
                services.AddTransient<InstallBrewPackagesStep>();

                // Linux
                services.AddTransient<AptUpdateStep>();
                services.AddTransient<InstallAptPackagesStep>();

                // WSL Guest
                services.AddTransient<InstallWslPrerequisitesStep>();
                services.AddTransient<ConfigureWslGitCredentialHelperStep>();
            })
            .Build();

        // 2. Select the correct orchestrator based on platform AND mode
        var platformFacts = host.Services.GetRequiredService<PlatformFacts>();
        var ui = host.Services.GetRequiredService<IUserInteraction>();

        IPlatformOrchestrator orchestrator;

        try
        {
            if (platformFacts.OS == OS.Windows)
            {
                orchestrator = host.Services.GetRequiredService<WindowsOrchestrator>();
            }
            else if (platformFacts.OS == OS.Linux && platformFacts.IsWsl && commandLineOptions.IsWslGuestMode)
            {
                orchestrator = host.Services.GetRequiredService<WslGuestOrchestrator>();
            }
            else if (platformFacts.OS == OS.Linux && !platformFacts.IsWsl)
            {
                orchestrator = host.Services.GetRequiredService<UbuntuOrchestrator>();
            }
            else if (platformFacts.OS == OS.MacOs)
            {
                orchestrator = host.Services.GetRequiredService<MacOsOrchestrator>();
            }
            else
            {
                throw new NotSupportedException($"Unsupported platform: {platformFacts.OS}, WSL: {platformFacts.IsWsl}");
            }
        }
        catch (Exception ex)
        {
            ui.WriteError($"Failed to initialize orchestrator: {ex.Message}");
            return;
        }

        // 3. Execute the chosen orchestration
        try
        {
            await orchestrator.ExecuteAsync().ConfigureAwait(false);
        }
        catch (OnboardingStepException ex)
        {
            ui.WriteError(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            ui.WriteError($"Unexpected error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
