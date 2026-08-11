// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.Permissions;

/// <summary>
/// 打印模板权限码常量
/// </summary>
/// <remarks>
/// 常量、SysResourceSeeder.ResourceCode、SysPermissionSeeder 三处必须一致。
/// </remarks>
public static class PrintingPermissionCodes
{
    /// <summary>
    /// 模块编码
    /// </summary>
    public const string Module = "print-template";

    /// <summary>
    /// 资源编码
    /// </summary>
    public const string Resource = "print-template";

    /// <summary>查看打印模板。</summary>
    public const string Read = "print-template:read";

    /// <summary>创建打印模板。</summary>
    public const string Create = "print-template:create";

    /// <summary>更新模板元数据和设计 JSON。</summary>
    public const string Update = "print-template:update";

    /// <summary>启用或停用打印模板。</summary>
    public const string Status = "print-template:status";

    /// <summary>删除已经停用的打印模板。</summary>
    public const string Delete = "print-template:delete";

    /// <summary>按编码解析、预览或直接打印模板。</summary>
    public const string Use = "print-template:use";

    /// <summary>管理平台全局模板及租户开放状态（平台专属）。</summary>
    public const string GlobalManage = "print-template:global-manage";

    /// <summary>
    /// 全部权限码
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Read, Create, Update, Status, Delete, Use, GlobalManage
    ];

    /// <summary>
    /// 可授予租户的权限码（平台专属除外）
    /// </summary>
    public static readonly IReadOnlyList<string> TenantGrantable =
    [
        Read, Create, Update, Status, Delete, Use
    ];
}
