// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Access.Operations.Auth;
using XiHan.BasicApp.Access.Operations.Menus;
using XiHan.BasicApp.Access.Operations.Permissions;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.UserDataScopes;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包操作的请求 / 响应序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CreateRoleRequest))]
[JsonSerializable(typeof(GetRoleRequest))]
[JsonSerializable(typeof(UpdateRoleRequest))]
[JsonSerializable(typeof(RoleIdsRequest))]
[JsonSerializable(typeof(RolePermissionsRequest))]
[JsonSerializable(typeof(RoleMembersRequest))]
[JsonSerializable(typeof(RoleMembersQueryRequest))]
[JsonSerializable(typeof(RolesByUserRequest))]
[JsonSerializable(typeof(RolesByUserResponse))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(RoleResponse))]
[JsonSerializable(typeof(RoleDetailResponse))]
[JsonSerializable(typeof(PageResult<RoleResponse>))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(PermissionBatchResult))]
[JsonSerializable(typeof(PermissionRuleResponse))]
[JsonSerializable(typeof(RoleMemberWindowRequest))]
[JsonSerializable(typeof(RoleMemberResponse))]
[JsonSerializable(typeof(UserGrantsRequest))]
[JsonSerializable(typeof(UserPermissionsRequest))]
[JsonSerializable(typeof(UserGrantsResponse))]
[JsonSerializable(typeof(UserDataScopeRequest))]
[JsonSerializable(typeof(SetUserDataScopeRequest))]
[JsonSerializable(typeof(UserDataScopeResponse))]
[JsonSerializable(typeof(RoleMembersResponse))]
[JsonSerializable(typeof(ListPermissionsRequest))]
[JsonSerializable(typeof(PermissionCatalogResponse))]
[JsonSerializable(typeof(PermissionsRequest))]
[JsonSerializable(typeof(PermissionsResponse))]
[JsonSerializable(typeof(ListMenusRequest))]
[JsonSerializable(typeof(GetMenuRequest))]
[JsonSerializable(typeof(CreateMenuRequest))]
[JsonSerializable(typeof(UpdateMenuRequest))]
[JsonSerializable(typeof(ResetMenuRequest))]
[JsonSerializable(typeof(MenuIdsRequest))]
[JsonSerializable(typeof(MenuResponse))]
[JsonSerializable(typeof(MenusResponse))]
public sealed partial class AccessJsonContext : JsonSerializerContext;
