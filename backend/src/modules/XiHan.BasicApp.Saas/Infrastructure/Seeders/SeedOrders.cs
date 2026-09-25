// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 种子执行顺序：按阶段划段，后一阶段只依赖前面阶段写好的数据
/// </summary>
/// <remarks>
/// 框架把所有模块的种子按 Order 统一排序执行。同一阶段内按模块错开：
/// SaaS +0、代码生成 +10、AI +20、工作流 +30、聊天 +40、打印 +50；模块内再有多个种子时在自己的 10 个号里排。
/// </remarks>
public static class SeedOrders
{
    /// <summary>
    /// 平台身份：超级管理员角色与账号
    /// </summary>
    public const int PlatformIdentity = 100;

    /// <summary>
    /// 操作字典：资源型权限的动作维度（读取、创建、更新……）
    /// </summary>
    public const int Operations = 200;

    /// <summary>
    /// 权限目录：各模块的资源与权限（依赖操作字典）
    /// </summary>
    public const int PermissionCatalog = 300;

    /// <summary>
    /// 菜单：各模块的页面与按钮（依赖权限目录，菜单按权限码绑定可见性）
    /// </summary>
    public const int Menus = 400;

    /// <summary>
    /// 套餐：版本与功能白名单（依赖全部模块的权限目录）
    /// </summary>
    public const int Editions = 500;

    /// <summary>
    /// 其余运行所需的平台数据：参数、存储、消息模板、OAuth 应用、定时任务、代码生成模板
    /// </summary>
    public const int PlatformData = 600;

    /// <summary>
    /// 演示数据：开关开启时才写（依赖以上全部）
    /// </summary>
    public const int Demo = 900;
}
