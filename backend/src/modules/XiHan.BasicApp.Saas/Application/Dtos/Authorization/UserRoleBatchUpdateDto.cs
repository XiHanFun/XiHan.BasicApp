// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户角色批量变更 DTO（一次性提交本次授权改动）
/// </summary>
public sealed class UserRoleBatchUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 待授予的角色主键集合
    /// </summary>
    public List<long> GrantRoleIds { get; set; } = [];

    /// <summary>
    /// 待撤销的用户角色记录主键集合
    /// </summary>
    public List<long> RevokeUserRoleIds { get; set; } = [];
}
