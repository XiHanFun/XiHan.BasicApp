#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailSenderOptions
// Guid:b7d3e1a9-5c42-4f60-8e7a-1d9c4b6f2e80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 邮件发送（SMTP）配置，绑定配置节 <c>XiHan:Email</c>
/// </summary>
public sealed class EmailSenderOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "XiHan:Email";

    /// <summary>
    /// 是否启用真实 SMTP 发送；为 false 或缺少 <see cref="SmtpHost"/> 时不发送
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// SMTP 服务器地址
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 服务器端口
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 是否使用 SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 默认发件邮箱（信封未显式指定 FromEmail 时使用）
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 认证用户名（为空则不进行认证）
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 认证密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 是否已具备发送条件（启用且配置了 SMTP 主机）
    /// </summary>
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(SmtpHost);
}
