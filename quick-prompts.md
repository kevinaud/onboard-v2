## Quick prompts

### Task Orchestrator

- Initialize the task tree from PRDs and iterations.

- Reconcile tasks with the latest iterations.md and backfill shared workflow steps across all iterations.

- Show a summary table for iter-5 and flag any missing shared steps.

### Developer

- Select the active iteration, list pending subtasks, and propose the best next subtask with rationale.

- Implement `InstallBuildEssentialStep` idempotently (ShouldExecuteAsync + ExecuteAsync), add NUnit tests, push to `iter-3/build-essential`, open a PR, and drive CI to green.

- Poll the current PR’s CI until completion, summarize failures with file/line/test names, and fix incrementally.

- Mark the subtask ‘Open PR: iter-3’ done with PR and run links; create a follow-up subtask for README updates.
