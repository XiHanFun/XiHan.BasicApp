// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.Auth;
using XiHan.BasicApp.Access.Operations.ConstraintRules;
using XiHan.BasicApp.Access.Operations.Delegations;
using XiHan.BasicApp.Access.Operations.FieldSecurity;
using XiHan.BasicApp.Access.Operations.LoginBans;
using XiHan.BasicApp.Access.Operations.PermissionConditions;
using XiHan.BasicApp.Access.Operations.Policies;
using XiHan.BasicApp.Access.Operations.Menus;
using XiHan.BasicApp.Access.Operations.Permissions;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.SessionRoles;
using XiHan.BasicApp.Access.Operations.UserDataScopes;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 授权包操作的请求 / 响应序列化上下文。
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。
/// </remarks>
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
[JsonSerializable(typeof(FieldSecurityEntitiesRequest))]
[JsonSerializable(typeof(FieldSecurityEntitiesResponse))]
[JsonSerializable(typeof(GetFieldSecurityRuleRequest))]
[JsonSerializable(typeof(FieldSecurityRuleIdsRequest))]
[JsonSerializable(typeof(CreateFieldSecurityRuleRequest))]
[JsonSerializable(typeof(UpdateFieldSecurityRuleRequest))]
[JsonSerializable(typeof(FieldSecurityRuleResponse))]
[JsonSerializable(typeof(PageResult<FieldSecurityRuleResponse>))]
[JsonSerializable(typeof(List<FieldSecurityRuleView>))]
[JsonSerializable(typeof(SetRoleParentsRequest))]
[JsonSerializable(typeof(RoleHierarchyResponse))]
[JsonSerializable(typeof(CreateDelegationRequest))]
[JsonSerializable(typeof(RevokeDelegationRequest))]
[JsonSerializable(typeof(DelegationIdsRequest))]
[JsonSerializable(typeof(DelegationResponse))]
[JsonSerializable(typeof(PageResult<DelegationResponse>))]
[JsonSerializable(typeof(List<ConditionClauseView>))]
[JsonSerializable(typeof(ConditionAttributesRequest))]
[JsonSerializable(typeof(ConditionAttributesResponse))]
[JsonSerializable(typeof(PermissionConditionsRequest))]
[JsonSerializable(typeof(PermissionConditionsResponse))]
[JsonSerializable(typeof(CreatePermissionConditionRequest))]
[JsonSerializable(typeof(UpdatePermissionConditionRequest))]
[JsonSerializable(typeof(PermissionConditionIdsRequest))]
[JsonSerializable(typeof(PermissionConditionResponse))]
[JsonSerializable(typeof(ConditionTrialRequest))]
[JsonSerializable(typeof(ConditionTrialResponse))]
[JsonSerializable(typeof(ConditionBatchTrialRequest))]
[JsonSerializable(typeof(ConditionBatchTrialResponse))]
[JsonSerializable(typeof(ConditionInventoryRequest))]
[JsonSerializable(typeof(ConditionInventoryResponse))]
[JsonSerializable(typeof(List<AccessPolicyView>))]
[JsonSerializable(typeof(List<PolicyClauseView>))]
[JsonSerializable(typeof(AccessPolicyResponse))]
[JsonSerializable(typeof(PageResult<AccessPolicyResponse>))]
[JsonSerializable(typeof(GetAccessPolicyRequest))]
[JsonSerializable(typeof(CreateAccessPolicyRequest))]
[JsonSerializable(typeof(UpdateAccessPolicyRequest))]
[JsonSerializable(typeof(SetAccessPolicyActionRequest))]
[JsonSerializable(typeof(SetAccessPolicyStatusRequest))]
[JsonSerializable(typeof(AccessPolicyIdsRequest))]
[JsonSerializable(typeof(AccessPolicyAttributesRequest))]
[JsonSerializable(typeof(AccessPolicyTemplatesRequest))]
[JsonSerializable(typeof(AccessPolicyTemplatesResponse))]
[JsonSerializable(typeof(TrialAccessPolicyRequest))]
[JsonSerializable(typeof(AccessPolicyTrialResponse))]
[JsonSerializable(typeof(LoginAutoBanSettings))]
[JsonSerializable(typeof(List<LoginBanEntry>))]
[JsonSerializable(typeof(LoginBansRequest))]
[JsonSerializable(typeof(LoginBansResponse))]
[JsonSerializable(typeof(LiftLoginBanRequest))]
[JsonSerializable(typeof(LiftLoginBanResponse))]
[JsonSerializable(typeof(List<ConstraintRuleView>))]
[JsonSerializable(typeof(ConstraintRuleResponse))]
[JsonSerializable(typeof(PageResult<ConstraintRuleResponse>))]
[JsonSerializable(typeof(GetConstraintRuleRequest))]
[JsonSerializable(typeof(CreateConstraintRuleRequest))]
[JsonSerializable(typeof(UpdateConstraintRuleRequest))]
[JsonSerializable(typeof(SetConstraintRuleStatusRequest))]
[JsonSerializable(typeof(ConstraintRuleIdsRequest))]
[JsonSerializable(typeof(SessionRoleResponse))]
[JsonSerializable(typeof(PageResult<SessionRoleResponse>))]
[JsonSerializable(typeof(GetSessionRoleRequest))]
[JsonSerializable(typeof(SessionRolesRequest))]
[JsonSerializable(typeof(SessionRolesResponse))]
public sealed partial class AccessJsonContext : JsonSerializerContext;
