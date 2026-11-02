// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的错误码。
/// </summary>
/// <remarks>
/// 格式为 <c>CODEGENERATION_&lt;KIND&gt;_&lt;NNN&gt;</c>。并发戳过期由框架仓储报（<c>PERSISTENCE_CONFLICT_*</c>），
/// 外部库连不上、连接串不合格式由框架的运行时数据源报（<c>PERSISTENCE_*</c>），都原样返回。
/// </remarks>
public static class CodeGenerationErrors
{
    /// <summary>
    /// 数据源不存在。
    /// </summary>
    public const string DataSourceNotFound = "CODEGENERATION_NOTFOUND_001";

    /// <summary>
    /// 表配置不存在。
    /// </summary>
    public const string TableNotFound = "CODEGENERATION_NOTFOUND_002";

    /// <summary>
    /// 模板不存在。
    /// </summary>
    public const string TemplateNotFound = "CODEGENERATION_NOTFOUND_003";

    /// <summary>
    /// 已有同名的数据源。
    /// </summary>
    public const string DataSourceNameTaken = "CODEGENERATION_CONFLICT_001";

    /// <summary>
    /// 数据源仍被表配置引用，不能删除。
    /// </summary>
    public const string DataSourceInUse = "CODEGENERATION_CONFLICT_002";

    /// <summary>
    /// 数据源已停用，不能用于读库结构与查询。
    /// </summary>
    public const string DataSourceDisabled = "CODEGENERATION_CONFLICT_003";

    /// <summary>
    /// 这张库表已经导入过。
    /// </summary>
    public const string TableAlreadyImported = "CODEGENERATION_CONFLICT_004";

    /// <summary>
    /// 模板编码已被占用。
    /// </summary>
    public const string TemplateCodeTaken = "CODEGENERATION_CONFLICT_005";

    /// <summary>
    /// 内置模板不能修改或删除。
    /// </summary>
    public const string TemplateBuiltIn = "CODEGENERATION_CONFLICT_006";

    /// <summary>
    /// 列表不支持该筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "CODEGENERATION_VALIDATION_001";

    /// <summary>
    /// 库表没有列。
    /// </summary>
    public const string TableHasNoColumn = "CODEGENERATION_VALIDATION_002";

    /// <summary>
    /// 表配置的生成模式不能是通用。
    /// </summary>
    public const string TemplateTypeUniversal = "CODEGENERATION_VALIDATION_003";

    /// <summary>
    /// 勾了导入却没勾新建。
    /// </summary>
    public const string ActionsConflict = "CODEGENERATION_VALIDATION_004";

    /// <summary>
    /// 列配置不属于这张表。
    /// </summary>
    public const string ColumnNotInTable = "CODEGENERATION_VALIDATION_005";

    /// <summary>
    /// 列的选项来源缺少它要的配置。
    /// </summary>
    public const string SelectorInvalid = "CODEGENERATION_VALIDATION_006";

    /// <summary>
    /// 生成前的配置检查不通过（树表、主从、关联、状态列、选项来源等）。
    /// </summary>
    public const string GenerationInvalid = "CODEGENERATION_VALIDATION_007";

    /// <summary>
    /// 模板渲染失败（语法或运行时错误、产物路径越界）。
    /// </summary>
    public const string TemplateRenderFailed = "CODEGENERATION_VALIDATION_008";

    /// <summary>
    /// 没有可用的模板。
    /// </summary>
    public const string NoTemplates = "CODEGENERATION_VALIDATION_009";

    /// <summary>
    /// 数据源的口令或连接串解不开。
    /// </summary>
    public const string SecretUnreadable = "CODEGENERATION_UNAVAILABLE_001";

    /// <summary>
    /// 主库的数据库类型不在支持之列。
    /// </summary>
    public const string DatabaseUnsupported = "CODEGENERATION_UNAVAILABLE_002";
}
