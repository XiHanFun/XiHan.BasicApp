// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Messaging;

/// <summary>
/// SaaS 消息模板编码常量（单一事实源）
/// </summary>
/// <remarks>
/// 与 SysMessageTemplate.TemplateCode 对应：发送方以常量引用，种子按常量落地，
/// 租户可创建同编码模板覆盖全局默认。新增模板编码必须在此登记，禁止散落魔法字符串。
/// </remarks>
public static class SaasMessageTemplateCodes
{
    /// <summary>
    /// 认证流程模板
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// 登录验证码邮件（变量：code/minutes/brand）
        /// </summary>
        public const string EmailLoginCode = "auth-email-login-code";

        /// <summary>
        /// 验证码邮件：绑定/换绑/两步验证通用（变量：code/minutes/brand/title）
        /// </summary>
        public const string EmailVerificationCode = "auth-email-verification-code";

        /// <summary>
        /// 找回密码邮件：携带一次性重置链接（变量：reset_url/brand）
        /// </summary>
        public const string PasswordReset = "auth-password-reset";

        /// <summary>
        /// 注册欢迎邮件（变量：user_name/brand）
        /// </summary>
        public const string Welcome = "auth-welcome";

        /// <summary>
        /// 登录验证码短信（变量：code/minutes/brand）
        /// </summary>
        public const string SmsLoginCode = "auth-sms-login-code";

        /// <summary>
        /// 验证码短信：绑定/换绑/两步验证通用（变量：code/minutes/brand/title）
        /// </summary>
        public const string SmsVerificationCode = "auth-sms-verification-code";
    }

    /// <summary>
    /// 系统通知多渠道扇出模板
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// 通知邮件（变量：title/content/brand）；模板缺失回退通知纯内容
        /// </summary>
        public const string Email = "notification-email";

        /// <summary>
        /// 通知短信（变量：title/brand）；云厂商发送须在短信配置 TemplateMap 中登记该编码到服务商模板码的映射
        /// </summary>
        public const string Sms = "notification-sms";
    }
}
