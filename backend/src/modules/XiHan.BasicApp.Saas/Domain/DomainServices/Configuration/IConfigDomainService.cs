#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConfigDomainService
// Guid:7c997735-2143-4cab-b9d5-c5279eb412f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 参数配置领域服务
/// </summary>
public interface IConfigDomainService
{
    /// <summary>
    /// 创建参数配置
    /// </summary>
    Task<ConfigCommandResult> CreateConfigAsync(ConfigCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除参数配置
    /// </summary>
    Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新参数配置
    /// </summary>
    Task<ConfigCommandResult> UpdateConfigAsync(ConfigUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新参数配置状态
    /// </summary>
    Task<ConfigCommandResult> UpdateConfigStatusAsync(ConfigStatusChangeCommand command, CancellationToken cancellationToken = default);
}
