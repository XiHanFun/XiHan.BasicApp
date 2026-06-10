#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasCacheKeys
// Guid:5026e4bd-d6fe-43f8-9c3d-530e920b0210
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块业务缓存键构造器。
/// </summary>
public static class SaasCacheKeys
{
    /// <summary>
    /// 配置值缓存键。
    /// </summary>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="configKey">配置键。</param>
    /// <returns>业务缓存键。</returns>
    public static string ConfigValue(long? tenantId, string configKey)
    {
        var tenantSegment = tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value.ToString() : "platform";
        return $"tenant:{tenantSegment}:key:{SaasConfigKeys.Normalize(configKey)}";
    }

    /// <summary>
    /// 指定配置键的所有租户缓存匹配模式。
    /// </summary>
    /// <param name="configKey">配置键。</param>
    /// <returns>业务缓存键匹配模式。</returns>
    public static string ConfigValuePattern(string configKey)
    {
        return $"tenant:*:key:{SaasConfigKeys.Normalize(configKey)}";
    }

    /// <summary>
    /// 所有配置值缓存匹配模式。
    /// </summary>
    public static string AllConfigValuesPattern()
    {
        return "tenant:*:key:*";
    }

    /// <summary>
    /// 用户授权快照缓存键（按 用户 × 租户上下文 双维隔离）。
    /// </summary>
    /// <remarks>
    /// 同一用户可能是多个租户的成员，角色/权限绑定按租户上下文生效；
    /// 仅按 userId 缓存会在切换租户后串味，故键必须包含当前租户维度（平台态记为 platform）。
    /// </remarks>
    /// <param name="tenantId">当前租户上下文（null/0 表示平台态）。</param>
    /// <param name="userId">用户标识。</param>
    /// <returns>业务缓存键。</returns>
    public static string AuthorizationSnapshot(long? tenantId, long userId)
    {
        var tenantSegment = tenantId is > 0 ? tenantId.Value.ToString() : "platform";
        return $"user:{userId}:tenant:{tenantSegment}";
    }

    /// <summary>
    /// 指定用户全部租户上下文的授权快照缓存匹配模式（授权变更后整体失效该用户）。
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <returns>业务缓存键匹配模式。</returns>
    public static string AuthorizationSnapshotPattern(long userId)
    {
        return $"user:{userId}:tenant:*";
    }

    /// <summary>
    /// 用户设置缓存键（按 用户 × 场景 × 设置键 隔离）。
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <param name="scene">设置场景。</param>
    /// <param name="settingKey">设置键（调用方已规范化）。</param>
    /// <returns>业务缓存键。</returns>
    public static string UserSetting(long userId, UserSettingScene scene, string settingKey)
    {
        return $"user:{userId}:scene:{(int)scene}:key:{settingKey}";
    }

    /// <summary>
    /// 指定用户的所有设置缓存匹配模式（写后整体失效该用户设置）。
    /// </summary>
    /// <param name="userId">用户标识。</param>
    /// <returns>业务缓存键匹配模式。</returns>
    public static string UserSettingPattern(long userId)
    {
        return $"user:{userId}:scene:*:key:*";
    }

    /// <summary>
    /// 菜单路由缓存键。
    /// </summary>
    /// <param name="permissionIds">权限标识集合。</param>
    /// <param name="hasAllPermissions">是否拥有全部权限。</param>
    /// <returns>业务缓存键。</returns>
    public static string MenuRoutes(IEnumerable<long> permissionIds, bool hasAllPermissions)
    {
        var source = hasAllPermissions
            ? "*"
            : string.Join(',', permissionIds.Distinct().OrderBy(static id => id));
        return $"permission-set:{Hash(source)}";
    }

    /// <summary>
    /// 可选全局权限选择项缓存键（仅无关键字时缓存，按模块/类型/上限区分）。
    /// </summary>
    /// <param name="moduleCode">模块编码。</param>
    /// <param name="permissionType">权限类型枚举值。</param>
    /// <param name="limit">数量上限。</param>
    /// <returns>业务缓存键。</returns>
    public static string PermissionSelect(string? moduleCode, int? permissionType, int limit)
    {
        var source = $"{(string.IsNullOrWhiteSpace(moduleCode) ? "all" : moduleCode.Trim())}|{(permissionType?.ToString() ?? "all")}|{limit}";
        return $"permission-select:{Hash(source)}";
    }

    /// <summary>
    /// 已启用角色选择项缓存键（仅无关键字时缓存，按类型/是否全局/上限区分）。
    /// </summary>
    /// <param name="roleType">角色类型枚举值。</param>
    /// <param name="isGlobal">是否全局。</param>
    /// <param name="limit">数量上限。</param>
    /// <returns>业务缓存键。</returns>
    public static string RoleSelect(int? roleType, bool? isGlobal, int limit)
    {
        var source = $"{(roleType?.ToString() ?? "all")}|{(isGlobal?.ToString() ?? "all")}|{limit}";
        return $"role-select:{Hash(source)}";
    }

    /// <summary>
    /// 已启用租户版本列表缓存键（平台级，全平台共享单键）。
    /// </summary>
    /// <returns>业务缓存键。</returns>
    public static string EnabledTenantEditions()
    {
        return "editions:enabled";
    }

    /// <summary>
    /// 可选全局资源选择项缓存键（仅无关键字时缓存，按类型/上限区分）。
    /// </summary>
    /// <param name="resourceType">资源类型枚举值。</param>
    /// <param name="limit">数量上限。</param>
    /// <returns>业务缓存键。</returns>
    public static string ResourceSelect(int? resourceType, int limit)
    {
        var source = $"{(resourceType?.ToString() ?? "all")}|{limit}";
        return $"resource-select:{Hash(source)}";
    }

    /// <summary>
    /// 可选全局操作选择项缓存键（仅无关键字时缓存，按类型/分类/上限区分）。
    /// </summary>
    /// <param name="operationTypeCode">操作类型编码枚举值。</param>
    /// <param name="category">操作分类枚举值。</param>
    /// <param name="limit">数量上限。</param>
    /// <returns>业务缓存键。</returns>
    public static string OperationSelect(int? operationTypeCode, int? category, int limit)
    {
        var source = $"{(operationTypeCode?.ToString() ?? "all")}|{(category?.ToString() ?? "all")}|{limit}";
        return $"operation-select:{Hash(source)}";
    }

    /// <summary>
    /// 部门树缓存键（仅无关键字时缓存，按租户隔离 + 是否仅启用/上限区分）。
    /// </summary>
    /// <param name="tenantId">租户标识。</param>
    /// <param name="onlyEnabled">是否仅含启用部门。</param>
    /// <param name="limit">数量上限。</param>
    /// <returns>业务缓存键。</returns>
    public static string DepartmentTree(long? tenantId, bool onlyEnabled, int limit)
    {
        var tenantSegment = tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value.ToString() : "platform";
        return $"tenant:{tenantSegment}:dept-tree:{Hash($"{onlyEnabled}|{limit}")}";
    }

    /// <summary>
    /// 消息模板缓存键（按 租户 × 渠道 × 模板编码 隔离；platform 表示全局上下文）。
    /// </summary>
    /// <param name="tenantId">当前租户上下文（null/0 为平台态）。</param>
    /// <param name="channel">消息渠道枚举值。</param>
    /// <param name="templateCode">模板编码（调用方已规范化）。</param>
    /// <returns>业务缓存键。</returns>
    public static string MessageTemplate(long? tenantId, int channel, string templateCode)
    {
        var tenantSegment = tenantId is > 0 ? tenantId.Value.ToString() : "platform";
        return $"tenant:{tenantSegment}:channel:{channel}:code:{templateCode}";
    }

    /// <summary>
    /// 全部消息模板缓存匹配模式（模板写路径整体失效）。
    /// </summary>
    public static string AllMessageTemplatesPattern()
    {
        return "tenant:*:channel:*:code:*";
    }

    /// <summary>
    /// 版本门控缓存键（租户 → 版本权限白名单）。
    /// </summary>
    /// <param name="tenantId">租户标识。</param>
    /// <returns>业务缓存键。</returns>
    public static string EditionGate(long tenantId)
    {
        return $"tenant:{tenantId}";
    }

    /// <summary>
    /// 全部版本门控缓存匹配模式（版本权限/租户版本变更后整体失效）。
    /// </summary>
    public static string AllEditionGatesPattern()
    {
        return "tenant:*";
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..24].ToLowerInvariant();
    }
}
