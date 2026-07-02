#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEmailConfigRepository
// Guid:7d1f5b28-9c46-4e03-a7b1-3f8e6d2c5a90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 邮件网关配置仓储接口
/// </summary>
public interface IEmailConfigRepository : ISaasRepository<SysEmailConfig>
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    Task<SysEmailConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认且启用的邮件网关配置
    /// </summary>
    Task<SysEmailConfig?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
