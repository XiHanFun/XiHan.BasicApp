// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户数据范围授权 DTO
/// </summary>
public sealed class UserDataScopeGrantDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门主键（用户级 Custom 范围绑定的部门；用户的范围档位由 SysUser.DataScopeOverride 控制）
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 是否包含子部门
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
