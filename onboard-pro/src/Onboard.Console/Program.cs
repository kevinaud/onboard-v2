// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Onboard.Console.Orchestrators;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Services;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Windows;

using OS = Onboard.Core.Models.OperatingSystem;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Parse for our custom mode flag
        bool isWslGuestMode = args.Contains("--mode", StringComparer.Ordinal) &&
string.Equals(args.ElementAtOrDefault(Array.IndexOf(args, "--mode") + 1), "wsl-guest", StringComparison.Ordinal);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register all singleton services
                services.AddSingleton<IProcessRunner, ProcessRunner>();
                services.AddSingleton<IUserInteraction, ConsoleUserInteraction>();
                services.AddSingleton<IPlatformDetector, PlatformDetector>();

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

                // Platform-Aware
                services.AddTransient<InstallVsCodeStep>();

                // Windows-Specific
                services.AddTransient<InstallGitForWindowsStep>();
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
            else if (platformFacts.OS == OS.Linux && platformFacts.IsWsl && isWslGuestMode)
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
        await orchestrator.ExecuteAsync().ConfigureAwait(false);
    }
}
