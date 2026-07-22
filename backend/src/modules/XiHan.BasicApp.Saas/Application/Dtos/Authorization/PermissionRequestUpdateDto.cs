// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限申请更新 DTO
/// </summary>
public sealed class PermissionRequestUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 角色主键
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 申请原因
    /// </summary>
    public string RequestReason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间
    /// </summary>
    public DateTimeOffset? ExpectedEffectiveTime { get; set; }

    /// <summary>
    /// 期望失效时间
    /// </summary>
    public DateTimeOffset? ExpectedExpirationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
