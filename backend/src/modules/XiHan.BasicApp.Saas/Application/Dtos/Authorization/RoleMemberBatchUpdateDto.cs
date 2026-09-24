// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色成员批量变更 DTO（以角色为中心，一次性提交加入与移出）
/// </summary>
public sealed class RoleMemberBatchUpdateDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 加入的成员（用户主键）
    /// </summary>
    public List<long> GrantUserIds { get; set; } = [];

    /// <summary>
    /// 移出的授权记录（用户角色绑定主键）
    /// </summary>
    public List<long> RevokeUserRoleIds { get; set; } = [];
}
