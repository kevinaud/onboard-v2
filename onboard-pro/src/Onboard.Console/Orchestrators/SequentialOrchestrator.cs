namespace Onboard.Console.Orchestrators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Provides shared orchestration logic for sequential onboarding steps.
/// </summary>
public abstract class SequentialOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction userInteraction;
    private readonly ExecutionOptions executionOptions;
    private readonly IReadOnlyList<IOnboardingStep> steps;
    private readonly string title;

    protected SequentialOrchestrator(IUserInteraction userInteraction, ExecutionOptions executionOptions, string title, IEnumerable<IOnboardingStep> steps)
    {
        this.userInteraction = userInteraction;
        this.executionOptions = executionOptions;
        this.title = title;
        this.steps = steps.ToList();
    }

    public async Task ExecuteAsync()
    {
        userInteraction.WriteHeader(title);

        foreach (var step in steps)
        {
            userInteraction.WriteLine($"Checking {step.Description}...");

            bool shouldExecute;
            try
            {
                shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw OnboardingStepException.CheckFailed(step.Description, ex);
            }

            if (!shouldExecute)
            {
                userInteraction.WriteSuccess($"{step.Description} already configured.");
                continue;
            }

            if (executionOptions.IsDryRun)
            {
                userInteraction.WriteLine($"Dry run: would execute {step.Description}.");
                continue;
            }

            userInteraction.WriteLine($"Running {step.Description}...");

            try
            {
                await step.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw OnboardingStepException.ExecutionFailed(step.Description, ex);
            }
        }

        userInteraction.WriteLine(string.Empty);
        userInteraction.WriteSuccess(executionOptions.IsDryRun ? $"{title} dry run complete." : $"{title} complete.");
    }
}
