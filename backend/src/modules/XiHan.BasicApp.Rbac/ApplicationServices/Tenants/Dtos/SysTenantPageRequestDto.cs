#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantPageRequestDto
// Guid:f6a7b8c9-d0e1-2345-6789-0abcdef12346
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.ApplicationServices.Tenants.Dtos;

/// <summary>
/// 租户分页查询DTO
/// </summary>
public class SysTenantPageRequestDto : PageRequestDtoBase
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string? TenantCode { get; set; }

    /// <summary>
    /// 租户名称
    /// </summary>
    public string? TenantName { get; set; }

    /// <summary>
    /// 租户简称
    /// </summary>
    public string? TenantShortName { get; set; }

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 联系邮箱
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// 隔离模式
    /// </summary>
    public TenantIsolationMode? IsolationMode { get; set; }

    /// <summary>
    /// 配置状态
    /// </summary>
    public TenantConfigStatus? ConfigStatus { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus? TenantStatus { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
