#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityDomainService
// Guid:874ca2e5-7544-4486-8799-a1a4d39f36ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 字段级安全领域服务
/// </summary>
public interface IFieldLevelSecurityDomainService
{
    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    Task<FieldLevelSecurityCommandResult> CreateAsync(FieldLevelSecurityCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    Task<FieldLevelSecurityCommandResult> UpdateAsync(FieldLevelSecurityUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    Task<FieldLevelSecurityCommandResult> UpdateStatusAsync(FieldLevelSecurityStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
