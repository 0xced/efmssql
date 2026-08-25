[assembly: System.CLSCompliant(false)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/0xced/WorkaroundSqlClientIssue26")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0", FrameworkDisplayName=".NET 10.0")]
namespace Microsoft.EntityFrameworkCore
{
    public static class SqlEngineDbContextOptionsBuilderWorkaroundSqlClientIssue26Extensions
    {
        public static void WorkAroundSqlClientIssue26<T>(this Microsoft.EntityFrameworkCore.Infrastructure.SqlEngineDbContextOptionsBuilderBase<T> builder)
            where T : Microsoft.EntityFrameworkCore.Infrastructure.SqlEngineDbContextOptionsBuilderBase<T> { }
    }
}