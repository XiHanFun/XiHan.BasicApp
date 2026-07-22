// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 邮件网关配置创建命令
/// </summary>
public sealed record EmailConfigCreateCommand(
    string ConfigCode,
    string ConfigName,
    string SmtpHost,
    int SmtpPort,
    bool UseSsl,
    bool AcceptInvalidCertificate,
    string FromEmail,
    string FromName,
    string? UserName,
    string? Password,
    bool IsBodyHtml,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// 邮件网关配置更新命令
/// </summary>
/// <remarks>Password 为空表示保留原密码（前端脱敏不回显）</remarks>
public sealed record EmailConfigUpdateCommand(
    long BasicId,
    string ConfigName,
    string SmtpHost,
    int SmtpPort,
    bool UseSsl,
    bool AcceptInvalidCertificate,
    string FromEmail,
    string FromName,
    string? UserName,
    string? Password,
    bool IsBodyHtml,
    int Sort,
    string? Remark);

/// <summary>
/// 邮件网关配置状态变更命令
/// </summary>
public sealed record EmailConfigStatusChangeCommand(long BasicId, bool IsEnabled);

/// <summary>
/// 邮件网关配置默认变更命令
/// </summary>
public sealed record EmailConfigDefaultChangeCommand(long BasicId);

/// <summary>
/// 邮件网关配置命令结果
/// </summary>
public sealed record EmailConfigCommandResult(SysEmailConfig Config);
