#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasCacheNames
// Guid:08ee4016-0b6a-4d44-93ee-52c1c7843ea7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块缓存名称。
/// </summary>
public static class SaasCacheNames
{
    /// <summary>
    /// 配置值缓存。
    /// </summary>
    public const string ConfigValue = "basicapp:saas:config:value";

    /// <summary>
    /// 用户授权快照缓存。
    /// </summary>
    public const string AuthorizationSnapshot = "basicapp:saas:auth:snapshot";

    /// <summary>
    /// 菜单路由缓存。
    /// </summary>
    public const string MenuRoutes = "basicapp:saas:navigation:routes";

    /// <summary>
    /// 可选全局权限选择项缓存。
    /// </summary>
    public const string PermissionSelect = "basicapp:saas:permission:select";

    /// <summary>
    /// 已启用角色选择项缓存。
    /// </summary>
    public const string RoleSelect = "basicapp:saas:role:select";

    /// <summary>
    /// 已启用租户版本列表缓存。
    /// </summary>
    public const string TenantEditions = "basicapp:saas:tenancy:editions";

    /// <summary>
    /// 可选全局资源选择项缓存。
    /// </summary>
    public const string ResourceSelect = "basicapp:saas:resource:select";

    /// <summary>
    /// 可选全局操作选择项缓存。
    /// </summary>
    public const string OperationSelect = "basicapp:saas:operation:select";

    /// <summary>
    /// 部门树缓存。
    /// </summary>
    public const string DepartmentTree = "basicapp:saas:organization:dept-tree";

    /// <summary>
    /// 用户设置缓存（全场景偏好/页面设置同步）。
    /// </summary>
    public const string UserSetting = "basicapp:saas:user:setting";

    /// <summary>
    /// 消息模板缓存（渠道+编码 → 模板内容，发送链路高频读取）。
    /// </summary>
    public const string MessageTemplate = "basicapp:saas:message:template";

    /// <summary>
    /// 版本门控缓存（租户 → 版本权限白名单，鉴权快照热路径）。
    /// </summary>
    public const string EditionGate = "basicapp:saas:tenancy:edition-gate";

    /// <summary>
    /// 字典项树缓存（字典驱动的下拉/选项高频读取）。
    /// </summary>
    public const string DictItemTree = "basicapp:saas:configuration:dict-tree";

    /// <summary>
    /// Telegram 会话多步交互状态缓存（botName:chatId:userId → 状态）。
    /// </summary>
    public const string TelegramConversationState = "basicapp:saas:bot:telegram-conversation";

    /// <summary>
    /// Telegram Update 幂等去重键前缀（原生 Redis SET NX，非 IDistributedCache 通道）。
    /// </summary>
    public const string TelegramUpdateDedup = "basicapp:saas:bot:telegram-dedup";
}
