### **Iteration 19: Windows Developer Experience Parity**

**Goal:** Close the gap between the legacy Windows bootstrapper and the new Onboard Pro flow by delivering the remaining quality-of-life tooling and configuration steps. This iteration ensures a freshly provisioned Windows host can immediately code, authenticate, and containerize without manual follow-up tasks.

---

#### **1. Ship VS Code Remote Development Essentials**

*   **Context:** The legacy automation ensures that VS Code can seamlessly target WSL and containers via the official Remote Development extension pack. Our new onboarding skips this, leaving remote workflows unavailable out of the box.
*   **Problem:** Without the extension pack, developers cannot attach VS Code to WSL or Docker targets, undermining the rest of the provisioning work.
*   **Solution:**
    1. Detect whether the Remote Development extension pack (`ms-vscode-remote.vscode-remote-extensionpack`) is already installed. Prefer `code --list-extensions` for idempotency.
    2. If missing, install it using `code --install-extension` via `IProcessRunner` (non-elevated).
    3. Surface a concise status message in the orchestrator and add unit coverage to confirm the install command fires only when needed.

---

#### **2. Configure Dotfiles Bootstrapping in VS Code**

*   **Context:** The legacy flow seeds VS Code's `settings.json` with dotfiles metadata so that developer environments hydrate automatically. The new implementation leaves this entirely manual.
*   **Problem:** Developers must remember to wire up dotfiles themselves, delaying workstation readiness and creating inconsistent setups.
*   **Solution:**
    1. Read the user's `%APPDATA%\Code\User\settings.json` (respect `IFileSystem` seams). If `dotfiles.repository` already exists, skip the step.
    2. Otherwise, prompt the user with three options: (a) provide a `githubuser/reponame`, (b) accept the default `kevinaud/dotfiles`, (c) skip configuration.
    3. Persist the chosen repository (and optional `dotfiles.targetPath`) back into `settings.json`, preserving any existing settings.
    4. Add automated tests that cover: existing configuration, user-supplied repository, default fallback, and skip path.

---

#### **3. Pre-authenticate Git Credential Manager with GitHub**

*   **Context:** The previous installer launched GitHub authentication so that Git Credential Manager (GCM) held a fresh OAuth token before work began. We currently omit this step, which forces a disruptive browser round-trip on the first git operation.
*   **Problem:** Developers hit git auth friction during their first push/pull, often interrupting onboarding workshops or demos.
*   **Solution:**
    1. Detect if GCM already has a GitHub credential (`git credential-manager get` wrapped via `IProcessRunner` and environment variables).
    2. When credentials are absent, invoke `gh auth login --hostname github.com --git-protocol https` with the `--web` flow so the browser-based experience matches legacy behavior.
    3. Provide clear messaging before and after the login. Add unit tests that validate the happy path and the "already authenticated" skip scenario. Mock the `gh` invocation for determinism.

---

#### **4. Align Docker Desktop with WSL Distro Selection**

*   **Context:** Docker Desktop stores its integration preferences in `%APPDATA%\Docker\settings-store.json`. Legacy onboarding synchronized this list with the WSL distro chosen earlier and restarted Docker when changes were made.
*   **Problem:** Our current workflow may leave the selected WSL distro disabled, breaking container-based dev inside WSL.
*   **Solution:**
    1. Reuse the distro information gathered in the WSL prerequisite step. Ensure it is threaded into the Docker configuration step (likely via shared state or configuration object).
    2. Read `settings-store.json` (using `IFileSystem`). If the selected distro is missing from `IntegratedWslDistros`, append it while preserving other entries.
    3. Persist the updated JSON atomically and, when changes occur, restart Docker Desktop (`powershell Start-Process -FilePath "Docker Desktop" -Verb RunAs -ArgumentList '"--shutdown"'` or equivalent) so the setting takes effect. Respect idempotency: no restart when no changes were required.
    4. Introduce unit tests that cover: no-op when the distro is present, append when missing, and restart invocation when mutations are made.

---

#### **5. Orchestrator & UX Integration**

*   **Context:** These additions must feel native within the existing Windows orchestrator workflow and log output.
*   **Tasks:**
    * Introduce dedicated onboarding steps under `Steps/Windows/` for each feature, honoring the `ShouldExecuteAsync`/`ExecuteAsync` contract and idempotency rules.
    * Register the new steps in `WindowsOrchestrator` in logical order: tooling (VS Code extensions), configuration (dotfiles, Docker), authentication (GCM), final checks.
    * Update the summary table expectations and extend unit tests in `Onboard.Core.Tests` to cover the new sequencing and branching paths.

---

#### **Definition of Done**

* New steps exist for VS Code extensions, dotfiles configuration, GitHub auth, and Docker integration.
* User prompts and file mutations follow existing abstractions (`IUserInteraction`, `IFileSystem`, `IProcessRunner`) and remain idempotent.
* Automated tests validate all new behaviors, including no-op paths.
* Windows onboarding run-through shows the new steps executing/skipping appropriately, with Docker ready for the chosen WSL distro and Git operations authenticated.
