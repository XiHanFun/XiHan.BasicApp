// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 零代码运行时（只读）应用服务接口
/// </summary>
/// <remarks>
/// 不生成/编译任何实体代码，运行时直接按 <c>SysCodeGenTable</c> 元数据解释执行：暴露字段 schema 与分页数据。
/// 本切片仅只读（schema/list），写入/DDL 留待后续阶段。
/// </remarks>
public interface IDynamicRuntimeAppService : IApplicationService
{
    /// <summary>
    /// 获取指定已配置表的字段 schema
    /// </summary>
    Task<DynamicRuntimeSchemaDto> GetSchemaAsync(long tableId, CancellationToken ct = default);

    /// <summary>
    /// 按表名动态分页查询指定已配置表的数据（只读）
    /// </summary>
    Task<DynamicRuntimePageResultDto> GetPageAsync(DynamicRuntimePageQueryDto input, CancellationToken ct = default);
}
