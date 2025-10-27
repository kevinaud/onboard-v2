namespace Onboard.Core.Steps.PlatformAware;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

using OperatingSystem = Onboard.Core.Models.OperatingSystem;

/// <summary>
/// Base class for onboarding steps that execute different logic per platform.
/// </summary>
public abstract class PlatformAwareStep : IOnboardingStep
{
    private readonly Dictionary<OperatingSystem, Func<Task<bool>>> shouldExecuteStrategies = new();
    private readonly Dictionary<OperatingSystem, Func<Task>> executeStrategies = new();

    protected PlatformAwareStep(PlatformFacts platformFacts, IProcessRunner processRunner)
    {
        PlatformFacts = platformFacts;
        ProcessRunner = processRunner;
    }

    /// <summary>Gets the description shown to the user.</summary>
    public abstract string Description { get; }

    /// <summary>Gets the detected platform facts.</summary>
    protected PlatformFacts PlatformFacts { get; }

    /// <summary>Gets the process runner helper.</summary>
    protected IProcessRunner ProcessRunner { get; }

    public Task<bool> ShouldExecuteAsync()
    {
        if (shouldExecuteStrategies.TryGetValue(PlatformFacts.OS, out var strategy))
        {
            return strategy();
        }

        return Task.FromResult(false);
    }

    public Task ExecuteAsync()
    {
        if (executeStrategies.TryGetValue(PlatformFacts.OS, out var strategy))
        {
            return strategy();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers the delegates that implement the step for a given operating system.
    /// </summary>
    /// <param name="operatingSystem">The operating system to register.</param>
    /// <param name="shouldExecute">Delegate used for the idempotency check.</param>
    /// <param name="execute">Delegate used to perform the action.</param>
    protected void AddStrategy(
        OperatingSystem operatingSystem,
        Func<Task<bool>> shouldExecute,
        Func<Task> execute)
    {
        shouldExecuteStrategies[operatingSystem] = shouldExecute;
        executeStrategies[operatingSystem] = execute;
    }
}
