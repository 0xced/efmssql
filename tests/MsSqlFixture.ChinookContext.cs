using System;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;
using Xunit.Sdk;

namespace WorkaroundSqlClientIssue26.Tests;

public abstract class MsSqlFixture<T>(IMessageSink messageSink) : MsSqlFixture(messageSink)
        where T : SqlEngineDbContextOptionsBuilderBase<T>
{
    public ChinookContext CreateChinookContext(ITestOutputHelper output, Action<SqlEngineDbContextOptionsBuilderBase<T>> optionsAction)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ChinookContext>().LogTo(output.WriteLine, [CoreEventId.QueryCanceled, CoreEventId.QueryIterationFailed]);
        var options = ConfigureSqlServer(optionsBuilder, optionsAction).Options;
        return new ChinookContext(options);
    }

    protected abstract DbContextOptionsBuilder<ChinookContext> ConfigureSqlServer(DbContextOptionsBuilder<ChinookContext> builder, Action<SqlEngineDbContextOptionsBuilderBase<T>> optionsAction);

    public class SqlServer(IMessageSink messageSink) : MsSqlFixture<SqlServerDbContextOptionsBuilder>(messageSink)
    {
        protected override DbContextOptionsBuilder<ChinookContext> ConfigureSqlServer(DbContextOptionsBuilder<ChinookContext> builder, Action<SqlEngineDbContextOptionsBuilderBase<SqlServerDbContextOptionsBuilder>> optionsAction)
            => builder.UseSqlServer(ConnectionString, optionsAction);
    }

    public class AzureSql(IMessageSink messageSink) : MsSqlFixture<AzureSqlDbContextOptionsBuilder>(messageSink)
    {
        protected override DbContextOptionsBuilder<ChinookContext> ConfigureSqlServer(DbContextOptionsBuilder<ChinookContext> builder, Action<SqlEngineDbContextOptionsBuilderBase<AzureSqlDbContextOptionsBuilder>> optionsAction)
            => builder.UseAzureSql(ConnectionString, optionsAction);
    }

    public class AzureSynapse(IMessageSink messageSink) : MsSqlFixture<AzureSynapseDbContextOptionsBuilder>(messageSink)
    {
        protected override DbContextOptionsBuilder<ChinookContext> ConfigureSqlServer(DbContextOptionsBuilder<ChinookContext> builder, Action<SqlEngineDbContextOptionsBuilderBase<AzureSynapseDbContextOptionsBuilder>> optionsAction)
            => builder.UseAzureSynapse(ConnectionString, optionsAction);
    }
}