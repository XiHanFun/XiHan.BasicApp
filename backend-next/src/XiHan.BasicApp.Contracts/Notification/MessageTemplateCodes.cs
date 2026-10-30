// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Notification;

/// <summary>
/// 跨包发送用的消息模板编码。
/// </summary>
/// <remarks>
/// 平台种子由 Notification 写入（4.x 同）；发送方经 <see cref="IMessageSender"/> 按编码取模板，当前一侧启用的同编码模板优先、没有取平台的。
/// 变量：<c>code</c> 验证码、<c>minutes</c> 有效分钟数、<c>brand</c> 品牌名（发送方不给即由 Notification 补上）；验证类的另有 <c>title</c> 用途标题。
/// </remarks>
public static class MessageTemplateCodes
{
    /// <summary>
    /// 登录验证码邮件。
    /// </summary>
    public const string EmailLoginCode = "auth-email-login-code";

    /// <summary>
    /// 验证码邮件（验证、换绑与两步验证）。
    /// </summary>
    public const string EmailVerificationCode = "auth-email-verification-code";

    /// <summary>
    /// 登录验证码短信。
    /// </summary>
    public const string SmsLoginCode = "auth-sms-login-code";

    /// <summary>
    /// 验证码短信（验证、换绑与两步验证）。
    /// </summary>
    public const string SmsVerificationCode = "auth-sms-verification-code";
}
