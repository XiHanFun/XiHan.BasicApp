// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字段级安全仓储实现
/// </summary>
public sealed class FieldLevelSecurityRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFieldLevelSecurity>(clientResolver), IFieldLevelSecurityRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysFieldLevelSecurity>> GetByResourceAndRoleAsync(long resourceId, long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(fls => fls.ResourceId == resourceId && fls.TargetType == FieldSecurityTargetType.Role && fls.TargetId == roleId)
            .ToListAsync(cancellationToken);
    }
}
