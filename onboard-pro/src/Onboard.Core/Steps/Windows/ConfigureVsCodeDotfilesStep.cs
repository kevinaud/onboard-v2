namespace Onboard.Core.Steps.Windows;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Configures VS Code dotfiles settings so environments hydrate automatically.
/// </summary>
public class ConfigureVsCodeDotfilesStep : IInteractiveOnboardingStep
{
    private const string DefaultRepository = "kevinaud/dotfiles";

    private static string GetDefaultSettingsPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Code", "User", "settings.json");
    }

    private static JsonObject ParseSettings(string contents)
    {
        if (string.IsNullOrWhiteSpace(contents))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(contents)?.AsObject() ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private static bool TryGetRepository(JsonObject settings, out string? repository)
    {
        repository = null;
        if (!settings.TryGetPropertyValue("dotfiles.repository", out JsonNode? value))
        {
            return false;
        }

        repository = value?.GetValue<string?>();
        return !string.IsNullOrWhiteSpace(repository);
    }

    private readonly IUserInteraction userInteraction;
    private readonly IFileSystem fileSystem;
    private readonly Func<string> settingsPathProvider;

    private SettingsSnapshot? cachedSettings;

    public ConfigureVsCodeDotfilesStep(IUserInteraction userInteraction, IFileSystem fileSystem, Func<string>? settingsPathProvider = null)
    {
        this.userInteraction = userInteraction;
        this.fileSystem = fileSystem;
        this.settingsPathProvider = settingsPathProvider ?? GetDefaultSettingsPath;
    }

    public string Description => "Configure VS Code dotfiles repository";

    public Task<bool> ShouldExecuteAsync()
    {
        cachedSettings = LoadSettings();
        return Task.FromResult(!cachedSettings.HasRepository);
    }

    public Task ExecuteAsync()
    {
        cachedSettings ??= LoadSettings();
        var settings = cachedSettings;

        userInteraction.WriteNormal(string.Empty);
        userInteraction.WriteNormal("[bold yellow]VS Code dotfiles configuration needs your input.[/]");
        userInteraction.WriteNormal(string.Empty);
        userInteraction.WriteNormal("[bold]What this does[/]");
        userInteraction.WriteNormal("- VS Code Dev Containers can automatically clone a dotfiles repository whenever a container starts.");
        userInteraction.WriteNormal("- The setting only applies inside Dev Container environments.");
        userInteraction.WriteNormal("- Learn more: https://code.visualstudio.com/docs/devcontainers/containers#_personalizing-with-dotfile-repositories");
        userInteraction.WriteNormal(string.Empty);
        userInteraction.WriteNormal($"[bold]Default repository[/]: [cyan]{DefaultRepository}[/]");
        userInteraction.WriteNormal("[bold]Choose an option[/]:");
        userInteraction.WriteNormal("- [bold cyan]CUSTOM[/]: Provide a repository and optional target path.");
        userInteraction.WriteNormal("- [bold green]DEFAULT[/]: Use the default repository above.");
        userInteraction.WriteNormal("- [bold gray]SKIP[/]: Leave dotfiles unconfigured for now.");

        string option = PromptForOption();
        if (string.Equals(option, "SKIP", StringComparison.OrdinalIgnoreCase))
        {
            userInteraction.WriteWarning("Skipping VS Code dotfiles configuration at user request.");
            return Task.CompletedTask;
        }

        string repository;
        string? targetPath = null;

        if (string.Equals(option, "CUSTOM", StringComparison.OrdinalIgnoreCase))
        {
            repository = PromptForRepository();
            targetPath = PromptForTargetPath();
        }
        else
        {
            repository = DefaultRepository;
        }

        UpdateSettings(settings!, repository, targetPath);
        WriteSettings(settings!);

        userInteraction.WriteSuccess("VS Code dotfiles configuration updated.");
        return Task.CompletedTask;
    }

    private SettingsSnapshot LoadSettings()
    {
        string path = settingsPathProvider();
        JsonObject settingsObject;
        bool hasRepository = false;

        if (fileSystem.FileExists(path))
        {
            string contents = fileSystem.ReadAllText(path);
            settingsObject = ParseSettings(contents);
            hasRepository = TryGetRepository(settingsObject, out _);
        }
        else
        {
            settingsObject = new JsonObject();
        }

        return new SettingsSnapshot(path, settingsObject, hasRepository);
    }

    private string PromptForOption()
    {
        while (true)
        {
            string response = userInteraction.Ask("Configure VS Code Dev Container dotfiles (CUSTOM/DEFAULT/SKIP):", "DEFAULT");
            if (string.IsNullOrWhiteSpace(response))
            {
                return "DEFAULT";
            }

            response = response.Trim().ToUpperInvariant();
            if (response is "CUSTOM" or "DEFAULT" or "SKIP")
            {
                return response;
            }

            userInteraction.WriteWarning($"Please enter CUSTOM to supply a repository, DEFAULT to use {DefaultRepository}, or SKIP to leave dotfiles unconfigured.");
        }
    }

    private string PromptForRepository()
    {
        while (true)
        {
            string response = userInteraction.Ask("Enter the GitHub repository to clone (owner/repo):");
            if (!string.IsNullOrWhiteSpace(response))
            {
                return response.Trim();
            }

            userInteraction.WriteWarning("Repository cannot be empty.");
        }
    }

    private string? PromptForTargetPath()
    {
        string response = userInteraction.Ask("Optional: enter a target path for dotfiles (leave blank for default):", string.Empty);
        return string.IsNullOrWhiteSpace(response) ? null : response.Trim();
    }

    private void UpdateSettings(SettingsSnapshot snapshot, string repository, string? targetPath)
    {
        snapshot.Settings["dotfiles.repository"] = repository;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            snapshot.Settings.Remove("dotfiles.targetPath");
        }
        else
        {
            snapshot.Settings["dotfiles.targetPath"] = targetPath;
        }
    }

    private void WriteSettings(SettingsSnapshot snapshot)
    {
        string directory = Path.GetDirectoryName(snapshot.Path)!;
        fileSystem.CreateDirectory(directory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        string payload = snapshot.Settings.ToJsonString(options);
        fileSystem.WriteAllText(snapshot.Path, payload);
    }

    private sealed record SettingsSnapshot(string Path, JsonObject Settings, bool HasRepository);
}
