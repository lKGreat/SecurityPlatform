using Atlas.Core.Abstractions;
using SqlSugar;

namespace Atlas.Infrastructure;

/// <summary>
/// SqlSugar-backed unit of work that wraps operations in a database transaction.
/// </summary>
public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    private readonly ISqlSugarClient _db;

    public SqlSugarUnitOfWork(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var result = await _db.Ado.UseTranAsync(async () =>
        {
            await action();
        });

        if (!result.IsSuccess)
        {
            throw result.ErrorException ?? new InvalidOperationException("数据库事务执行失败。");
        }
    }
}
