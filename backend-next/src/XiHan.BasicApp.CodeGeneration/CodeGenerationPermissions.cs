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
    /// 修改与启停表配置。
    /// </summary>
    public const string TablesUpdate = "codegeneration.codegen-tables.update";

    /// <summary>
    /// 删除表配置。
    /// </summary>
    public const string TablesDelete = "codegeneration.codegen-tables.delete";

    /// <summary>
    /// 导入库表与同步结构。
    /// </summary>
    public const string TablesImport = "codegeneration.codegen-tables.import";

    /// <summary>
    /// 生成代码。
    /// </summary>
    /// <remarks>
    /// 打包下载，或在开发环境写进项目。
    /// </remarks>
    public const string TablesExecute = "codegeneration.codegen-tables.execute";

    /// <summary>
    /// 查看列配置。
    /// </summary>
    public const string TableColumnsRead = "codegeneration.codegen-table-columns.read";

    /// <summary>
    /// 保存列配置。
    /// </summary>
    public const string TableColumnsUpdate = "codegeneration.codegen-table-columns.update";

    /// <summary>
    /// 查看模板与检查模板语法。
    /// </summary>
    public const string TemplatesRead = "codegeneration.codegen-templates.read";

    /// <summary>
    /// 新建模板。
    /// </summary>
    public const string TemplatesCreate = "codegeneration.codegen-templates.create";

    /// <summary>
    /// 修改与启停模板。
    /// </summary>
    public const string TemplatesUpdate = "codegeneration.codegen-templates.update";

    /// <summary>
    /// 删除模板。
    /// </summary>
    public const string TemplatesDelete = "codegeneration.codegen-templates.delete";

    /// <summary>
    /// 查看生成历史与下载产物包。
    /// </summary>
    public const string HistoriesRead = "codegeneration.codegen-histories.read";

    /// <summary>
    /// 零代码运行时：按表配置只读查看数据。
    /// </summary>
    public const string RuntimeRead = "codegeneration.codegen-runtime.read";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(DatasourcesRead, "Read code generation data sources", PermissionScope.Platform, description: "View the external data sources used to import table schemas; passwords and connection strings are never returned."),
        new(DatasourcesCreate, "Create code generation data sources", PermissionScope.Platform, audited: true, description: "Create external data sources; the password and the connection string are stored encrypted."),
        new(DatasourcesUpdate, "Update code generation data sources", PermissionScope.Platform, audited: true, description: "Edit, enable or disable data sources and test their connections."),
        new(DatasourcesDelete, "Delete code generation data sources", PermissionScope.Platform, audited: true, description: "Delete data sources that no table configuration references."),
        new(TablesRead, "Read code generation tables", PermissionScope.Platform, description: "Open the code generation page, view the imported table configurations and list the tables of a database."),
        new(TablesUpdate, "Update code generation tables", PermissionScope.Platform, audited: true, description: "Edit, enable or disable table configurations."),
        new(TablesDelete, "Delete code generation tables", PermissionScope.Platform, audited: true, description: "Delete table configurations together with their column configurations."),
        new(TablesImport, "Import code generation tables", PermissionScope.Platform, audited: true, description: "Import database tables as table configurations and synchronize them with the latest schema."),
        new(TablesExecute, "Generate code", PermissionScope.Platform, audited: true, description: "Generate the code of a table configuration as a downloadable package, or write it into the project in development."),
        new(TableColumnsRead, "Read code generation columns", PermissionScope.Platform, description: "View the column configurations of a table."),
        new(TableColumnsUpdate, "Update code generation columns", PermissionScope.Platform, audited: true, description: "Save the column configurations of a table."),
        new(TemplatesRead, "Read code generation templates", PermissionScope.Platform, description: "View the code generation templates and check their syntax."),
        new(TemplatesCreate, "Create code generation templates", PermissionScope.Platform, audited: true, description: "Create custom code generation templates."),
        new(TemplatesUpdate, "Update code generation templates", PermissionScope.Platform, audited: true, description: "Edit custom templates and enable or disable any template; built-in templates cannot be edited."),
        new(TemplatesDelete, "Delete code generation templates", PermissionScope.Platform, audited: true, description: "Delete custom templates; built-in templates cannot be deleted."),
        new(HistoriesRead, "Read code generation history", PermissionScope.Platform, description: "View the code generation history and download the generated packages."),
        new(RuntimeRead, "Read zero-code runtime data", PermissionScope.Platform, description: "View the data of an enabled table configuration read-only, without generating any code."),
    ];
}
