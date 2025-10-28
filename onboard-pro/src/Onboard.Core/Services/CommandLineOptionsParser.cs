// <copyright file="CommandLineOptionsParser.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using System;
using System.Collections.Generic;

using Onboard.Core.Models;

/// <summary>
/// Parses supported command-line options for the onboarding executable.
/// </summary>
public static class CommandLineOptionsParser
{
    private const string ModeOption = "--mode";
    private const string ModeOptionWithEquals = "--mode=";
    private const string DryRunOption = "--dry-run";
    private const string DryRunOptionWithEquals = "--dry-run=";
    private const string VerboseOption = "--verbose";
    private const string VerboseOptionWithEquals = "--verbose=";
    private const string VerboseAlias = "-v";
    private const string WslGuestValue = "wsl-guest";

    /// <summary>
    /// Attempts to parse the supported command-line options from the supplied argument list.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="options">Populated with the parsed options when parsing succeeds.</param>
    /// <param name="errorMessage">Set to a user-facing error when parsing fails.</param>
    /// <returns>True when parsing succeeds; otherwise, false.</returns>
    public static bool TryParse(IEnumerable<string> args, out CommandLineOptions options, out string? errorMessage)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        bool isWslGuestMode = false;
        bool? isDryRun = null;
        bool? isVerbose = null;
        bool expectingModeValue = false;
        bool modeSpecified = false;

        errorMessage = null;

        foreach (string arg in args)
        {
            if (expectingModeValue)
            {
                if (!TryParseModeValue(arg, ref isWslGuestMode, ref errorMessage))
                {
                    options = default;
                    return false;
                }

                expectingModeValue = false;
                modeSpecified = true;
                continue;
            }

            if (string.Equals(arg, ModeOption, StringComparison.Ordinal))
            {
                if (modeSpecified)
                {
                    errorMessage = "The --mode option can only be specified once.";
                    options = default;
                    return false;
                }

                expectingModeValue = true;
                continue;
            }

            if (arg.StartsWith(ModeOptionWithEquals, StringComparison.Ordinal))
            {
                if (modeSpecified)
                {
                    errorMessage = "The --mode option can only be specified once.";
                    options = default;
                    return false;
                }

                string value = arg[ModeOptionWithEquals.Length..];
                if (!TryParseModeValue(value, ref isWslGuestMode, ref errorMessage))
                {
                    options = default;
                    return false;
                }

                modeSpecified = true;
                continue;
            }

            if (string.Equals(arg, DryRunOption, StringComparison.Ordinal))
            {
                if (isDryRun.HasValue)
                {
                    errorMessage = "The --dry-run option can only be specified once.";
                    options = default;
                    return false;
                }

                isDryRun = true;
                continue;
            }

            if (arg.StartsWith(DryRunOptionWithEquals, StringComparison.Ordinal))
            {
                if (isDryRun.HasValue)
                {
                    errorMessage = "The --dry-run option can only be specified once.";
                    options = default;
                    return false;
                }

                string value = arg[DryRunOptionWithEquals.Length..];
                if (!TryParseBooleanOption(DryRunOption, value, out bool parsedDryRun, out errorMessage))
                {
                    options = default;
                    return false;
                }

                isDryRun = parsedDryRun;
                continue;
            }

            if (string.Equals(arg, VerboseOption, StringComparison.Ordinal) || string.Equals(arg, VerboseAlias, StringComparison.Ordinal))
            {
                if (isVerbose.HasValue)
                {
                    errorMessage = "The --verbose option can only be specified once.";
                    options = default;
                    return false;
                }

                isVerbose = true;
                continue;
            }

            if (arg.StartsWith(VerboseOptionWithEquals, StringComparison.Ordinal))
            {
                if (isVerbose.HasValue)
                {
                    errorMessage = "The --verbose option can only be specified once.";
                    options = default;
                    return false;
                }

                string value = arg[VerboseOptionWithEquals.Length..];
                if (!TryParseBooleanOption(VerboseOption, value, out bool parsedVerbose, out errorMessage))
                {
                    options = default;
                    return false;
                }

                isVerbose = parsedVerbose;
                continue;
            }
        }

        if (expectingModeValue)
        {
            errorMessage = "The --mode option requires a value.";
            options = default;
            return false;
        }

        options = new CommandLineOptions(isWslGuestMode, isDryRun ?? false, isVerbose ?? false);
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

    private static bool TryParseBooleanOption(string optionName, string rawValue, out bool parsedValue, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            errorMessage = $"Unsupported {optionName} value ''. Expected 'true' or 'false'.";
            parsedValue = false;
            return false;
        }

        if (rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            parsedValue = true;
            errorMessage = null;
            return true;
        }

        if (rawValue.Equals("false", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("no", StringComparison.OrdinalIgnoreCase) || rawValue.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            parsedValue = false;
            errorMessage = null;
            return true;
        }

        errorMessage = $"Unsupported {optionName} value '{rawValue}'. Expected 'true' or 'false'.";
        parsedValue = false;
        return false;
    }
}
