// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 平台级权限划分（单一事实源）
/// </summary>
/// <remarks>
/// 集中维护「平台专属」权限码并界定「可授予租户」的范围，供多处复用，避免各种子复制粘贴造成口径漂移：
/// - 租户版本(Enterprise)白名单排除：全部权限减去 <see cref="PlatformOnlyCodes"/>；
/// - 租户管理员(tenant_admin)授权：仅 Saas 模块自身权限再减去平台专属（见 <see cref="IsTenantGrantable"/>）。
/// 外部模块的平台专属码经 <see cref="ContributePlatformOnly"/> 在模块 ConfigureServices 阶段登记，
/// 使版本白名单与租户授权对其保持同一排除口径。
/// <para>
/// 平台专属码只在平台上下文生效（<see cref="IsEffectiveIn"/>）：即便持有者带通配权限进入租户，也调不动平台接口、看不到平台菜单。
/// </para>
/// </remarks>
public static class SaasPlatformPermissions
{
    private static readonly HashSet<string> _platformOnlyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 租户目录是平台数据：SysTenant 行都在 0 号且不走租户过滤，租户持有查看码就能读到全部租户
        SaasPermissionCodes.Tenant.Read,
        SaasPermissionCodes.Tenant.Export,
        SaasPermissionCodes.Tenant.SupportMember,
        SaasPermissionCodes.Tenant.Create,
        SaasPermissionCodes.Tenant.Update,
        SaasPermissionCodes.Tenant.Status,
        SaasPermissionCodes.Tenant.Delete,
        // InitDb 按租户主键操作且不校验调用方归属，拿到它就能初始化任意租户的独立库，只能留在平台侧
        SaasPermissionCodes.Tenant.InitDb,
        SaasPermissionCodes.TenantEdition.Read,
        SaasPermissionCodes.TenantEdition.Create,
        SaasPermissionCodes.TenantEdition.Update,
        SaasPermissionCodes.TenantEdition.Status,
        SaasPermissionCodes.TenantEdition.Default,
        SaasPermissionCodes.TenantEdition.Export,
        SaasPermissionCodes.TenantEditionPermission.Read,
        SaasPermissionCodes.TenantEditionPermission.Grant,
        SaasPermissionCodes.TenantEditionPermission.Update,
        SaasPermissionCodes.TenantEditionPermission.Revoke,
        SaasPermissionCodes.Resource.Create,
        SaasPermissionCodes.Resource.Update,
        SaasPermissionCodes.Resource.Status,
        SaasPermissionCodes.Resource.Delete,
        SaasPermissionCodes.Operation.Create,
        SaasPermissionCodes.Operation.Update,
        SaasPermissionCodes.Operation.Status,
        SaasPermissionCodes.Operation.Delete,
        SaasPermissionCodes.Menu.Create,
        SaasPermissionCodes.Menu.Update,
        SaasPermissionCodes.Menu.Status,
        SaasPermissionCodes.Menu.Delete,
        SaasPermissionCodes.Cache.Read,
        SaasPermissionCodes.Cache.Clear,
        SaasPermissionCodes.Server.Read,
        SaasPermissionCodes.Numbering.GlobalManage,
        SaasPermissionCodes.Impersonation.CrossTenant,
        // 任务调度是平台运维能力：调度器按任务编码全局登记，任务与执行日志都是平台数据；
        // 需要逐租户处理的数据维护由平台任务在任务内部逐租户切入完成
        SaasPermissionCodes.Task.Read,
        SaasPermissionCodes.Task.Create,
        SaasPermissionCodes.Task.Update,
        SaasPermissionCodes.Task.Status,
        SaasPermissionCodes.Task.RunStatus,
        SaasPermissionCodes.Task.Delete,
        SaasPermissionCodes.Task.Export,
        SaasPermissionCodes.TaskLog.Read
    };

    /// <summary>
    /// 平台专属权限码：仅平台超级管理员可拥有，租户管理员的「全部权限」与企业版白名单均排除之。
    /// </summary>
    public static IReadOnlySet<string> PlatformOnlyCodes => _platformOnlyCodes;

    /// <summary>
    /// 登记外部模块的平台专属权限码（模块 ConfigureServices 阶段调用，幂等）。
    /// </summary>
    /// <param name="codes">平台专属权限码</param>
    public static void ContributePlatformOnly(params string[] codes)
    {
        foreach (var code in codes)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                _ = _platformOnlyCodes.Add(code);
            }
        }
    }

    /// <summary>
    /// 权限码在给定上下文是否生效：平台专属码只在平台上下文生效，其余两侧都生效
    /// </summary>
    /// <param name="code">权限码</param>
    /// <param name="isPlatformContext">当前是否平台上下文</param>
    public static bool IsEffectiveIn(string code, bool isPlatformContext)
    {
        return isPlatformContext || !PlatformOnlyCodes.Contains(code.Trim());
    }

    /// <summary>
    /// 该权限码是否可授予租户：仅 Saas 模块自身权限（以模块前缀界定）且非平台专属。
    /// </summary>
    /// <remarks>
    /// 以 Saas 模块前缀界定，天然排除其它模块的权限——外部模块的租户默认授权由各模块自己的角色权限种子承载。
    /// </remarks>
    /// <param name="code">权限码</param>
    public static bool IsTenantGrantable(string code)
    {
        return code.StartsWith(SaasPermissionCodes.Module + ":", StringComparison.OrdinalIgnoreCase)
            && !PlatformOnlyCodes.Contains(code);
    }
}
