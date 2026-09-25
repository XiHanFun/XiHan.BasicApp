// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Permissions;

/// <summary>
/// 代码生成权限码常量
/// </summary>
/// <remarks>
/// 权限目录 <c>CodeGenPermissionCatalogSeeder</c> 按「资源 × 操作」声明：资源 <c>code_gen</c> + 操作 read/create/update/delete/import/execute，
/// 声明出的码必须与这里一一对应（有测试钉住）。
/// </remarks>
public static class CodeGenPermissionCodes
{
    /// <summary>模块编码</summary>
    public const string Module = "code_gen";

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

    /// <summary>导入（从数据库导入表结构、导入模板）</summary>
    public const string Import = "code_gen:import";

    /// <summary>执行（执行代码生成）</summary>
    public const string Execute = "code_gen:execute";
}
