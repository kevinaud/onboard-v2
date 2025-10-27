// <copyright file="CommandLineOptionsParser.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

/// <summary>
/// Parses supported command-line options for the onboarding executable.
/// </summary>
public static class CommandLineOptionsParser
{
    private const string ModeOption = "--mode";
    private const string ModeOptionWithEquals = "--mode=";
    private const string WslGuestValue = "wsl-guest";

    /// <summary>
    /// Attempts to parse the --mode option from the supplied argument list.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="isWslGuestMode">Set to true when --mode wsl-guest is specified.</param>
    /// <param name="errorMessage">Set to a user-facing error when parsing fails.</param>
    /// <returns>True when parsing succeeds; otherwise, false.</returns>
    public static bool TryParseMode(IEnumerable<string> args, out bool isWslGuestMode, out string? errorMessage)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        isWslGuestMode = false;
        errorMessage = null;

        bool expectingValue = false;
        bool modeSpecified = false;

        foreach (string arg in args)
        {
            if (expectingValue)
            {
                if (!TryParseModeValue(arg, ref isWslGuestMode, ref errorMessage))
                {
                    return false;
                }

                expectingValue = false;
                modeSpecified = true;
                continue;
            }

            if (string.Equals(arg, ModeOption, StringComparison.Ordinal))
            {
                if (modeSpecified)
                {
                    errorMessage = "The --mode option can only be specified once.";
                    return false;
                }

                expectingValue = true;
                continue;
            }

            if (arg.StartsWith(ModeOptionWithEquals, StringComparison.Ordinal))
            {
                if (modeSpecified)
                {
                    errorMessage = "The --mode option can only be specified once.";
                    return false;
                }

                string value = arg[ModeOptionWithEquals.Length..];
                if (!TryParseModeValue(value, ref isWslGuestMode, ref errorMessage))
                {
                    return false;
                }

                modeSpecified = true;
            }
        }

        if (expectingValue)
        {
            errorMessage = "The --mode option requires a value.";
            return false;
        }

        return true;
    }

    private static bool TryParseModeValue(string? rawValue, ref bool isWslGuestMode, ref string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            errorMessage = "The --mode option requires a non-empty value.";
            return false;
        }

        if (string.Equals(rawValue, WslGuestValue, StringComparison.OrdinalIgnoreCase))
        {
            isWslGuestMode = true;
            errorMessage = null;
            return true;
        }

        errorMessage = $"Unsupported --mode value '{rawValue}'. Expected 'wsl-guest'.";
        return false;
    }
}
