#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceRepository
// Guid:9eac0f17-8060-413a-939f-f0fa13fdc0f3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 资源仓储实现
/// </summary>
public sealed class ResourceRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysResource>(clientResolver, unitOfWorkManager), IResourceRepository
{
    /// <inheritdoc />
    public async Task<SysResource?> GetByCodeAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(resource => resource.ResourceCode == resourceCode)
            .FirstAsync(cancellationToken);
    }
}
