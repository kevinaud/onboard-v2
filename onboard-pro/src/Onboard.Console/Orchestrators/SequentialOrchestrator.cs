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
        this.userInteraction.WriteNormal(title);

        foreach (var step in this.steps)
        {
            this.userInteraction.WriteNormal($"Checking {step.Description}...");

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
                this.userInteraction.WriteSuccess($"{step.Description} already configured.");
                continue;
            }

            if (this.executionOptions.IsDryRun)
            {
                this.userInteraction.WriteNormal($"Dry run: would execute {step.Description}.");
                continue;
            }

            this.userInteraction.WriteNormal($"Running {step.Description}...");

            try
            {
                await step.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw OnboardingStepException.ExecutionFailed(step.Description, ex);
            }
        }

        this.userInteraction.WriteNormal(string.Empty);
        this.userInteraction.WriteSuccess(this.executionOptions.IsDryRun ? $"{this.title} dry run complete." : $"{this.title} complete.");
    }
}
