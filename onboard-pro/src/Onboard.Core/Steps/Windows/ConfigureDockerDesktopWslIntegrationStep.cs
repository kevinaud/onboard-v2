namespace Onboard.Core.Steps.Windows;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Aligns Docker Desktop's WSL integration with the selected distribution and restarts Docker when changes are applied.
/// </summary>
public class ConfigureDockerDesktopWslIntegrationStep : IOnboardingStep
{
    private const string RestartCommand = "-NoProfile -Command \"Start-Process -FilePath 'Docker Desktop' -Verb RunAs -ArgumentList '--shutdown'\"";

    private static string? GetAppDataPath()
    {
        string? appData = Environment.GetEnvironmentVariable("APPDATA");
        if (!string.IsNullOrWhiteSpace(appData))
        {
            return appData;
        }

        appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData) ? null : appData;
    }

    private static bool HasIntegratedDistro(JsonObject settings, string distro)
    {
        if (settings["IntegratedWslDistros"] is not JsonArray array)
        {
            return false;
        }

        foreach (JsonNode? node in array)
        {
            if (node is JsonValue value && string.Equals(value.ToString(), distro, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EnsureIntegratedDistro(JsonObject settings, string distro)
    {
        JsonArray array;
        if (settings["IntegratedWslDistros"] is JsonArray existing)
        {
            array = existing;
        }
        else
        {
            array = new JsonArray();
            settings["IntegratedWslDistros"] = array;
        }

        foreach (JsonNode? node in array)
        {
            if (node is JsonValue value && string.Equals(value.ToString(), distro, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        array.Add(distro);
        return true;
    }

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private readonly IFileSystem fileSystem;
    private readonly OnboardingConfiguration configuration;
    private readonly Func<string?> appDataProvider;

    private SettingsState? cachedState;

    public ConfigureDockerDesktopWslIntegrationStep(
        IProcessRunner processRunner,
        IUserInteraction userInteraction,
        IFileSystem fileSystem,
        OnboardingConfiguration configuration,
        Func<string?>? appDataProvider = null)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.fileSystem = fileSystem;
        this.configuration = configuration;
        this.appDataProvider = appDataProvider ?? GetAppDataPath;
    }

    public string Description => "Configure Docker Desktop WSL integration";

    public Task<bool> ShouldExecuteAsync()
    {
        cachedState = LoadSettings();
        return Task.FromResult(!cachedState.IsConfigured || cachedState.Status != SettingsStatus.Ready);
    }

    public async Task ExecuteAsync()
    {
        cachedState ??= LoadSettings();

        if (cachedState.Status == SettingsStatus.MissingAppData)
        {
            userInteraction.WriteWarning("Unable to locate the AppData folder. Configure Docker Desktop's WSL integration manually.");
            return;
        }

        if (cachedState.Status == SettingsStatus.ParseFailure)
        {
            userInteraction.WriteWarning($"Failed to parse Docker Desktop settings: {cachedState.FailureMessage}. Review the file manually before rerunning onboarding.");
            return;
        }

        if (cachedState.TargetDistro is null)
        {
            userInteraction.WriteWarning("No WSL distribution was detected. Run the WSL prerequisite step before configuring Docker Desktop.");
            return;
        }

        bool updated = EnsureIntegratedDistro(cachedState.Settings, cachedState.TargetDistro);
        if (!updated)
        {
            userInteraction.WriteSuccess("Docker Desktop already integrates with the selected WSL distribution.");
            return;
        }

        PersistSettings(cachedState);

        userInteraction.WriteNormal("Restarting Docker Desktop to apply updated WSL integration...");
        var restartResult = await processRunner.RunAsync("powershell", RestartCommand, requestElevation: false, useShellExecute: true).ConfigureAwait(false);
        if (!restartResult.IsSuccess)
        {
            userInteraction.WriteWarning("Docker Desktop restart was requested but may not have completed. Restart Docker Desktop manually if required.");
            return;
        }

        userInteraction.WriteSuccess("Docker Desktop WSL integration updated.");
    }

    private SettingsState LoadSettings()
    {
        string? appData = appDataProvider();
        string targetDistro = configuration.ActiveWslDistroName ?? configuration.WslDistroName;

        if (string.IsNullOrWhiteSpace(appData))
        {
            return SettingsState.MissingAppData(targetDistro);
        }

        string dockerDirectory = Path.Combine(appData, "Docker");
        string settingsPath = Path.Combine(dockerDirectory, "settings-store.json");

        if (!fileSystem.FileExists(settingsPath))
        {
            return SettingsState.Ready(targetDistro, dockerDirectory, settingsPath, new JsonObject(), isConfigured: false);
        }

        try
        {
            string content = fileSystem.ReadAllText(settingsPath);
            JsonObject settings = JsonNode.Parse(content)?.AsObject() ?? new JsonObject();
            bool isConfigured = HasIntegratedDistro(settings, targetDistro);
            return SettingsState.Ready(targetDistro, dockerDirectory, settingsPath, settings, isConfigured);
        }
        catch (JsonException ex)
        {
            return SettingsState.ParseFailure(targetDistro, ex.Message);
        }
    }

    private void PersistSettings(SettingsState state)
    {
        string directory = state.SettingsDirectory;
        fileSystem.CreateDirectory(directory);

        string payload = state.Settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        string tempFile = Path.Combine(directory, $"settings-store.json.{Guid.NewGuid():N}.tmp");

        try
        {
            fileSystem.WriteAllText(tempFile, payload);
            fileSystem.MoveFile(tempFile, state.SettingsPath, overwrite: true);
        }
        finally
        {
            fileSystem.DeleteFile(tempFile);
        }
    }

    private enum SettingsStatus
    {
        Ready,
        MissingAppData,
        ParseFailure,
    }

    private sealed record SettingsState(SettingsStatus Status, string? TargetDistro, string SettingsDirectory, string SettingsPath, JsonObject Settings, bool IsConfigured, string? FailureMessage)
    {
        public static SettingsState Ready(string targetDistro, string directory, string path, JsonObject settings, bool isConfigured) =>
            new(SettingsStatus.Ready, targetDistro, directory, path, settings, isConfigured, null);

        public static SettingsState MissingAppData(string targetDistro) =>
            new(SettingsStatus.MissingAppData, targetDistro, string.Empty, string.Empty, new JsonObject(), false, null);

        public static SettingsState ParseFailure(string targetDistro, string message) =>
            new(SettingsStatus.ParseFailure, targetDistro, string.Empty, string.Empty, new JsonObject(), false, message);
    }
}
