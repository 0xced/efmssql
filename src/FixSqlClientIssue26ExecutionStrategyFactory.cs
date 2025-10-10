using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace efmssql;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public class FixSqlClientIssue26ExecutionStrategyFactory(ExecutionStrategyDependencies dependencies) : SqlServerExecutionStrategyFactory(dependencies)
{
    protected override IExecutionStrategy CreateDefaultStrategy(ExecutionStrategyDependencies dependencies) => new Strategy(dependencies);

    private class Strategy(ExecutionStrategyDependencies dependencies) : SqlServerExecutionStrategy(dependencies)
    {
        private static readonly Lazy<string> OperationCanceledMessage = new(() =>
        {
            try
            {
                new CancellationToken(canceled: true).ThrowIfCancellationRequested();
                return "The operation was canceled.";
            }
            catch (OperationCanceledException exception)
            {
                return exception.Message;
            }
        });

        public override async Task<TResult> ExecuteAsync<TState, TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded, CancellationToken cancellationToken)
        {
            try
            {
                return await base.ExecuteAsync(state, operation, verifySucceeded, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(OperationCanceledMessage.Value, exception, cancellationToken);
            }
        }
    }
}
