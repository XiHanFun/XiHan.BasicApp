#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasPermissionGroups
// Guid:5c7e1a93-2d6b-4f08-91a4-7b3c5d9e0f21
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Permissions;

/// <summary>
/// SaaS 权限功能分组定义（单一事实源）
/// </summary>
/// <remarks>
/// 把扁平的权限码按「资源/功能块」归组：权限码形如 saas:{resource}:{action}，以 {resource} 作为组码（GroupCode），
/// 配上中文显示名（GroupName）。权限分配 UI 据此把权限按功能块展示，而非一长串平铺。
/// 新增资源时在此补一行；未登记的资源回退用组码本身作为显示名。
/// </remarks>
public static class SaasPermissionGroups
{
    /// <summary>
    /// 资源组码 → 组显示名
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> GroupNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["tenant"] = "租户",
        ["tenant-member"] = "租户成员",
        ["tenant-edition"] = "租户版本",
        ["tenant-edition-permission"] = "租户版本权限",
        ["permission"] = "权限定义",
        ["resource"] = "资源定义",
        ["operation"] = "操作定义",
        ["menu"] = "菜单",
        ["department"] = "部门",
        ["user"] = "用户",
        ["user-security"] = "用户安全",
        ["user-session"] = "用户会话",
        ["session-role"] = "会话角色",
        ["user-statistics"] = "用户统计",
        ["password-history"] = "密码历史",
        ["external-login"] = "第三方登录",
        ["oauth-app"] = "OAuth 应用",
        ["oauth-code"] = "OAuth 授权码",
        ["oauth-token"] = "OAuth Token",
        ["access-log"] = "访问日志",
        ["api-log"] = "API 日志",
        ["diff-log"] = "数据变更日志",
        ["exception-log"] = "异常日志",
        ["login-log"] = "登录日志",
        ["operation-log"] = "操作日志",
        ["permission-change-log"] = "权限变更日志",
        ["task"] = "系统任务",
        ["task-log"] = "任务日志",
        ["review"] = "系统审查",
        ["review-log"] = "审查日志",
        ["config"] = "系统配置",
        ["dict"] = "系统字典",
        ["version"] = "系统版本",
        ["file"] = "系统文件",
        ["message"] = "系统消息",
        ["message-template"] = "消息模板",
        ["notification"] = "系统通知",
        ["storage-config"] = "存储配置",
        ["cache"] = "缓存管理",
        ["server"] = "服务监控",
        ["user-department"] = "用户部门",
        ["role"] = "角色",
        ["role-hierarchy"] = "角色继承",
        ["role-data-scope"] = "角色数据范围",
        ["role-permission"] = "角色权限",
        ["user-role"] = "用户角色",
        ["user-permission"] = "用户直授权限",
        ["permission-condition"] = "权限条件",
        ["user-data-scope"] = "用户数据范围",
        ["field-level-security"] = "字段级安全",
        ["permission-delegation"] = "权限委托",
        ["permission-request"] = "权限申请",
        ["constraint-rule"] = "约束规则"
    };

    /// <summary>
    /// 取权限码的组码（资源段）：saas:{resource}:{action} → resource；无法解析时回退模块段
    /// </summary>
    public static string ResolveGroupCode(string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return "other";
        }

        var parts = permissionCode.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? parts[1] : (parts.Length > 0 ? parts[0] : "other");
    }

    /// <summary>
    /// 取权限码的组显示名（未登记回退组码）
    /// </summary>
    public static string ResolveGroupName(string? permissionCode)
    {
        var groupCode = ResolveGroupCode(permissionCode);
        return GroupNameMap.TryGetValue(groupCode, out var name) ? name : groupCode;
    }
}
