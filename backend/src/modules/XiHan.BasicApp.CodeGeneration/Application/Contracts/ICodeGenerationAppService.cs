// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成编排应用服务接口（导入 → 预览 → 生成 → 下载）
/// </summary>
/// <remarks>
/// 这是引擎面的对外入口：负责权限/事务/DTO 转换/历史留痕，生成步骤下沉到 <c>ICodeGenerationEngine</c>。
/// </remarks>
public interface ICodeGenerationAppService : IApplicationService
{
    /// <summary>列出可导入的数据库表（逆向工程）</summary>
    Task<IReadOnlyList<string>> ListDatabaseTablesAsync(CodeGenDbTableQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>从数据库表导入并创建一条表配置（含列配置）</summary>
    Task<CodeGenTableDetailDto> ImportTableAsync(CodeGenImportTableDto input, CancellationToken cancellationToken = default);

    /// <summary>批量从数据库表导入（逐表隔离异常，返回成功/失败明细）</summary>
    Task<CodeGenImportTablesResultDto> ImportTablesAsync(CodeGenImportTablesDto input, CancellationToken cancellationToken = default);

    /// <summary>同步表结构：按最新库结构重算，人工改过的字段冻结，其余跟随重新推断</summary>
    Task<CodeGenSchemaSyncResultDto> SyncSchemaAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>预览生成（仅返回产物内容）</summary>
    Task<CodeGenResultDto> PreviewAsync(CodeGenPreviewRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>执行生成（按 GenType 分流：预览/Zip）</summary>
    Task<CodeGenResultDto> GenerateAsync(CodeGenGenerateRequestDto input, CancellationToken cancellationToken = default);
}
