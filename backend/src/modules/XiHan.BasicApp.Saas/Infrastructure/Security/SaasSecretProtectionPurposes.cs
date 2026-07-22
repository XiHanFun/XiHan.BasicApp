// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// SaaS 模块 Data Protection 保护用途（隔离密钥环的稳定标识）。
/// </summary>
/// <remarks>
/// 集中定义各类可逆加密字段的 Purpose 与密文前缀，避免散落魔法字符串；
/// Purpose 一经上线不得随意变更（否则历史密文无法解密），如需轮换以 <c>.vN</c> 递增。
/// </remarks>
public static class SaasSecretProtectionPurposes
{
    /// <summary>
    /// 密文前缀标记：区分已加密值与历史明文。
    /// </summary>
    public const string CipherPrefix = "dp:";

    /// <summary>
    /// 存储配置密钥（SecretAccessKey）保护用途。
    /// </summary>
    public const string StorageSecretAccessKey = "XiHan.BasicApp.Saas.StorageConfig.SecretAccessKey.v1";

    /// <summary>
    /// 租户数据库连接字符串保护用途。
    /// </summary>
    public const string TenantConnectionString = "XiHan.BasicApp.Saas.Tenant.ConnectionString.v1";

    /// <summary>
    /// 短信网关访问密钥保护用途。
    /// </summary>
    public const string SmsConfigAccessKeySecret = "XiHan.BasicApp.Saas.SmsConfig.AccessKeySecret.v1";

    /// <summary>
    /// 邮件网关认证密码保护用途。
    /// </summary>
    public const string EmailConfigPassword = "XiHan.BasicApp.Saas.EmailConfig.Password.v1";

    /// <summary>
    /// 机器人配置签名秘钥保护用途（Webhook 型：钉钉/飞书）。
    /// </summary>
    public const string BotConfigSecret = "XiHan.BasicApp.Saas.BotConfig.Secret.v1";

    /// <summary>
    /// Telegram 机器人 Token 保护用途。
    /// </summary>
    public const string TelegramBotToken = "XiHan.BasicApp.Saas.TelegramBot.Token.v1";

    /// <summary>
    /// 系统配置加密值（IsEncrypted 行的 ConfigValue）保护用途。
    /// </summary>
    public const string ConfigValue = "XiHan.BasicApp.Saas.Config.Value.v1";

    /// <summary>
    /// AI Provider API 密钥保护用途。
    /// </summary>
    public const string AiProviderApiKey = "XiHan.BasicApp.Saas.AiProvider.ApiKey.v1";

    /// <summary>
    /// 用户 OpenAPI 凭证密钥（AppSecret）保护用途。
    /// </summary>
    /// <remarks>
    /// 与账号密码不同：开放接口 HMAC 验签需还原明文密钥参与运算，故此密钥可逆加密落库（非单向哈希）。
    /// </remarks>
    public const string UserApiCredentialSecret = "XiHan.BasicApp.Saas.UserApiCredential.Secret.v1";
}
