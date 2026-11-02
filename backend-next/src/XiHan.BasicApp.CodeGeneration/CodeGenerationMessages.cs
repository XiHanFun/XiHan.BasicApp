// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）。
/// </remarks>
public static class CodeGenerationMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.codegeneration";

    /// <summary>
    /// 操作 <c>codegen-datasources.list</c> 的说明。
    /// </summary>
    public const string ListCodegenDatasourcesDescription = "codegen_datasources.list.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.options</c> 的说明。
    /// </summary>
    public const string ListCodegenDatasourceOptionsDescription = "codegen_datasources.options.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.get</c> 的说明。
    /// </summary>
    public const string GetCodegenDatasourceDescription = "codegen_datasources.get.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.create</c> 的说明。
    /// </summary>
    public const string CreateCodegenDatasourceDescription = "codegen_datasources.create.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.update</c> 的说明。
    /// </summary>
    public const string UpdateCodegenDatasourceDescription = "codegen_datasources.update.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.set-status</c> 的说明。
    /// </summary>
    public const string SetCodegenDatasourceStatusDescription = "codegen_datasources.set_status.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.delete</c> 的说明。
    /// </summary>
    public const string DeleteCodegenDatasourcesDescription = "codegen_datasources.delete.description";

    /// <summary>
    /// 操作 <c>codegen-datasources.test-connection</c> 的说明。
    /// </summary>
    public const string TestCodegenDatasourceDescription = "codegen_datasources.test_connection.description";

    /// <summary>
    /// 操作 <c>codegen-tables.list</c> 的说明。
    /// </summary>
    public const string ListCodegenTablesDescription = "codegen_tables.list.description";

    /// <summary>
    /// 操作 <c>codegen-tables.options</c> 的说明。
    /// </summary>
    public const string ListCodegenTableOptionsDescription = "codegen_tables.options.description";

    /// <summary>
    /// 操作 <c>codegen-tables.get</c> 的说明。
    /// </summary>
    public const string GetCodegenTableDescription = "codegen_tables.get.description";

    /// <summary>
    /// 操作 <c>codegen-tables.db-tables</c> 的说明。
    /// </summary>
    public const string ListCodegenDbTablesDescription = "codegen_tables.db_tables.description";

    /// <summary>
    /// 操作 <c>codegen-tables.import</c> 的说明。
    /// </summary>
    public const string ImportCodegenTablesDescription = "codegen_tables.import.description";

    /// <summary>
    /// 操作 <c>codegen-tables.sync-schema</c> 的说明。
    /// </summary>
    public const string SyncCodegenTableSchemaDescription = "codegen_tables.sync_schema.description";

    /// <summary>
    /// 操作 <c>codegen-tables.update</c> 的说明。
    /// </summary>
    public const string UpdateCodegenTableDescription = "codegen_tables.update.description";

    /// <summary>
    /// 操作 <c>codegen-tables.set-status</c> 的说明。
    /// </summary>
    public const string SetCodegenTableStatusDescription = "codegen_tables.set_status.description";

    /// <summary>
    /// 操作 <c>codegen-tables.delete</c> 的说明。
    /// </summary>
    public const string DeleteCodegenTablesDescription = "codegen_tables.delete.description";

    /// <summary>
    /// 操作 <c>codegen-table-columns.list</c> 的说明。
    /// </summary>
    public const string ListCodegenTableColumnsDescription = "codegen_table_columns.list.description";

    /// <summary>
    /// 操作 <c>codegen-table-columns.save</c> 的说明。
    /// </summary>
    public const string SaveCodegenTableColumnsDescription = "codegen_table_columns.save.description";

    /// <summary>
    /// 表配置不存在。
    /// </summary>
    public const string TableNotFound = "codegeneration.table.not_found";

    /// <summary>
    /// 这张库表已经导入过。
    /// </summary>
    public const string TableAlreadyImported = "codegeneration.table.already_imported";

    /// <summary>
    /// 库表没有列。
    /// </summary>
    public const string TableHasNoColumn = "codegeneration.table.no_column";

    /// <summary>
    /// 表配置的生成模式不能是通用。
    /// </summary>
    public const string TemplateTypeUniversal = "codegeneration.table.template_type_universal";

    /// <summary>
    /// 勾了导入却没勾新建。
    /// </summary>
    public const string ActionsConflict = "codegeneration.table.actions_conflict";

    /// <summary>
    /// 列配置不属于这张表。
    /// </summary>
    public const string ColumnNotInTable = "codegeneration.column.not_in_table";

    /// <summary>
    /// 系统字典缺少字典码。
    /// </summary>
    public const string SelectorDictCodeRequired = "codegeneration.column.selector_dict_code_required";

    /// <summary>
    /// 枚举缺少类型全名。
    /// </summary>
    public const string SelectorEnumRequired = "codegeneration.column.selector_enum_required";

    /// <summary>
    /// 常量选项不是合法的 JSON。
    /// </summary>
    public const string SelectorConstInvalid = "codegeneration.column.selector_const_invalid";

    /// <summary>
    /// 关联缺少关联表。
    /// </summary>
    public const string SelectorRelationRequired = "codegeneration.column.selector_relation_required";

    /// <summary>
    /// 关联表缺少显示列。
    /// </summary>
    public const string SelectorLabelRequired = "codegeneration.column.selector_label_required";

    /// <summary>
    /// 主库的数据库类型不在支持之列。
    /// </summary>
    public const string DatabaseUnsupported = "codegeneration.database.unsupported";

    /// <summary>
    /// 数据源不存在。
    /// </summary>
    public const string DataSourceNotFound = "codegeneration.datasource.not_found";

    /// <summary>
    /// 已有同名的数据源。
    /// </summary>
    public const string DataSourceNameTaken = "codegeneration.datasource.name_taken";

    /// <summary>
    /// 数据源仍被表配置引用。
    /// </summary>
    public const string DataSourceInUse = "codegeneration.datasource.in_use";

    /// <summary>
    /// 数据源已停用。
    /// </summary>
    public const string DataSourceDisabled = "codegeneration.datasource.disabled";

    /// <summary>
    /// 口令或连接串解不开。
    /// </summary>
    public const string SecretUnreadable = "codegeneration.datasource.secret_unreadable";

    /// <summary>
    /// 测试连接成功。
    /// </summary>
    public const string ConnectionSucceeded = "codegeneration.datasource.connection_succeeded";

    /// <summary>
    /// 不支持的筛选。
    /// </summary>
    public const string FilterUnsupported = "codegeneration.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序。
    /// </summary>
    public const string SortUnsupported = "codegeneration.query.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(CodeGenerationMessages)));

    /// <summary>
    /// 本包的消息目录。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
