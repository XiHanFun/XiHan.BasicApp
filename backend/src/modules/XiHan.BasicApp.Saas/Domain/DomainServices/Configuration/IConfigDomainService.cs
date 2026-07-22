// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
