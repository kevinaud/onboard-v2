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
        var results = new List<StepResult>();
        OnboardingStepException? failure = null;

        this.userInteraction.WriteNormal($"Starting {this.title}...");

        foreach (var step in this.steps)
        {
            try
            {
                await this.userInteraction.RunStatusAsync($"Checking {step.Description}...", async status =>
                {
                    bool shouldExecute;

                    try
                    {
                        shouldExecute = await step.ShouldExecuteAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        status.WriteError($"Failed while checking {step.Description}: {ex.Message}");
                        results.Add(new StepResult(step.Description, StepStatus.Failed, Exception: ex));
                        throw OnboardingStepException.CheckFailed(step.Description, ex);
                    }

                    if (!shouldExecute)
                    {
                        status.WriteSuccess($"{step.Description} already configured.");
                        results.Add(new StepResult(step.Description, StepStatus.Skipped, "Already configured"));
                        return;
                    }

                    if (this.executionOptions.IsDryRun)
                    {
                        status.WriteNormal($"Dry run: would execute {step.Description}.");
                        results.Add(new StepResult(step.Description, StepStatus.Skipped, "Dry run"));
                        return;
                    }

                    status.UpdateStatus($"Running {step.Description}...");

                    try
                    {
                        await step.ExecuteAsync().ConfigureAwait(false);
                        status.WriteSuccess($"{step.Description} completed.");
                        results.Add(new StepResult(step.Description, StepStatus.Executed));
                    }
                    catch (Exception ex)
                    {
                        status.WriteError($"{step.Description} failed: {ex.Message}");
                        results.Add(new StepResult(step.Description, StepStatus.Failed, Exception: ex));
                        throw OnboardingStepException.ExecutionFailed(step.Description, ex);
                    }
                }).ConfigureAwait(false);
            }
            catch (OnboardingStepException ex)
            {
                failure = ex;
                break;
            }
        }

        this.userInteraction.ShowSummary(results);

        if (failure is null)
        {
            this.userInteraction.WriteSuccess(this.executionOptions.IsDryRun ? $"{this.title} dry run complete." : $"{this.title} complete.");
            return;
        }

        throw failure;
    }
}
