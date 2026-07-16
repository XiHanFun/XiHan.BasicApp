#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowPermissionCodes
// Guid:7a3e91c5-d260-4b84-9f17-e05c83d64ab2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Workflow.Domain.Permissions;

/// <summary>
/// 工作流权限编码常量
/// </summary>
/// <remarks>
/// 三处（本常量、SysResourceSeeder.ResourceCode、SysPermissionSeeder 目标字典）必须一致，否则鉴权 403。
/// 待办办理接口不设权限码（登录即可），受理人归属校验由任务服务在实例锁内执行。
/// </remarks>
public static class WorkflowPermissionCodes
{
    /// <summary>
    /// 资源编码
    /// </summary>
    public const string Resource = "workflow";

    /// <summary>
    /// 查看（定义/实例列表与详情）
    /// </summary>
    public const string Read = "workflow:read";

    /// <summary>
    /// 创建（定义草稿/新版本）
    /// </summary>
    public const string Create = "workflow:create";

    /// <summary>
    /// 更新（草稿编辑/发布/停用/归档/实例挂起恢复）
    /// </summary>
    public const string Update = "workflow:update";

    /// <summary>
    /// 删除（草稿定义）
    /// </summary>
    public const string Delete = "workflow:delete";

    /// <summary>
    /// 执行（发起实例/取消/终止/重试/发布信号）
    /// </summary>
    public const string Execute = "workflow:execute";
}
