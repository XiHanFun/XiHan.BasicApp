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

    /// <summary>
    /// 操作 <c>codegen-tables.preview</c> 的说明。
    /// </summary>
    public const string PreviewCodegenTableDescription = "codegen_tables.preview.description";

    /// <summary>
    /// 操作 <c>codegen-templates.list</c> 的说明。
    /// </summary>
    public const string ListCodegenTemplatesDescription = "codegen_templates.list.description";

    /// <summary>
    /// 操作 <c>codegen-templates.get</c> 的说明。
    /// </summary>
    public const string GetCodegenTemplateDescription = "codegen_templates.get.description";

    /// <summary>
    /// 操作 <c>codegen-templates.validate</c> 的说明。
    /// </summary>
    public const string ValidateCodegenTemplateDescription = "codegen_templates.validate.description";

    /// <summary>
    /// 操作 <c>codegen-templates.create</c> 的说明。
    /// </summary>
    public const string CreateCodegenTemplateDescription = "codegen_templates.create.description";

    /// <summary>
    /// 操作 <c>codegen-templates.update</c> 的说明。
    /// </summary>
    public const string UpdateCodegenTemplateDescription = "codegen_templates.update.description";

    /// <summary>
    /// 操作 <c>codegen-templates.set-status</c> 的说明。
    /// </summary>
    public const string SetCodegenTemplateStatusDescription = "codegen_templates.set_status.description";

    /// <summary>
    /// 操作 <c>codegen-templates.delete</c> 的说明。
    /// </summary>
    public const string DeleteCodegenTemplatesDescription = "codegen_templates.delete.description";

    /// <summary>
    /// 模板不存在（参数 <c>id</c>）。
    /// </summary>
    public const string TemplateNotFound = "codegeneration.template.not_found";

    /// <summary>
    /// 模板编码已被占用（参数 <c>code</c>）。
    /// </summary>
    public const string TemplateCodeTaken = "codegeneration.template.code_taken";

    /// <summary>
    /// 内置模板不能修改或删除（参数 <c>code</c>）。
    /// </summary>
    public const string TemplateBuiltIn = "codegeneration.template.built_in";

    /// <summary>
    /// 表已有实体、表配置的类名与它不同（参数 <c>table</c>、<c>entity</c>、<c>expected</c>）。
    /// </summary>
    public const string GenerateClassMismatch = "codegeneration.generate.class_mismatch";

    /// <summary>
    /// 包名不合规（参数 <c>module</c>）。
    /// </summary>
    public const string GenerateModuleInvalid = "codegeneration.generate.module_invalid";

    /// <summary>
    /// 包的命名空间不合规（参数 <c>namespace</c>）。
    /// </summary>
    public const string GenerateNamespaceInvalid = "codegeneration.generate.namespace_invalid";

    /// <summary>
    /// 系统字典列没有字典码（参数 <c>column</c>）。
    /// </summary>
    public const string GenerateDictCodeMissing = "codegeneration.generate.dict_code_missing";

    /// <summary>
    /// 系统字典列不是文本（参数 <c>column</c>、<c>type</c>）。
    /// </summary>
    public const string GenerateDictNotString = "codegeneration.generate.dict_not_string";

    /// <summary>
    /// 关联列不是 Guid（参数 <c>column</c>、<c>type</c>）。
    /// </summary>
    public const string GenerateRelationNotGuid = "codegeneration.generate.relation_not_guid";

    /// <summary>
    /// 关联列没选关联的表或那张表配置不存在（参数 <c>column</c>）。
    /// </summary>
    public const string GenerateRelationTargetInvalid = "codegeneration.generate.relation_target_invalid";

    /// <summary>
    /// 关联树的目标不是配好父级列的树表（参数 <c>column</c>、<c>target</c>）。
    /// </summary>
    public const string GenerateRelationTreeInvalid = "codegeneration.generate.relation_tree_invalid";

    /// <summary>
    /// 关联的显示列没选、不在目标表或不是文本（参数 <c>column</c>、<c>label</c>）。
    /// </summary>
    public const string GenerateRelationLabelInvalid = "codegeneration.generate.relation_label_invalid";

    /// <summary>
    /// 布尔或二进制列勾了唯一（参数 <c>column</c>、<c>type</c>）。
    /// </summary>
    public const string GenerateUniqueUnsupported = "codegeneration.generate.unique_unsupported";

    /// <summary>
    /// 勾了状态切换却没有可用的状态列（参数 <c>table</c>）。
    /// </summary>
    public const string GenerateStatusMissing = "codegeneration.generate.status_missing";

    /// <summary>
    /// 有多列启停枚举、又没有名为 Status 的（参数 <c>columns</c>）。
    /// </summary>
    public const string GenerateStatusAmbiguous = "codegeneration.generate.status_ambiguous";

    /// <summary>
    /// 状态列没进列表（参数 <c>column</c>）。
    /// </summary>
    public const string GenerateStatusNotListed = "codegeneration.generate.status_not_listed";

    /// <summary>
    /// 树表的父级列没配、不在列配置或不是 Guid（参数 <c>table</c>、<c>column</c>）。
    /// </summary>
    public const string GenerateTreeParentInvalid = "codegeneration.generate.tree_parent_invalid";

    /// <summary>
    /// 树表的名称列没配、不在列配置或没进列表（参数 <c>table</c>、<c>column</c>）。
    /// </summary>
    public const string GenerateTreeNameInvalid = "codegeneration.generate.tree_name_invalid";

    /// <summary>
    /// 主从表的主表或外键列没配好（参数 <c>table</c>）。
    /// </summary>
    public const string GenerateMasterInvalid = "codegeneration.generate.master_invalid";

    /// <summary>
    /// 没有可用的模板。
    /// </summary>
    public const string GenerateNoTemplates = "codegeneration.generate.no_templates";

    /// <summary>
    /// 模板渲染失败（参数 <c>template</c>、<c>reason</c>）。
    /// </summary>
    public const string GenerateRenderFailed = "codegeneration.generate.render_failed";

    /// <summary>
    /// 模板产出的路径越界（参数 <c>template</c>、<c>path</c>）。
    /// </summary>
    public const string GeneratePathInvalid = "codegeneration.generate.path_invalid";

    /// <summary>
    /// 常量选项列没有选项（参数 <c>column</c>）。
    /// </summary>
    public const string ConstOptionsMissing = "codegeneration.generate.const_options_missing";

    /// <summary>
    /// 常量选项的形状不对（参数 <c>column</c>、<c>value</c>）。
    /// </summary>
    public const string ConstOptionsInvalid = "codegeneration.generate.const_options_invalid";

    /// <summary>
    /// 常量选项的值与列的类型不符（参数 <c>column</c>、<c>type</c>、<c>valueType</c>）。
    /// </summary>
    public const string ConstOptionsTypeMismatch = "codegeneration.generate.const_options_type_mismatch";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(CodeGenerationMessages)));

    /// <summary>
    /// 本包的消息目录。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
