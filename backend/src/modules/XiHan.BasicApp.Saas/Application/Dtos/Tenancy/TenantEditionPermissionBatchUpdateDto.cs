// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本权限批量变更中的单条映射状态变更项
/// </summary>
public sealed class TenantEditionPermissionStatusItemDto
{
    /// <summary>
    /// 租户版本权限绑定主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 目标映射状态
    /// </summary>
    public ValidityStatus Status { get; set; }
}

/// <summary>
/// 租户版本权限批量变更输入
/// </summary>
public sealed class TenantEditionPermissionBatchUpdateDto
{
    /// <summary>
    /// 租户版本主键
    /// </summary>
    public long EditionId { get; set; }

    /// <summary>
    /// 待授予的权限主键集合
    /// </summary>
    public List<long> GrantPermissionIds { get; set; } = [];

    /// <summary>
    /// 待撤销的租户版本权限绑定主键集合
    /// </summary>
    public List<long> RevokeEditionPermissionIds { get; set; } = [];

    /// <summary>
    /// 待变更映射状态的绑定集合
    /// </summary>
    public List<TenantEditionPermissionStatusItemDto> StatusChanges { get; set; } = [];
}
