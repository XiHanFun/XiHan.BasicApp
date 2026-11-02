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
