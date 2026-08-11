// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 打印模板 SqlSugar 仓储实现，显式过滤所属租户以隔离全局与私有模板。
/// </summary>
/// <param name="clientResolver">按当前租户上下文解析数据库连接的客户端解析器。</param>
public sealed class PrintTemplateRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPrintTemplate>(clientResolver), IPrintTemplateRepository
{
    /// <inheritdoc />
    public async Task<SysPrintTemplate?> FindByCodeInScopeAsync(
        long ownerTenantId,
        string templateCode,
        bool enabledOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();

        // 默认多租户过滤可能同时暴露全局与当前租户数据，因此必须追加精确所属租户条件。
        var query = CreateQueryable()
            .Where(template => template.TenantId == ownerTenantId && template.TemplateCode == templateCode);
        if (enabledOnly)
        {
            query = query.Where(template => template.Status == EnableStatus.Enabled);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysPrintTemplate?> FindByIdInScopeAsync(
        long ownerTenantId,
        long id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await CreateQueryable()
            .Where(template => template.TenantId == ownerTenantId && template.BasicId == id)
            .FirstAsync(cancellationToken);
    }
}
