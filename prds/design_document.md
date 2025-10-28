## **Design Document: Onboard Pro (Draft 2.0)**

**Version:** 2.0

**Date:** October 27, 2025

**Author:** Gemini

### **1\. Overview**

This document outlines the design for **Onboard Pro**, a command-line developer onboarding tool written in C\#. Its purpose is to automate the setup of a consistent development environment across multiple operating systems (Windows, macOS, Ubuntu) for new developers joining a project.

#### **1.1. Motivation**

The project's current onboarding process relies on a complex, monolithic PowerShell script. As the script has grown, it has become difficult to maintain, test, and extend. Migrating the core logic to C\# will provide superior structure, type safety, testability, and overall maintainability.

#### **1.2. Goals**

* **Maintainability:** Decompose all onboarding logic into small, modular, and reusable C\# classes.  
* **Testability:** All business logic must be unit-testable. This is a primary driver for the design, mandating the abstraction of all external dependencies (e.g., file system, process execution, user I/O) via interfaces.  
* **Cross-Platform Support:** The tool must support distinct onboarding paths for:  
  * Windows 11 (Host)  
  * WSL (Ubuntu 22.04 Guest)  
  * macOS (Apple Silicon & Intel)  
  * Ubuntu (Native LTS)  
* **Idempotency:** All operations must be idempotent. If a tool is already installed or a setting is already configured, the step must be skipped, and this status reported to the user.  
* **User Experience:** The initial invocation for a new developer on a Unix-based system must be a simple one-liner. The Windows experience will be a documented, multi-step process.  
* **Extensibility:** The design must make it trivial to add, remove, or modify onboarding steps without causing regressions.

#### **1.3. Non-Goals**

* **Graphical User Interface (GUI):** This is a console-only application.  
* **Parallel Execution:** Onboarding steps will be executed sequentially to ensure correctness and simplify debugging.

---

### **2\. Architecture & User Experience**

The architecture is a hybrid model that uses minimal bootstrapper scripts to download and execute a powerful, self-contained C\# console application. The C\# application's behavior is controlled by the detected platform and command-line arguments.

#### **2.1. Core Architectural Pillars**

1. **Bootstrapper Scripts (setup.ps1 / setup.sh):** Minimal, user-facing scripts hosted on GitHub. Their sole responsibility is to detect the user's OS/architecture, download the correct pre-compiled binary of the C\# application from GitHub Releases, and execute it, passing through any command-line arguments.  
2. **C\# Console Application (Onboard):** The core of the system. This self-contained executable (e.g., Onboard-win-x64.exe) contains all onboarding logic.  
3. **Dependency Injection (DI) Container:** Manages the lifecycle of services and injects dependencies, which is the foundation for testability.  
4. **Platform Detector:** A service that runs at startup to identify the host environment and produce an immutable PlatformFacts object.  
5. **Platform Orchestrators:** A set of high-level classes, one for each supported environment (e.g., WindowsOrchestrator, WslGuestOrchestrator), that define the *sequence* of steps to be run.  
6. **Onboarding Steps:** Individual classes that implement IOnboardingStep. Each class represents a single, atomic, idempotent action.

#### **2.2. User Experience Flow**

This is a critical aspect of the design, differing by platform.

##### **2.2.1. macOS & Native Ubuntu**

The user experience is a single command pasted into their terminal.

1. User opens their terminal (e.g., Terminal, iTerm, GNOME Terminal).  
2. User pastes the one-liner from the documentation:  
   Bash  
   curl \-sL 'https://.../setup.sh' | bash

3. The setup.sh script detects the OS (macos or linux) and architecture (arm64 or x64).  
4. It downloads the corresponding binary (e.g., Onboard-macos-arm64) to /tmp.  
5. It executes the binary.  
6. The C\# app starts, its PlatformDetector identifies the OS, and the DI container provides the correct orchestrator (MacOsOrchestrator or UbuntuOrchestrator).  
7. The orchestrator runs all steps sequentially, printing progress.  
8. The script cleans up the binary.

##### **2.2.2. Windows & WSL (Multi-Step Process)**

This is a documented, three-step procedure.

* **Step 1: (Manual) Install WSL**  
  1. User follows documentation to install Ubuntu 22.04.  
  2. This is typically done by running wsl \--install \-d Ubuntu-22.04 in an *Administrator PowerShell*.  
  3. User must launch the new Ubuntu instance and complete the one-time user/password setup.  
* **Step 2: (Automated) Configure Windows Host**  
  1. User opens a standard **PowerShell** terminal.  
  2. User pastes the Windows-specific one-liner from the documentation:  
     PowerShell  
     irm 'https://.../setup.ps1' | iex

  3. The setup.ps1 script downloads the *Windows* binary (Onboard-win-x64.exe) to $env:TEMP.  
  4. It executes the binary.  
  5. The C\# app starts. The PlatformDetector identifies OS \= Windows. The Program.cs logic selects the WindowsOrchestrator.  
  6. The WindowsOrchestrator runs steps to configure the *host* (e.g., install Git for Windows, install VS Code, install Docker Desktop).  
* **Step 3: (Automated) Configure WSL Guest**  
  1. User opens their **Ubuntu WSL** terminal.  
  2. User pastes the WSL-specific one-liner from the documentation:  
     Bash  
     curl \-sL 'https://.../setup.sh' | bash \-s \-- \--mode wsl-guest

  3. The setup.sh script detects OS \= linux and ARCH \= x64 (or arm64 on Windows ARM).  
  4. It downloads the *Linux* binary (e.g., Onboard-linux-x64) to /tmp.  
  5. It executes the binary, crucially passing the \--mode wsl-guest flag.  
  6. The C\# app starts. The PlatformDetector identifies OS \= Linux and IsWsl \= true.  
  7. The Program.cs logic sees the isWslGuestMode flag is true and selects the WslGuestOrchestrator.  
  8. The WslGuestOrchestrator runs steps to configure the *guest* (e.g., apt update, install build-essential, configure .bashrc).

---

### **3\. Core Implementation Details**

#### **3.1. Project Structure**

/onboard-pro/  
├── .devcontainer/  
│   └── devcontainer.json   \# Configured for the specified .NET image  
├── .github/  
│   └── workflows/  
│       └── release.yml     \# Automates dotnet publish and GH Release  
├── src/  
│   ├── Onboard.Console/    \# The main executable project (Composition Root)  
│   │   ├── Program.cs  
│   │   ├── Orchestrators/  
│   │   │   ├── WindowsOrchestrator.cs  
│   │   │   ├── MacOsOrchestrator.cs  
│   │   │   ├── UbuntuOrchestrator.cs  
│   │   │   └── WslGuestOrchestrator.cs  <-- NEW  
│   │   ├── Services/  
│   │   │   └── SpectreUserInteraction.cs  
│   │   └── Onboard.Console.csproj  
│   ├── Onboard.Core/       \# Class library (all logic, abstractions, steps)  
│   │   ├── Abstractions/  
│   │   │   ├── IOnboardingStep.cs  
│   │   │   ├── IPlatformOrchestrator.cs  
│   │   │   ├── IProcessRunner.cs  
│   │   │   ├── IUserInteraction.cs  
│   │   │   ├── IStatusContext.cs  
│   │   │   └── IPlatformDetector.cs  
│   │   ├── Models/  
│   │   │   ├── PlatformFacts.cs  
│   │   │   ├── ProcessResult.cs  
│   │   │   └── StepResult.cs  
│   │   ├── Services/  
│   │   │   ├── PlatformDetector.cs  
│   │   │   └── ProcessRunner.cs        # Concrete implementation  
│   │   ├── Steps/  
│   │   │   ├── Shared/  
│   │   │   │   ├── CloneProjectRepoStep.cs  
│   │   │   │   └── ConfigureGitUserStep.cs  
│   │   │   ├── Windows/  
│   │   │   │   ├── EnableWslFeaturesStep.cs  
│   │   │   │   ├── InstallGitForWindowsStep.cs  
│   │   │   │   ├── InstallWindowsVsCodeStep.cs  
│   │   │   │   └── InstallDockerDesktopStep.cs  
│   │   │   ├── MacOs/  
│   │   │   │   ├── InstallHomebrewStep.cs  
│   │   │   │   ├── InstallBrewPackagesStep.cs  
│   │   │   │   └── InstallMacVsCodeStep.cs  
│   │   │   ├── Linux/  
│   │   │   │   └── InstallLinuxVsCodeStep.cs  
│   │   │   ├── Ubuntu/  
│   │   │   │   └── InstallAptPackagesStep.cs  
│   │   │   └── WslGuest/  
│   │   │       ├── AptUpdateStep.cs  
│   │   │       ├── InstallWslPrerequisitesStep.cs  
│   │   │       └── ConfigureWslGitCredentialHelperStep.cs  
│   │   └── Onboard.Core.csproj  
│   └── Onboard.sln  
└── tests/  
    └── Onboard.Core.Tests/     \# Unit test project  
        ├── Steps/  
        │   └── ConfigureGitUserStepTests.cs  
        └── Onboard.Core.Tests.csproj

#### **3.2. Recommended Libraries**

* **Dependency Injection:** Microsoft.Extensions.Hosting (provides a generic host, DI container, and logging).  
* **Unit Testing:** NUnit (test framework), NUnit3TestAdapter, Microsoft.NET.Test.Sdk.  
* **Mocking:** Moq (for creating mock objects in tests).

#### **3.3. Development Environment**

Development will be done within a VS Code Dev Container. The .devcontainer/devcontainer.json file will be configured to use the mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm image. This ensures a consistent and reproducible development environment with the .NET 9 SDK pre-installed.

#### **3.4. Diagnostics & Logging**

* **Persistent log pipeline** – The console host configures Serilog to write a single rolling log file to `Path.GetTempPath()/onboard-pro.log` with a one-second flush interval. This keeps diagnostics off the user's Desktop while remaining easy to discover across platforms.
* **Command transcripts** – `ProcessRunner` receives `ILogger<ProcessRunner>` and records each external command at `Debug` level along with the exit code and the first 1024 characters of stdout/stderr. This is crucial for diagnosing package manager failures without rerunning the tool.
* **User interaction mirroring** – `SpectreUserInteraction` mirrors all user-facing output (banner, prompts, semantic status messages, summary entries) to the same logger so the log file forms a complete transcript of the session.
* **Linux reliability hardening** – When running on Linux, `ProcessRunner` injects `DEBIAN_FRONTEND=noninteractive` into the child process environment if the variable is not already defined, eliminating blocking prompts from apt-based installers.

#### **3.5. Centralized Configuration**

`OnboardingConfiguration` is a simple record in `Onboard.Core/Models` that captures shared constants used across multiple steps. The first properties added are `WslDistroName` (the normalized distro label returned by `wsl.exe -l -q`) and `WslDistroImage` (the identifier passed to `wsl --install`). `Program.cs` registers a singleton instance so Windows steps pull their inputs from one place: `EnableWslFeaturesStep` checks `WslDistroName` when determining whether onboarding can proceed, and `InstallDockerDesktopStep` writes the same value into `settings-store.json` when enabling WSL integration. Future iterations can extend this record (or load it from JSON) without hunting down scattered literals in the step implementations.

---

### **4\. Low-Level Class Design**

This section details the specific C\# classes and interfaces.

#### **4.1. External Dependency Abstractions (For Testability)**

These interfaces are the key to testability. No class (outside of their concrete implementation) should ever directly use System.Console or System.Diagnostics.Process.

* **Onboard.Core/Abstractions/IProcessRunner.cs**  
  C\#  
  public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)  
  {  
      public bool IsSuccess \=\> ExitCode \== 0;  
  }

  /// \<summary\>  
  /// An abstraction for running external processes.  
  /// \</summary\>  
  public interface IProcessRunner  
  {  
      Task\<ProcessResult\> RunAsync(string fileName, string arguments);  
  }

* **Onboard.Core/Abstractions/IUserInteraction.cs**  
  C\#  
  /// <summary>  
  /// An abstraction for all console I/O, banners, prompts, and spinner-driven status updates.  
  /// </summary>  
  public interface IUserInteraction  
  {  
      void WriteNormal(string message);  
      void WriteSuccess(string message);  
      void WriteWarning(string message);  
      void WriteError(string message);  
      void WriteDebug(string message);  
      void ShowWelcomeBanner(PlatformFacts platformFacts);  
      void ShowSummary(IReadOnlyCollection<StepResult> results);  
      Task RunStatusAsync(string statusMessage, Func<IStatusContext, Task> action, CancellationToken cancellationToken = default);  
      string Ask(string prompt, string? defaultValue = null);  
      bool Confirm(string prompt, bool defaultValue = false);  
  }

* **Onboard.Core/Abstractions/IStatusContext.cs**  
  C\#  
  /// <summary>  
  /// Represents the live status surface exposed while a spinner is running.  
  /// </summary>  
  public interface IStatusContext  
  {  
      void UpdateStatus(string status);  
      void WriteNormal(string message);  
      void WriteSuccess(string message);  
      void WriteWarning(string message);  
      void WriteError(string message);  
      void WriteDebug(string message);  
  }

#### **4.2. Platform Detection**

* **Onboard.Core/Models/PlatformFacts.cs**  
  C\#  
  public enum OperatingSystem { Windows, MacOs, Linux, Unknown }  
  public enum Architecture { X64, Arm64, Unknown }

  /// \<summary\>  
  /// An immutable record holding all facts about the current environment.  
  /// This will be registered as a singleton in the DI container.  
  /// \</summary\>  
  public record PlatformFacts(  
      OperatingSystem OS,  
      Architecture Arch,  
      bool IsWsl, // True if running inside a WSL guest  
      string HomeDirectory  
  );

* Onboard.Core/Services/PlatformDetector.cs  
  This class will use System.Runtime.InteropServices.RuntimeInformation to determine OS and Arch. It will also check for the presence of environment variables like WSL\_DISTRO\_NAME or WSL\_INTEROP to set the IsWsl flag.

#### **4.3. Step Abstractions**

* **Onboard.Core/Abstractions/IOnboardingStep.cs**  
  C\#  
  /// \<summary\>  
  /// The base contract for all onboarding steps.  
  /// \</summary\>  
  public interface IOnboardingStep  
  {  
      /// \<summary\>User-friendly description for progress reporting.\</summary\>  
      string Description { get; }

      /// \<summary\>  
      /// The idempotency check. Returns true if the step needs to run, false if complete.  
      /// \</summary\>  
      Task\<bool\> ShouldExecuteAsync();

      /// \<summary\>  
      /// The action. This only runs if ShouldExecuteAsync() returns true.  
      /// \</summary\>  
      Task ExecuteAsync();  
  }

* Platform-specific installer steps (e.g., `InstallWindowsVsCodeStep`, `InstallMacVsCodeStep`, `InstallLinuxVsCodeStep`) now implement `IOnboardingStep` directly. Each class encapsulates the native package manager commands, defines its own idempotency check, and surfaces clear failure messages.  
  C#  
  public sealed class InstallWindowsVsCodeStep : IOnboardingStep  
  {  
      private readonly IProcessRunner _processRunner;  
      private readonly IUserInteraction _ui;  
  
      public InstallWindowsVsCodeStep(IProcessRunner processRunner, IUserInteraction ui)  
      {  
          _processRunner = processRunner;  
          _ui = ui;  
      }  
  
      public string Description => "Install Visual Studio Code";  
  
      public async Task<bool> ShouldExecuteAsync()  
      {  
          var result = await _processRunner.RunAsync("where", "code.cmd").ConfigureAwait(false);  
          return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput) ? false : true;  
      }  
  
      public async Task ExecuteAsync()  
      {  
          var result = await _processRunner.RunAsync("winget", "install --id Microsoft.VisualStudioCode -e --source winget").ConfigureAwait(false);  
          if (!result.IsSuccess)  
          {  
              throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)  
                  ? "Failed to install Visual Studio Code via winget."  
                  : result.StandardError.Trim());  
          }  
  
          _ui.WriteSuccess("Visual Studio Code installed via winget.");  
      }  
  }  
  
  The macOS and Linux variants follow the same structure, swapping in the appropriate `brew` and `apt` commands. This keeps each installer focused on a single operating system and eliminates the need for a shared strategy layer.

* **Step result aggregation** – Orchestrators collect `StepResult` instances (containing the step name, `StepStatus`, optional skip reason, and any surfaced exception) so that the presentation layer can render a completion summary without duplicating decision logic.

#### **4.4. Orchestration**

* **Onboard.Core/Abstractions/IPlatformOrchestrator.cs**  
  C\#  
  public interface IPlatformOrchestrator  
  {  
      Task ExecuteAsync();  
  }

* Orchestrator Implementations (e.g., WindowsOrchestrator.cs)  
  These classes now derive from `SequentialOrchestrator`, pass the ordered steps through the base constructor, and reuse the shared execution loop.  
  C#  
  public sealed class WindowsOrchestrator : SequentialOrchestrator  
  {  
    public WindowsOrchestrator(  
      IUserInteraction ui,  
      ExecutionOptions executionOptions,  
      EnableWslFeaturesStep enableWslFeaturesStep,  
      InstallGitForWindowsStep installGitForWindowsStep,  
      InstallWindowsVsCodeStep installWindowsVsCodeStep,  
      InstallDockerDesktopStep installDockerDesktopStep,  
      ConfigureGitUserStep configureGitUserStep)  
      : base(  
  ui,  
  executionOptions,  
        "Windows host onboarding",  
        new IOnboardingStep[]  
        {  
          enableWslFeaturesStep,  
          installGitForWindowsStep,  
          installWindowsVsCodeStep,  
          installDockerDesktopStep,  
          configureGitUserStep,  
        })  
    {  
    }  
  }

  The other orchestrators follow the same pattern, each injecting the platform-specific VS Code installer introduced in this iteration.

#### **4.5. The Composition Root (Onboard.Console/Program.cs)**

This is the most critical piece of logic, tying everything together.

C\#

using Microsoft.Extensions.DependencyInjection;  
using Microsoft.Extensions.Hosting;  
using Onboard.Core.Abstractions;  
using Onboard.Core.Services;  
using Onboard.Console.Orchestrators;
using Onboard.Console.Services;  
using Spectre.Console;

public static class Program  
{  
    public static async Task Main(string\[\] args)  
    {  
        // 1\. Parse for our custom mode flag  
        bool isWslGuestMode \= args.Contains("--mode") &&   
                              args.ElementAtOrDefault(Array.IndexOf(args, "--mode") \+ 1) \== "wsl-guest";

        var host \= Host.CreateDefaultBuilder(args)  
            .ConfigureServices((context, services) \=\>  
            {  
                // Register all singleton services  
                services.AddSingleton\<IProcessRunner, ProcessRunner\>();  
                services.AddSingleton\<IAnsiConsole\>(_ => AnsiConsole.Console);  
                services.AddSingleton\<IUserInteraction, SpectreUserInteraction\>();  
                services.AddSingleton\<IPlatformDetector, PlatformDetector\>();

                // Register PlatformFacts by invoking the detector once at startup  
                services.AddSingleton(provider \=\>   
                    provider.GetRequiredService\<IPlatformDetector\>().Detect());

                // Register all Orchestrators  
                services.AddTransient\<WindowsOrchestrator\>();  
                services.AddTransient\<MacOsOrchestrator\>();  
                services.AddTransient\<UbuntuOrchestrator\>();  
                services.AddTransient\<WslGuestOrchestrator\>();

                // Register all Onboarding Steps  
                // Shared  
                services.AddTransient\<ConfigureGitUserStep\>();  
                // Windows  
                services.AddTransient<EnableWslFeaturesStep>();  
                services.AddTransient<InstallGitForWindowsStep>();  
                services.AddTransient<InstallWindowsVsCodeStep>();  
                services.AddTransient<InstallDockerDesktopStep>();  
                // macOS  
                services.AddTransient<InstallHomebrewStep>();  
                services.AddTransient<InstallBrewPackagesStep>();  
                services.AddTransient<InstallMacVsCodeStep>();  
                // Linux / Ubuntu  
                services.AddTransient<AptUpdateStep>();  
                services.AddTransient<InstallAptPackagesStep>();  
                services.AddTransient<InstallLinuxVsCodeStep>();  
                // WSL guest  
                services.AddTransient<InstallWslPrerequisitesStep>();  
                services.AddTransient<ConfigureWslGitCredentialHelperStep>();  
            })  
            .Build();

        // 2\. Select the correct orchestrator based on platform AND mode  
        var platformFacts \= host.Services.GetRequiredService\<PlatformFacts\>();  
        var ui \= host.Services.GetRequiredService\<IUserInteraction\>();  
          
        IPlatformOrchestrator orchestrator;

        try  
        {  
            if (platformFacts.OS \== OperatingSystem.Windows)  
            {  
                orchestrator \= host.Services.GetRequiredService\<WindowsOrchestrator\>();  
            }  
            else if (platformFacts.OS \== OperatingSystem.Linux && platformFacts.IsWsl && isWslGuestMode)  
            {  
                orchestrator \= host.Services.GetRequiredService\<WslGuestOrchestrator\>();  
            }  
            else if (platformFacts.OS \== OperatingSystem.Linux && \!platformFacts.IsWsl)  
            {  
                orchestrator \= host.Services.GetRequiredService\<UbuntuOrchestrator\>();  
            }  
            else if (platformFacts.OS \== OperatingSystem.MacOs)  
            {  
                orchestrator \= host.Services.GetRequiredService\<MacOsOrchestrator\>();  
            }  
            else  
            {  
                throw new NotSupportedException($"Unsupported platform: {platformFacts.OS}, WSL: {platformFacts.IsWsl}");  
            }  
        }  
        catch(Exception ex)  
        {  
            ui.WriteError($"Failed to initialize orchestrator: {ex.Message}");  
            return;  
        }  
          
        // 3\. Execute the chosen orchestration  
        await orchestrator.ExecuteAsync();  
    }  
}

---

### **5\. Testability Strategy**

Unit testing is a primary goal. All logic will be tested in Onboard.Core.Tests.

* **Test Subject:** Any IOnboardingStep implementation.  
* **Mocks:** Mock\<IProcessRunner\>, Mock\<IUserInteraction\>.  
* **Stubs:** A manually created PlatformFacts object.  
* **Example Test (ConfigureGitUserStepTests.cs)**  
  C\#  
  \[Test\]  
  public async Task ConfigureGitUserStep\_WhenConfigMissing\_PromptsUserAndRunsCommands()  
  {  
      // Arrange  
      var mockProcessRunner \= new Mock\<IProcessRunner\>();  
      var mockUI \= new Mock\<IUserInteraction\>();  
      var platformFacts \= new PlatformFacts(OperatingSystem.Windows, Architecture.X64, false, "C:\\\\Users\\\\Test");

      // Simulate that git config is missing  
      mockProcessRunner.Setup(p \=\> p.RunAsync("git", "config \--global user.name"))  
                       .ReturnsAsync(new ProcessResult(1, "", "")); // Exit code 1

      // Simulate user input  
      mockUI.Setup(ui \=\> ui.Prompt("Please enter your full name for Git commits: "))  
            .Returns("Test User");  
      mockUI.Setup(ui \=\> ui.Prompt("Please enter your email for Git commits: "))  
            .Returns("test@example.com");

      var step \= new ConfigureGitUserStep(mockProcessRunner.Object, mockUI.Object);

      // Act  
      bool shouldRun \= await step.ShouldExecuteAsync();  
      await step.ExecuteAsync();

      // Assert  
      Assert.IsTrue(shouldRun);

      // Verify the correct commands were run with the prompted values  
      mockProcessRunner.Verify(p \=\>   
          p.RunAsync("git", "config \--global user.name \\"Test User\\""), Times.Once);  
      mockProcessRunner.Verify(p \=\>   
          p.RunAsync("git", "config \--global user.email \\"test@example.com\\""), Times.Once);

      mockUI.Verify(ui \=\>   
          ui.WriteSuccess("Git user configured as 'Test User \<test@example.com\>'."), Times.Once);  
  }

---

### **6\. Build & Deployment**

#### **6.1. Publish Commands**

A GitHub Actions workflow (.github/workflows/release.yml) will execute these commands on every new Git tag and upload the binaries as release assets.

Bash

\# Windows x64  
dotnet publish src/Onboard.Console \-c Release \-r win-x64 \--self-contained true /p:PublishSingleFile=true \-o ./publish/win-x64 \-p:AssemblyName=Onboard-win-x64

\# macOS Apple Silicon (arm64)  
dotnet publish src/Onboard.Console \-c Release \-r osx-arm64 \--self-contained true /p:PublishSingleFile=true \-o ./publish/osx-arm64 \-p:AssemblyName=Onboard-macos-arm64

\# macOS Intel (x64)  
dotnet publish src/Onboard.Console \-c Release \-r osx-x64 \--self-contained true /p:PublishSingleFile=true \-o ./publish/osx-x64 \-p:AssemblyName=Onboard-macos-x64

\# Linux x64 (for Ubuntu & WSL)  
dotnet publish src/Onboard.Console \-c Release \-r linux-x64 \--self-contained true /p:PublishSingleFile=true \-o ./publish/linux-x64 \-p:AssemblyName=Onboard-linux-x64

#### **6.2. Bootstrapper Script Logic**

* **setup.ps1 (Windows)**: This script will download Onboard-win-x64.exe and execute it.  
  PowerShell  
  $url \= "https://.../Onboard-win-x64.exe"  
  $outFile \= Join-Path $env:TEMP "Onboard.exe"  
  Invoke-WebRequest \-Uri $url \-OutFile $outFile  
  Start-Process \-FilePath $outFile \-Wait  
  Remove-Item $outFile

* **setup.sh (macOS/Linux/WSL)**: This script must detect the OS/Arch to download the correct binary *and pass all arguments* ("$@") to it.  
  Bash  
  \#\!/bin/bash  
  set \-euo pipefail

  OS=$(uname \-s)  
  ARCH=$(uname \-m)  
  BINARY\_NAME=""

  if \[ "$OS" \== "Darwin" \]; then  
    \[ "$ARCH" \== "arm64" \] && BINARY\_NAME="Onboard-macos-arm64" || BINARY\_NAME="Onboard-macos-x64"  
  elif \[ "$OS" \== "Linux" \]; then  
    \[ "$ARCH" \== "x86\_64" \] && BINARY\_NAME="Onboard-linux-x64"  
  fi  
  \# ... error handling if BINARY\_NAME is empty ...

  URL="https://.../$BINARY\_NAME"  
  OUT\_FILE="/tmp/Onboard"

  curl \-L "$URL" \-o "$OUT\_FILE"  
  chmod \+x "$OUT\_FILE"

  \# Critically, pass all script arguments ("$@") to the binary  
  "$OUT\_FILE" "$@"

  rm "$OUT\_FILE"  