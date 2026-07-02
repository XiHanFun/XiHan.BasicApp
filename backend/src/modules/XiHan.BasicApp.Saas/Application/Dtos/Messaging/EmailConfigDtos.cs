#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigDtos
// Guid:7f2a4d81-3c69-4e50-b8a7-1d5e9c2f6b34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 邮件配置分页查询 DTO
/// </summary>
public sealed class EmailConfigPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（配置编码、名称、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否默认网关
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 邮件配置创建 DTO
/// </summary>
public sealed class EmailConfigCreateDto : BasicAppCDto
{
    /// <summary>
    /// 配置编码（租户内唯一）
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

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
    /// 是否接受无效/自签 TLS 证书（生产务必保持 false）
    /// </summary>
    public bool AcceptInvalidCertificate { get; set; }

    /// <summary>
    /// 默认发件邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名（兼作品牌名）
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 认证登录名（为空则不进行认证）
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// SMTP 认证密码（敏感字段，仅写入）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 默认是否使用 HTML 正文
    /// </summary>
    public bool IsBodyHtml { get; set; } = true;

    /// <summary>
    /// 是否默认网关
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 邮件配置更新 DTO
/// </summary>
public sealed class EmailConfigUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

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
    /// 是否接受无效/自签 TLS 证书（生产务必保持 false）
    /// </summary>
    public bool AcceptInvalidCertificate { get; set; }

    /// <summary>
    /// 默认发件邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名（兼作品牌名）
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 认证登录名（为空则不进行认证）
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// SMTP 认证密码（为空表示保留原密码）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 默认是否使用 HTML 正文
    /// </summary>
    public bool IsBodyHtml { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 邮件配置状态更新 DTO
/// </summary>
public sealed class EmailConfigStatusUpdateDto
{
    /// <summary>
    /// 邮件配置主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 邮件配置默认更新 DTO
/// </summary>
public sealed class EmailConfigDefaultUpdateDto
{
    /// <summary>
    /// 邮件配置主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 邮件配置列表项 DTO
/// </summary>
/// <remarks>Password 为敏感字段不下发，仅以 HasPassword 标识是否已配置</remarks>
public class EmailConfigListItemDto : BasicAppDto
{
    /// <summary>
    /// 配置编码
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 服务器地址
    /// </summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 服务器端口
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// 是否使用 SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// 默认发件邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名（兼作品牌名）
    /// </summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// 是否已配置认证密码
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// 是否默认网关
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 邮件配置详情 DTO
/// </summary>
/// <remarks>Password 为敏感字段不下发，仅以 HasPassword 标识是否已配置</remarks>
public sealed class EmailConfigDetailDto : EmailConfigListItemDto
{
    /// <summary>
    /// 是否接受无效/自签 TLS 证书
    /// </summary>
    public bool AcceptInvalidCertificate { get; set; }

    /// <summary>
    /// SMTP 认证登录名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 默认是否使用 HTML 正文
    /// </summary>
    public bool IsBodyHtml { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改人主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
