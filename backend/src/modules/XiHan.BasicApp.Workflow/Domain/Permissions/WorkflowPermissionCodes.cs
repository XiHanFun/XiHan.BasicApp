// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Workflow.Domain.Permissions;

/// <summary>
/// 工作流权限编码常量
/// </summary>
/// <remarks>
/// 权限目录 <c>WorkflowPermissionCatalogSeeder</c> 按「资源 × 操作」声明，声明出的码必须与这里一一对应，否则鉴权 403。
/// 待办办理接口不设权限码（登录即可），受理人归属校验由任务服务在实例锁内执行。
/// </remarks>
public static class WorkflowPermissionCodes
{
    /// <summary>
    /// 模块编码
    /// </summary>
    public const string Module = "workflow";

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
