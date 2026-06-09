#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionPermissionListItemDto
// Guid:7de9112e-2758-407d-8a92-7b76e16c1e1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本权限列表项 DTO
/// </summary>
public sealed class TenantEditionPermissionListItemDto : BasicAppDto
{
    /// <summary>
    /// 租户版本主键
    /// </summary>
    public long EditionId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public PermissionType? PermissionType { get; set; }

    /// <summary>
    /// 模块编码
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// 是否全局权限
    /// </summary>
    public bool? IsGlobalPermission { get; set; }

    /// <summary>
    /// 权限是否需要审计
    /// </summary>
    public bool? IsRequireAudit { get; set; }

    /// <summary>
    /// 权限状态
    /// </summary>
    public EnableStatus? PermissionStatus { get; set; }

    /// <summary>
    /// 绑定状态
    /// </summary>
    public ValidityStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
