#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantUpdateDto
// Guid:8ef218f3-c4f4-46b8-a0cb-f7c9116ba88c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户更新 DTO
/// </summary>
public sealed class TenantUpdateDto : BasicAppUDto
{
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
    public TenantIsolationMode IsolationMode { get; set; } = TenantIsolationMode.Field;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }

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
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
