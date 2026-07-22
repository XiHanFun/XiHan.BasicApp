// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 消息模板仓储实现
/// </summary>
public sealed class MessageTemplateRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysMessageTemplate>(clientResolver), IMessageTemplateRepository
{
    /// <inheritdoc />
    public async Task<SysMessageTemplate?> FindEnabledByCodeAsync(MessageChannel channel, string templateCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();

        // 全局租户过滤已合并 (当前租户 OR 全局)；按 TenantId 降序使租户自定义模板优先于全局默认
        return await CreateQueryable()
            .Where(template => template.Channel == channel
                && template.TemplateCode == templateCode
                && template.Status == EnableStatus.Enabled)
            .OrderByDescending(template => template.TenantId)
            .FirstAsync(cancellationToken);
    }
}
