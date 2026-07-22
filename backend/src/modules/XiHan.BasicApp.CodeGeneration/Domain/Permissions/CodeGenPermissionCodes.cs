// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Permissions;

/// <summary>
/// 代码生成权限码常量
/// </summary>
/// <remarks>
/// 与种子数据 <c>SysPermissionSeeder</c> 对齐：资源 <c>code_gen</c> + 操作 read/create/update/delete/export/import/execute。
/// </remarks>
public static class CodeGenPermissionCodes
{
    /// <summary>资源编码</summary>
    public const string Resource = "code_gen";

    /// <summary>查看（列表/详情/预览）</summary>
    public const string Read = "code_gen:read";

    /// <summary>创建（数据源/表/列/模板配置）</summary>
    public const string Create = "code_gen:create";

    /// <summary>更新</summary>
    public const string Update = "code_gen:update";

    /// <summary>删除</summary>
    public const string Delete = "code_gen:delete";

    /// <summary>导出/下载（生成产物 Zip 下载）</summary>
    public const string Export = "code_gen:export";

    /// <summary>导入（从数据库导入表结构、导入模板）</summary>
    public const string Import = "code_gen:import";

    /// <summary>执行（执行代码生成）</summary>
    public const string Execute = "code_gen:execute";
}
