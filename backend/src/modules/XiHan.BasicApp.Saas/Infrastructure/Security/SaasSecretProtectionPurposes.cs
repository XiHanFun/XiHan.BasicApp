#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasSecretProtectionPurposes
// Guid:5c1a7e42-0b9d-4f83-9a6e-14d2c8b7f350
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 10:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
}
