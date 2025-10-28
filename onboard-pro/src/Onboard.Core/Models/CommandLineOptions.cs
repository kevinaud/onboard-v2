namespace Onboard.Core.Models;

using System.Runtime.InteropServices;

/// <summary>
/// Represents parsed command-line options.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CommandLineOptions(bool IsWslGuestMode, bool IsDryRun);
