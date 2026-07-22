// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 类型映射提供器：把数据库列类型翻译为 C#/TS 类型与默认表单/查询语义
/// </summary>
public interface ITypeMappingProvider
{
    /// <summary>
    /// 映射列类型
    /// </summary>
    /// <param name="databaseType">数据库类型（决定方言）</param>
    /// <param name="dbColumnType">数据库列类型（如 varchar、bigint、datetime）</param>
    /// <param name="isNullable">是否可空（影响 C# 可空标注）</param>
    /// <returns>映射结果</returns>
    ColumnTypeMapping Map(DatabaseType databaseType, string? dbColumnType, bool isNullable);
}
