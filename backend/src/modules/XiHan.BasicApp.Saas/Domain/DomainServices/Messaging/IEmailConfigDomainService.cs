// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 邮件网关配置领域服务
/// </summary>
public interface IEmailConfigDomainService
{
    /// <summary>
    /// 创建邮件网关配置
    /// </summary>
    Task<EmailConfigCommandResult> CreateEmailConfigAsync(EmailConfigCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件网关配置
    /// </summary>
    Task<EmailConfigCommandResult> UpdateEmailConfigAsync(EmailConfigUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件网关配置启用状态
    /// </summary>
    Task<EmailConfigCommandResult> UpdateEmailConfigStatusAsync(EmailConfigStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认邮件网关配置
    /// </summary>
    Task<EmailConfigCommandResult> SetDefaultEmailConfigAsync(EmailConfigDefaultChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除邮件网关配置
    /// </summary>
    Task<EmailConfigCommandResult> DeleteEmailConfigAsync(long id, CancellationToken cancellationToken = default);
}
