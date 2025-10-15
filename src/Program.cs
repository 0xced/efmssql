using System;
using System.Linq;
using Database;
using efmssql;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

var sqlContainer = await AnsiConsole.Status()
    .Spinner(Spinner.Known.OrangePulse)
    .StartAsync("Starting SQL Server container", _ => ChinookContainer.StartAsync(AnsiConsole.WriteLine));

AnsiConsole.WriteLine($"🛢 SQL Server database available on {sqlContainer.ConnectionString}");

var useWorkaround = !args.Contains("--no-workaround");
var ensureCreated = !args.Contains("--skip-init");

var cancelDelay = ensureCreated ? 2 : 5;
AnsiConsole.WriteLine($"⏱️ Canceling after {cancelDelay} ms");
if (useWorkaround)
{
    AnsiConsole.WriteLine($"✳️ Using issue #26 workaround ({nameof(FixSqlClientIssue26ExecutionStrategy)})");
}

// A few ms after ReaderExecutingAsync is a good timing to get an exception that is not TaskCanceledException: A task was canceled.
var interceptor = new DbCommandInterceptor(cancelDelay);
var cancellationToken = interceptor.CancellationToken;
var optionsBuilder = new DbContextOptionsBuilder<ChinookContext>()
    .AddInterceptors(interceptor)
    .UseSqlServer(sqlContainer.ConnectionString, useWorkaround ? sql => sql.ExecutionStrategy(FixSqlClientIssue26ExecutionStrategy.Create) : null);

await using var context = new ChinookContext(optionsBuilder.Options);
if (ensureCreated)
{
    // Without "initializing things" (dotnet run -- --skip-init) => System.InvalidOperationException: Operation cancelled by user.
    // When "initializing things" upfront (_ = await context.Database.EnsureCreatedAsync()) => Microsoft.Data.SqlClient.SqlException (0x80131904): A severe error occurred on the current command.  The results, if any, should be discarded.
    AnsiConsole.WriteLine("➡️ await context.Database.EnsureCreatedAsync()");
    _ = await context.Database.EnsureCreatedAsync();
}

try
{
    AnsiConsole.WriteLine("➡️ context.Tracks.CountAsync(cancellationToken)");
    var count = await context.Tracks.CountAsync(cancellationToken);
    AnsiConsole.WriteLine($"✅ {count} tracks");
}
catch (OperationCanceledException exception)
{
    AnsiConsole.WriteLine($"⚪️ {exception.Message}");
    if (exception.InnerException != null)
    {
        AnsiConsole.WriteException(exception.InnerException, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
    }
}
catch (Exception exception)
{
    AnsiConsole.WriteLine("❌ An unexpected exception occured");
    AnsiConsole.WriteException(exception, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
}
