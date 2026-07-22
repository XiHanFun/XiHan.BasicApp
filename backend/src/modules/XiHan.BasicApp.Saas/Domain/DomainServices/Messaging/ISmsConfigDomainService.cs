// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 短信网关配置领域服务
/// </summary>
public interface ISmsConfigDomainService
{
    /// <summary>
    /// 创建短信网关配置
    /// </summary>
    Task<SmsConfigCommandResult> CreateSmsConfigAsync(SmsConfigCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信网关配置
    /// </summary>
    Task<SmsConfigCommandResult> UpdateSmsConfigAsync(SmsConfigUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信网关配置启用状态
    /// </summary>
    Task<SmsConfigCommandResult> UpdateSmsConfigStatusAsync(SmsConfigStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认短信网关配置
    /// </summary>
    Task<SmsConfigCommandResult> SetDefaultSmsConfigAsync(SmsConfigDefaultChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除短信网关配置
    /// </summary>
    Task<SmsConfigCommandResult> DeleteSmsConfigAsync(long id, CancellationToken cancellationToken = default);
}
