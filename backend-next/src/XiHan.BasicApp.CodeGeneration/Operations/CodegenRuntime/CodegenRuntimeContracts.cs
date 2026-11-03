// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenRuntime;

/// <summary>
/// 零代码运行时的一列
/// </summary>
/// <param name="ColumnName">列名</param>
/// <param name="PropertyName">数据行里的键（属性名的 camelCase）</param>
/// <param name="Label">显示名（列注释）</param>
/// <param name="TsType">TypeScript 类型</param>
/// <param name="HtmlType">控件</param>
/// <param name="QueryType">查询方式</param>
/// <param name="IsList">是否进列表</param>
/// <param name="IsQuery">是否可查询</param>
/// <param name="IsRequired">是否必填</param>
public sealed record CodegenRuntimeColumnResponse(
    string ColumnName,
    string PropertyName,
    string? Label,
    string? TsType,
    CodeGenHtmlType HtmlType,
    CodeGenQueryType QueryType,
    bool IsList,
    bool IsQuery,
    bool IsRequired);

/// <summary>
/// 零代码运行时的表结构
/// </summary>
/// <param name="TableId">表配置 ID</param>
/// <param name="TableName">表名</param>
/// <param name="ClassName">类名</param>
/// <param name="TableComment">表注释</param>
/// <param name="Columns">列，按列配置的顺序</param>
public sealed record CodegenRuntimeSchemaResponse(Guid TableId, string TableName, string ClassName, string? TableComment, IReadOnlyList<CodegenRuntimeColumnResponse> Columns);

/// <summary>
/// <c>codegen-runtime.get-schema</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
public sealed record CodegenRuntimeIdRequest(Guid Id);

/// <summary>
/// <c>codegen-runtime.list</c> 的请求
/// </summary>
/// <param name="Id">表配置 ID</param>
/// <param name="Page">分页（每页最多 500 条）</param>
public sealed record ListCodegenRuntimeRequest(Guid Id, [Required] PageInfo Page);
