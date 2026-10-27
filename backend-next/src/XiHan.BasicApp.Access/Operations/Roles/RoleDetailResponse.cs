// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.get</c> 的响应：角色、它当前的权限码与成员数。
/// </summary>
/// <param name="Role">角色。</param>
/// <param name="Permissions">已授予的权限码，按码排序。</param>
/// <param name="CustomDepartments">自定义数据范围的部门（只有 DataScope 为 Custom 时非空）。</param>
/// <param name="MemberCount">成员数。</param>
public sealed record RoleDetailResponse(RoleResponse Role, IReadOnlyList<string> Permissions, long MemberCount, IReadOnlyList<DataScopeDepartmentRequest> CustomDepartments);
