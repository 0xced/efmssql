The goal of this project is to reproduce and workaround an old and still unaddressed SqlClient issue (#26): [Cancelling an async SqlClient operation throws SqlException, not TaskCanceledException](https://github.com/dotnet/SqlClient/issues/26)

I was bitten by this issue when using [Blazor virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization#item-provider-delegate) where an `OperationCanceledException` is expected to signal cancelation: https://github.com/dotnet/aspnetcore/blob/v9.0.10/src/Components/Web/src/Virtualization/Virtualize.cs#L437

## Reproducing the issue

The issue can be reproduced by cancelling a database query just at the right time, i.e. a few milliseconds after it starts executing. Using the SQL Server container through [Testcontainers for .NET](https://dotnet.testcontainers.org) with the [Chinook database](https://github.com/lerocha/chinook-database) makes it relatively easy to create a simple project for reproduction. EF Core and a custom `DbCommandInterceptor` are used to trigger the cancellation at the right time.

Here's what happens on a M2 MacBook Pro running macOS 15.6.1 with [OrbStack](https://orbstack.dev).

### Without the workaround

**dotnet run --project src/efmssql.csproj -- --no-workaround**

As described in issue #26, a `SqlException` is thrown instead of an `OperationCanceledException` which would be the expected exception.

```
⏱️ Canceling after 2 ms
➡️ await context.Database.EnsureCreatedAsync()
➡️ context.Tracks.CountAsync(cancellationToken)
❌ An unexpected exception occured
SqlException: A severe error occurred on the current command.  The results, if any, should be discarded.
Operation cancelled by user.
  at DbDataReader Microsoft.Data.SqlClient.SqlCommand.<>c.<ExecuteDbDataReaderAsync>b__195_0(Task<SqlDataReader> result)
  […]
  at async Task<TSource> Microsoft.EntityFrameworkCore.Query.ShapedQueryCompilingExpressionVisitor.SingleAsync<TSource>(IAsyncEnumerable<TSource> asyncEnumerable, CancellationToken cancellationToken)
  at async Task Program.<Main>$(string[] args) in Program.cs:43
```

### With the workaround

**dotnet run --project src/efmssql.csproj**

The `SqlException` is now wrapped inside an `OperationCanceledException`, hooray! 🎉

```
⏱️ Canceling after 2 ms
✳️ Using issue #26 workaround (FixSqlClientIssue26ExecutionStrategy)
➡️ await context.Database.EnsureCreatedAsync()
➡️ context.Tracks.CountAsync(cancellationToken)
⚪️ The operation was canceled.
SqlException: A severe error occurred on the current command.  The results, if any, should be discarded.
Operation cancelled by user.
  at DbDataReader Microsoft.Data.SqlClient.SqlCommand.<>c.<ExecuteDbDataReaderAsync>b__195_0(Task<SqlDataReader> result)
  […]
  at async Task<TResult> Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken)
  at async Task<TResult> efmssql.FixSqlClientIssue26ExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken) in FixSqlClientIssue26ExecutionStrategy.cs:34
```

## Working around the issue

I have chosen to workaround this issue at the EF Core level because it's easy to control all code that uses the [SQL Server driver](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) through a [custom execution strategy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency#custom-execution-strategy).

The custom stragegy inherits from the built-in `SqlServerExecutionStrategy` and wraps any exception that occurs when cancellation is requested into an `OperationCanceledException`.

Note that **all** exceptions are caught when cancellation is requested. It's not enough to catch `SqlException` since other types of exceptions might also be thrown. For example, running `dotnet run --project src/efmssql.csproj -- --skip-init` might throw an `InvalidOperationException`. Since the exception is sensitive to the cancellation timing, it might be different each time.

<details>
<summary>Example #1 trace with `InvalidOperationException`</summary>

```
⏱️ Canceling after 5 ms
✳️ Using issue #26 workaround (FixSqlClientIssue26ExecutionStrategy)
➡️ context.Tracks.CountAsync(cancellationToken)
⚪️ The operation was canceled.
InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure' to the 'UseSqlServer' call.
     SqlException: The request failed to run because the batch is aborted, this can be caused by abort signal sent from client, or another request is running in the same session, which makes the session busy.
     Operation cancelled by user.
       at DbDataReader Microsoft.Data.SqlClient.SqlCommand.<>c.<ExecuteDbDataReaderAsync>b__195_0(Task<SqlDataReader> result)
       […]
       at async Task<TResult> Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken)
  at async Task<TResult> Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken)
  at async Task<TResult> efmssql.FixSqlClientIssue26ExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken) in FixSqlClientIssue26ExecutionStrategy.cs:38
```

</details>

<details>
<summary>Example #2 trace with `InvalidOperationException`</summary>

```
⏱️ Canceling after 5 ms
✳️ Using issue #26 workaround (FixSqlClientIssue26ExecutionStrategy)
➡️ context.Tracks.CountAsync(cancellationToken)
⚪️ The operation was canceled.
InvalidOperationException: Operation cancelled by user.
  at DbDataReader Microsoft.Data.SqlClient.SqlCommand.<>c.<ExecuteDbDataReaderAsync>b__195_0(Task<SqlDataReader> result)
  […]
  at async Task<TResult> Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal.SqlServerExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken)
  at async Task<TResult> efmssql.FixSqlClientIssue26ExecutionStrategy.ExecuteAsync<TState,TResult>(TState state, Func<DbContext, TState, CancellationToken, Task<TResult>> operation, Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>> verifySucceeded, CancellationToken cancellationToken) in FixSqlClientIssue26ExecutionStrategy.cs:38
```

</details>

The implementation can be found in [src/FixSqlClientIssue26ExecutionStrategy.cs](src/FixSqlClientIssue26ExecutionStrategy.cs)

Registering this strategy is done with the options of the `UseSqlServer` extension method.

```csharp
var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlServer(connectionString, sql => sql.ExecutionStrategy(FixSqlClientIssue26ExecutionStrategy.Create))
    .Options;
```
