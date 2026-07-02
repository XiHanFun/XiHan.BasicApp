#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsConfigDomainService
// Guid:2f5a9d14-7c68-4e03-b2f5-9a4d8e1c6b70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
