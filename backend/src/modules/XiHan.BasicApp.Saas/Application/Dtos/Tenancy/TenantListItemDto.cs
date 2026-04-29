#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantListItemDto
// Guid:a8018220-96ea-45e3-a59c-0ae5a2d2bb37
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户列表项 DTO
/// </summary>
public sealed class TenantListItemDto : BasicAppDto
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户名称
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 版本/套餐主键
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus ConfigStatus { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 用户数限制
    /// </summary>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 存储空间限制(MB)
    /// </summary>
    public long? StorageLimit { get; set; }

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
