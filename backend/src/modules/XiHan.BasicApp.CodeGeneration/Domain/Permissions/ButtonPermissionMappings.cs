#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ButtonPermissionMappings
// Guid:c0de9e00-000a-4a00-9000-00000000000a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.Permissions;

/// <summary>
/// 标准按钮 → 操作码映射项
/// </summary>
/// <param name="Key">按钮键（前端 action key）</param>
/// <param name="Title">按钮显示名</param>
/// <param name="Action">对应操作码（与 SysOperation 操作字典对齐；多个按钮可共享同一操作）</param>
public sealed record CodeGenButtonPermission(string Key, string Title, string Action);

/// <summary>
/// 代码生成的标准按钮/权限映射约定（生成菜单+按钮权限片段时使用）
/// </summary>
/// <remarks>
/// 与 [[menu-seed-architecture]] 一致：权限码沿用 {资源}:{操作} 两段式（同 <c>code_gen:read</c>），
/// 生成模块的资源段取表名（snake，唯一）。关联不入生成代码，此处仅权限/菜单声明约定。
/// </remarks>
public static class ButtonPermissionMappings
{
    /// <summary>
    /// 标准按钮集（查询/详情/新增/编辑/删除/导出/导入）
    /// </summary>
    public static readonly IReadOnlyList<CodeGenButtonPermission> Buttons =
    [
        new("query", "查询", "read"),
        new("detail", "详情", "read"),
        new("create", "新增", "create"),
        new("update", "编辑", "update"),
        new("delete", "删除", "delete"),
        new("export", "导出", "export"),
        new("import", "导入", "import")
    ];

    /// <summary>
    /// 去重后的标准操作集（权限码派生用）
    /// </summary>
    public static readonly IReadOnlyList<string> Actions =
    [
        "read", "create", "update", "delete", "export", "import"
    ];
}
