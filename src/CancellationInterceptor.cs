using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace efmssql;

public class CancellationInterceptor(int? cancelDelay) : IDbCommandInterceptor
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (cancelDelay.HasValue)
        {
            _cancellationTokenSource.CancelAfter(cancelDelay.Value);
        }
        return ValueTask.FromResult(result);
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
}
