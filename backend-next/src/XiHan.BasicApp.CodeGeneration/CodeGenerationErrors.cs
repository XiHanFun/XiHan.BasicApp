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
    /// 列表不支持该筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "CODEGENERATION_VALIDATION_001";

    /// <summary>
    /// 数据源的口令或连接串解不开。
    /// </summary>
    public const string SecretUnreadable = "CODEGENERATION_UNAVAILABLE_001";
}
