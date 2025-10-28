### **Iteration 14: Centralized Configuration**

**Goal:** Extract hardcoded values (like distro versions) into a centralized configuration source to prevent future maintenance headaches.

1.  **Create Configuration Model:**
    *   Create `src/Onboard.Core/Models/OnboardingConfiguration.cs`.
    *   Add properties for standard values, for example:
        ```csharp
        public string WslDistroName { get; init; } = "Ubuntu-22.04";
        public string WslDistroImage { get; init; } = "Ubuntu-22.04";
        ```

2.  **Register Configuration:**
    *   In `Program.cs`, register this configuration object as a singleton. (For now, hardcoded defaults in the class are acceptable, but this paves the way for reading from a JSON file later if needed).

3.  **Refactor Steps to use Configuration:**
    *   Update `EnableWslFeaturesStep.cs` to inject `OnboardingConfiguration` and use `WslDistroName` instead of the hardcoded "Ubuntu-22.04" string.
    *   Update `InstallDockerDesktopStep.cs` to use the same configuration value when checking/updating `settings-store.json`.
