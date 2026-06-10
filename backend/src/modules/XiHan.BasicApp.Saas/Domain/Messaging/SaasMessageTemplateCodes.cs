#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasMessageTemplateCodes
// Guid:fb047c9c-d03b-46e7-c489-bfca3fa00dc4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
        /// 找回密码临时密码邮件（变量：temporary_password/brand）
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
    }
}
