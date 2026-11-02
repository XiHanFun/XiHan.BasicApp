// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的权限码。
/// </summary>
/// <remarks>
/// 只在平台一侧（照 4.x）。4.x 是一组 <c>code_gen:{read,create,update,delete,import,execute}</c>，6.0 权限码的资源段要与操作一致，按资源拆开，
/// 动作照 4.x 各方法要的那个码：启停与测试连接归 <c>update</c>（4.x 测试连接只要读权限却写库，方案 C2）。
/// </remarks>
public static class CodeGenerationPermissions
{
    /// <summary>
    /// 查看数据源。
    /// </summary>
    public const string DatasourcesRead = "codegeneration.codegen-datasources.read";

    /// <summary>
    /// 新建数据源。
    /// </summary>
    public const string DatasourcesCreate = "codegeneration.codegen-datasources.create";

    /// <summary>
    /// 修改、启停数据源与测试连接。
    /// </summary>
    public const string DatasourcesUpdate = "codegeneration.codegen-datasources.update";

    /// <summary>
    /// 删除数据源。
    /// </summary>
    public const string DatasourcesDelete = "codegeneration.codegen-datasources.delete";

    /// <summary>
    /// 查看表配置。
    /// </summary>
    /// <remarks>
    /// 也是代码生成页的读权限。
    /// </remarks>
    public const string TablesRead = "codegeneration.codegen-tables.read";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(DatasourcesRead, "Read code generation data sources", PermissionScope.Platform, description: "View the external data sources used to import table schemas; passwords and connection strings are never returned."),
        new(DatasourcesCreate, "Create code generation data sources", PermissionScope.Platform, audited: true, description: "Create external data sources; the password and the connection string are stored encrypted."),
        new(DatasourcesUpdate, "Update code generation data sources", PermissionScope.Platform, audited: true, description: "Edit, enable or disable data sources and test their connections."),
        new(DatasourcesDelete, "Delete code generation data sources", PermissionScope.Platform, audited: true, description: "Delete data sources that no table configuration references."),
        new(TablesRead, "Read code generation tables", PermissionScope.Platform, description: "Open the code generation page and view the imported table configurations."),
    ];
}
