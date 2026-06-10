#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateRepository
// Guid:ac5f2d47-8be6-4192-af34-6dbc8a5bae79
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
