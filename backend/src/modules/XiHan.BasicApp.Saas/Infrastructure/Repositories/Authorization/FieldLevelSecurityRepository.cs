// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 字段级安全仓储实现
/// </summary>
public sealed class FieldLevelSecurityRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysFieldLevelSecurity>(clientResolver), IFieldLevelSecurityRepository
{
    /// <summary>
    /// 取某实体上已启用的规则（租户过滤本身就放行平台行）
    /// </summary>
    public async Task<IReadOnlyList<SysFieldLevelSecurity>> GetEnabledByEntityAsync(string entityName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(rule => rule.EntityName == entityName && rule.Status == EnableStatus.Enabled)
            .ToListAsync(cancellationToken);
    }
}
