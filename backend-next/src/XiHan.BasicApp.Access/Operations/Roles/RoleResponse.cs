// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 角色的响应形态：列表项、创建 / 修改的响应与详情里的基本信息共用。
/// </summary>
/// <param name="Id">角色 ID（也是令牌 <c>role</c> 声明与授予主体的值）。</param>
/// <param name="OwnerTenantId">所属侧：<c>platform</c> 或租户 ID。</param>
/// <param name="Code">角色码。</param>
/// <param name="Name">显示名。</param>
/// <param name="Description">说明。</param>
/// <param name="Order">排序号。</param>
/// <param name="IsBuiltIn">是否内建。</param>
/// <param name="Status">状态。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record RoleResponse(
    Guid Id,
    string OwnerTenantId,
    string Code,
    string Name,
    string? Description,
    int Order,
    bool IsBuiltIn,
    RoleStatus Status,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static RoleResponse From(SysRole role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleResponse(role.Id, role.OwnerTenantId, role.Code, role.Name, role.Description, role.Order, role.IsBuiltIn, role.Status, role.CreatedAt);
    }
}
