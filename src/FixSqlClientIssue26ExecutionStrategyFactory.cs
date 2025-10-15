using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace efmssql;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class FixSqlClientIssue26ExecutionStrategy(ExecutionStrategyDependencies dependencies) : SqlServerExecutionStrategy(dependencies)
{
    private static readonly string OperationCanceledMessage = "The operation was canceled.";

    static FixSqlClientIssue26ExecutionStrategy()
    {
        try
        {
            new CancellationToken(canceled: true).ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException exception)
        {
            OperationCanceledMessage = exception.Message;
        }
    }

    public static IExecutionStrategy Create(ExecutionStrategyDependencies dependencies) => new FixSqlClientIssue26ExecutionStrategy(dependencies);

    public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken)
    {
        try
        {
            return await base.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(OperationCanceledMessage, exception, cancellationToken);
        }
    }
}
