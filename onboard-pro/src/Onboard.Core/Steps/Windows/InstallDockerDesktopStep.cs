namespace Onboard.Core.Steps.Windows;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Installs Docker Desktop via winget and reminds the user to finish configuration.
/// </summary>
public class InstallDockerDesktopStep : IOnboardingStep
{
    private const string DetectionArguments = "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"";
    private const string WingetCommand = "install --id Docker.DockerDesktop -e --source winget";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private readonly OnboardingConfiguration configuration;

    public InstallDockerDesktopStep(
        IProcessRunner processRunner,
        IUserInteraction userInteraction,
        OnboardingConfiguration configuration)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.configuration = configuration;
    }

    public string Description => "Install Docker Desktop";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("powershell", DetectionArguments).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return true;
        }

        string output = result.StandardOutput.Trim();
        return !bool.TryParse(output, out bool isInstalled) || !isInstalled;
    }

    public async Task ExecuteAsync()
    {
        var result = await processRunner.RunAsync("winget", WingetCommand).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "winget failed to install Docker Desktop."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("Docker Desktop installed via winget.");
        bool integrationConfigured = await ConfigureDockerSettingsAsync().ConfigureAwait(false);
        userInteraction.WriteLine("Launch Docker Desktop and accept the terms of service if prompted.");

        if (integrationConfigured)
        {
            userInteraction.WriteLine($"WSL integration for {configuration.WslDistroName} has been pre-configured. Restart Docker Desktop if it was already running.");
        }
        else
        {
            userInteraction.WriteLine($"Verify WSL integration for {configuration.WslDistroName} inside Docker Desktop before continuing.");
        }
    }

    private static bool EnsureIntegratedDistro(JsonObject settings, string distroName)
    {
        JsonArray integratedDistros;

        if (settings["IntegratedWslDistros"] is JsonArray existingArray)
        {
            integratedDistros = existingArray;
        }
        else
        {
            integratedDistros = new JsonArray();
            settings["IntegratedWslDistros"] = integratedDistros;
        }

        foreach (JsonNode? node in integratedDistros)
        {
            if (node is JsonValue value && string.Equals(value.ToString(), distroName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        integratedDistros.Add(distroName);
        return true;
    }

    private async Task<bool> ConfigureDockerSettingsAsync()
    {
        string? appData = Environment.GetEnvironmentVariable("APPDATA");
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        if (string.IsNullOrWhiteSpace(appData))
        {
            userInteraction.WriteWarning("Unable to locate the AppData folder. Please ensure WSL integration is enabled manually in Docker Desktop.");
            return false;
        }

        string dockerDirectory = Path.Combine(appData, "Docker");
        string settingsPath = Path.Combine(dockerDirectory, "settings-store.json");

        JsonObject settings;

        if (File.Exists(settingsPath))
        {
            try
            {
                string existingContent = await File.ReadAllTextAsync(settingsPath).ConfigureAwait(false);
                settings = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
            }
            catch (JsonException ex)
            {
                userInteraction.WriteWarning($"Unable to parse Docker Desktop settings. Enable WSL integration manually in Docker Desktop. Details: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                userInteraction.WriteWarning($"Failed to read Docker Desktop settings: {ex.Message}. Enable WSL integration manually if needed.");
                return false;
            }
        }
        else
        {
            Directory.CreateDirectory(dockerDirectory);
            settings = new JsonObject();
        }

        bool updated = EnsureIntegratedDistro(settings, configuration.WslDistroName);
        if (!updated)
        {
            return true;
        }

        try
        {
            string json = settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsPath, json).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            userInteraction.WriteWarning($"Failed to update Docker Desktop settings: {ex.Message}. Enable WSL integration manually if needed.");
            return false;
        }

        return true;
    }
}
