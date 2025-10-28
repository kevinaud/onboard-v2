# Onboard Pro

Onboard Pro is a cross-platform onboarding CLI that automates common developer workstation setup tasks for Windows, macOS, Ubuntu, and WSL guests. It replaces the legacy PowerShell onboarding scripts with a testable C# architecture powered by dependency injection and platform-aware steps.

## Quick Start

### Requirements
- An internet connection and access to the latest GitHub release assets.
- `curl`, `bash`, and `python3` on macOS, Ubuntu, or WSL (needed by `setup.sh`).
- PowerShell 5.1 or newer on Windows (the bundled `setup.ps1` runs fine in Windows PowerShell or PowerShell 7).

### macOS, Ubuntu, and WSL guests
```bash
curl -fsSL https://raw.githubusercontent.com/kevinaud/onboard-v2/main/setup.sh -o setup.sh
chmod +x setup.sh
./setup.sh            # add --mode wsl-guest when running _inside_ WSL
```

The bootstrapper detects your host OS/architecture, downloads the matching single-file release asset, and executes it. To force a specific tagged release or reuse an already-downloaded binary:

- `ONBOARD_RELEASE_TAG=v0.2.0 ./setup.sh`
- `ONBOARD_KEEP_DOWNLOADED_BINARY=true ./setup.sh`
- `ONBOARD_REPOSITORY=kevinaud/onboard-v2 ./setup.sh` (default; override when testing forks)

### Windows hosts
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
Invoke-RestMethod https://raw.githubusercontent.com/kevinaud/onboard-v2/main/setup.ps1 -OutFile setup.ps1
.\setup.ps1
```

The PowerShell bootstrapper mirrors the bash script behaviour: it resolves the latest (or requested) tag, downloads `Onboard-win-x64.exe`, executes it, and cleans up the temporary binary unless `-KeepDownloadedBinary` or `ONBOARD_KEEP_DOWNLOADED_BINARY` is supplied.

> Tip: run `./setup.sh --mode wsl-guest` **inside** your WSL distribution to execute the WSL-specific workflow.

## Command-line modes

The compiled onboarding binary auto-detects the host platform. When running inside a WSL distribution you must explicitly select the guest workflow:

- `Onboard --mode wsl-guest`

Add `--dry-run` to preview the ordered steps without running any external commands. The flag stacks with other arguments, for example `Onboard --mode wsl-guest --dry-run` inside a WSL distribution.

All other platforms (Windows host, native macOS, native Ubuntu) are selected automatically.

## Diagnostics & Troubleshooting

- Every run writes a detailed transcript to `Path.GetTempPath()/onboard-pro.log`. On Windows this resolves to `%TEMP%\onboard-pro.log`; on macOS and Linux it resolves to `/tmp/onboard-pro.log`.
- The log captures all user-facing console output plus every external command invocation (command line, exit code, and the first 1024 characters of stdout/stderr) to simplify support escalation.
- Retention is deliberately simple: the most recent run overwrites the previous file. Copy the log somewhere safe before starting a new attempt if you need to keep historical output.
- When reporting issues, attach the log file together with the command-line arguments you used (for example `--mode wsl-guest --dry-run`). Dry-run mode still records the console transcript but skips external command execution, so the absence of process entries is expected in that scenario.

## Architecture overview

- `Onboard.Core` – domain logic, abstractions (e.g., `IProcessRunner`, `IUserInteraction`), immutable `PlatformFacts`, and platform-specific onboarding steps.
- `Onboard.Console` – composition root. Configures dependency injection, selects the appropriate orchestrator, and executes the ordered step pipeline.
- `Onboard.Core.Tests` – NUnit test suite with Moq-based doubles covering services, step idempotency, and orchestrator wiring.

Key concepts:
- **Platform detection** – `PlatformDetector` resolves OS, architecture, and WSL facts once per run.
- **Orchestrators** – one per platform (`WindowsOrchestrator`, `MacOsOrchestrator`, `UbuntuOrchestrator`, `WslGuestOrchestrator`) that compose the required steps.
- **Steps** – each implements `IOnboardingStep`, performs an idempotency check via `ShouldExecuteAsync`, and executes safely when required.
- **Process execution** – commands run through `IProcessRunner`, making both live runs and unit tests consistent.
- **Configuration** – `OnboardingConfiguration` centralizes host-specific constants (for example the default WSL distro name/image) and is injected into steps that need them.

## Configuration defaults

The defaults that drive host-specific behaviour live in `src/Onboard.Core/Models/OnboardingConfiguration.cs`. Today the record exposes two properties:

- `WslDistroName` – the distribution name returned by `wsl.exe -l -q`.
- `WslDistroImage` – the identifier passed to `wsl --install -d <image>`.

`Program.cs` registers a single `OnboardingConfiguration` instance in the DI container, so Windows steps such as `EnableWslFeaturesStep` and `InstallDockerDesktopStep` consume the same values. Updating the record allows you to align the onboarding workflow with a different corporate-standard WSL distribution without hunting down hard-coded strings.

## Release workflow

Tagging a commit with `v*.*.*` (for example `v0.2.0`) triggers `.github/workflows/release.yml`:
- Matrix publishes single-file, self-contained binaries for `win-x64`, `osx-x64`, `osx-arm64`, and `linux-x64`.
- Each job uploads its binary as both a workflow artifact and a release asset.
- The Windows leg also generates GitHub release notes.

You can also trigger the release workflow manually through the **Run workflow** button in GitHub Actions and optionally supply a `version` input.

## Local development

### Prerequisites
- .NET SDK 9.0
- PowerShell 7 (optional, only for running `setup.ps1` locally on non-Windows hosts)

### Common commands
```bash
dotnet restore onboard-pro/Onboard.sln
dotnet build onboard-pro/Onboard.sln
dotnet test onboard-pro/tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj
```

Run the console app directly from the repo while developing:
```bash
dotnet run --project onboard-pro/src/Onboard.Console/Onboard.Console.csproj -- --mode wsl-guest --dry-run
```

## Contributing

1. Create a feature branch from `main`.
2. Keep changes scoped to a single iteration task when possible.
3. Run `dotnet build` and `dotnet test` before opening a PR.
4. Tag releases using semantic versioning to publish new installer binaries.

## License

This project inherits the license of the repository it resides in. Review the root `LICENSE` file (or contact the maintainers) for details.
