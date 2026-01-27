using Atlas.Application.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly ISqlSugarClient _db;

    public UserAccountRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        return _db.Insertable(account).ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        return _db.Updateable(account).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<UserAccount?> FindByUsernameAsync(TenantId tenantId, string username, CancellationToken cancellationToken)
    {
        var query = _db.Queryable<UserAccount>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Username == username);
        var result = await query.FirstAsync(cancellationToken);
        return result;
    }
}
