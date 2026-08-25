using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace WorkaroundSqlClientIssue26.Tests;

public class WorkaroundSqlClientIssue26Test(ITestOutputHelper output, MsSqlFixture fixture) : IClassFixture<MsSqlFixture>
{
    private static readonly Type[] ExpectedSqlClientErroneousCancellationExceptionTypes = [typeof(TaskCanceledException), typeof(SqlException), typeof(InvalidOperationException)];

    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "Required for xUnit")]
    public static TheoryData<string> TestData =
    [
        "SqlConnection.InternalOpenAsync",
        "SqlCommand.BeginExecuteReaderInternalReadStage",
        "SqlCommand.WriteBeginExecuteEvent",
    ];

    [Theory, MemberData(nameof(TestData))]
    public async Task TestWorkaround(string cancellationMessage)
        => await ExecuteTestAsync(cancellationMessage, sql => sql.WorkAroundSqlClientIssue26());

    [Theory, MemberData(nameof(TestData))]
    public async Task TestRetryOnFailureAndWorkaround(string cancellationMessage)
        => await ExecuteTestAsync(cancellationMessage, sql => sql.EnableRetryOnFailure().WorkAroundSqlClientIssue26());

    [Theory, MemberData(nameof(TestData))]
    public async Task TestRecordRetryOnFailureAndWorkaround(string cancellationMessage)
    {
        RecordRetryingStrategy strategy = null!;
        await ExecuteTestAsync(cancellationMessage, sql => sql.ExecutionStrategy(d => strategy = new RecordRetryingStrategy(d)).WorkAroundSqlClientIssue26());
        Assert.True(strategy.Executed);

        // There's unfortunately little to assert on this since the cancellation exceptions are rather random, so they are just logged
        foreach (var (exception, shouldRetry) in strategy.Retries)
        {
            var sqlErrors = (exception as SqlException)?.Errors.Cast<SqlError>().Select(e => e.Number) ?? [];
            output.WriteLine($"🔁 ShouldRetry: {shouldRetry}, Errors: {string.Join(", ", sqlErrors)} Exception: {exception}");
        }
    }

    private async Task ExecuteTestAsync(string cancellationMessage, Action<SqlServerDbContextOptionsBuilder> sqlServerOptionsAction)
    {
        using var cancellationEventListener = new CancellationEventListener(output, cancellationMessage);
        var cancellationToken = cancellationEventListener.CancellationToken;

        await using var context = fixture.CreateChinookContext(output, sqlServerOptionsAction);

        var exception = await Record.ExceptionAsync(() => context.Tracks.CountAsync(cancellationToken));

        output.WriteLine($"💥 Recorded exception from {exception?.Source}: {exception}");
        var operationCanceledException = Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
        Assert.Equal(cancellationToken, operationCanceledException.CancellationToken);

        // The exception we get is not 100% reliable; sometimes the exception is thrown from EF Core, sometimes from SqlClient.
        // When it's coming from EF Core, the FixSqlClientIssue26ExecutionStrategy can't catch it
        if (operationCanceledException.Source == "WorkaroundSqlClientIssue26")
        {
            // When it's coming from SqlClient, ensure the erroneous exception is saved in the inner exception
            output.WriteLine($"💥 Inner exception from {exception.InnerException?.Source}: {exception.InnerException}");
            Assert.NotNull(exception.InnerException);
            Assert.Contains(exception.InnerException.GetType(), ExpectedSqlClientErroneousCancellationExceptionTypes);
        }
    }
}