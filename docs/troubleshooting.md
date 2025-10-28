# Diagnostics Troubleshooting Guide

The Onboard Pro binary now mirrors every user-visible message and external command invocation to a log file so support engineers can reproduce issues quickly. Use the steps below when you need to gather evidence from a failing run.

## Locate the log file

| Platform | Path |
| --- | --- |
| Windows | `%TEMP%\onboard-pro.log` |
| macOS | `/tmp/onboard-pro.log` |
| Ubuntu / WSL | `/tmp/onboard-pro.log` |

> The application resolves the file path by combining `Path.GetTempPath()` with `onboard-pro.log`. Retention is single-file: each run overwrites the previous log. Copy the file to a safe location before rerunning the tool if you want to preserve history.

## What the log contains

- A timestamped entry for every console message (`INFO`, `HEADER`, `SUCCESS`, `WARNING`, `ERROR`, `PROMPT`, `PROMPT_RESPONSE`).
- A Debug-level entry for every external command, including the executable path, arguments, exit code, and the first 1024 characters of stdout/stderr. Longer outputs are truncated with an ellipsis.
- Exceptions raised by onboarding steps, including the step name when available.

Sensitive information such as personal access tokens should not appear in the log because prompts echo only what the user typed. Nevertheless, review the file before sharing externally.

## Attach the log to a support ticket

1. Reproduce the issue.
2. Copy the log file listed above to a new location (for example, your Desktop).
3. Compress the file if requested (e.g., `zip onboard-pro-log.zip onboard-pro.log`).
4. Provide the archive along with the command-line flags you used (such as `--mode wsl-guest --dry-run`).

Dry-run mode still records the console transcript but skips process execution, so the log will not contain command entries in that scenario.

## Resetting the log between runs

Delete the existing file (`del %TEMP%\onboard-pro.log` on Windows, `rm /tmp/onboard-pro.log` on macOS/Linux) if you want to ensure the next run starts with a clean slate. The application will recreate the file automatically.
