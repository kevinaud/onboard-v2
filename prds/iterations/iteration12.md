### **Iteration 12: Reliability and Diagnostics**

**Goal:** Improve the robustness of external process execution and add persistent logging to aid in debugging user issues.

1.  **Enhance `IProcessRunner` for Linux:**
    *   Modify `ProcessRunner.cs` to automatically inject the `DEBIAN_FRONTEND=noninteractive` environment variable into the process start info when running on Linux. This prevents `apt-get` from hanging on standard input requests (like `tzdata` configuration).

2.  **Implement Persistent Logging:**
    *   Introduce `Serilog` and `Serilog.Sinks.File` packages to `Onboard.Console`.
    *   Configure standard .NET `ILogger<T>` in `Program.cs` to write to a log file in a standard location (e.g., `%TEMP%/onboard-pro.log` on Windows, `/tmp/onboard-pro.log` on Linux/macOS).
    *   Inject `ILogger<ProcessRunner>` into `ProcessRunner` and log *all* command executions (command, arguments, exit code, and a truncated version of stdout/stderr) at the `Debug` level.
    *   Update `ConsoleUserInteraction` to also log all user-facing output to the file logger, ensuring a complete transcript of the session exists.
