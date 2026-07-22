// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 租户版本权限授权 DTO
/// </summary>
public sealed class TenantEditionPermissionGrantDto
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
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
