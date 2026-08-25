using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace WorkaroundSqlClientIssue26.Tests;

public sealed class RecordRetryingStrategy(ExecutionStrategyDependencies dependencies) : SqlServerRetryingExecutionStrategy(dependencies)
{
    public readonly List<(Exception Exception, bool ShouldRetry)> Retries = [];
    public bool Executed { get; private set; }

    public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken = new CancellationToken())
    {
        Executed = true;
        return await base.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        var shouldRetry = base.ShouldRetryOn(exception);
        Retries.Add((exception, shouldRetry));
        return shouldRetry;
    }
}