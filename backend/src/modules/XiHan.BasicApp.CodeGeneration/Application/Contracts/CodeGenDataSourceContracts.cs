// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.CodeGeneration.Application.Contracts;

/// <summary>
/// 代码生成数据源命令应用服务接口
/// </summary>
public interface ICodeGenDataSourceAppService : IApplicationService
{
    /// <summary>
    /// 创建数据源
    /// </summary>
    Task<CodeGenDataSourceDetailDto> CreateAsync(CodeGenDataSourceCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新数据源
    /// </summary>
    Task<CodeGenDataSourceDetailDto> UpdateAsync(CodeGenDataSourceUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新数据源状态
    /// </summary>
    Task<CodeGenDataSourceDetailDto> UpdateStatusAsync(CodeGenDataSourceStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除数据源
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试数据源连接（测试通过方可启用/保存）
    /// </summary>
    Task<CodeGenConnectionTestResultDto> TestConnectionAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码生成数据源查询应用服务接口
/// </summary>
public interface ICodeGenDataSourceQueryService : IApplicationService
{
    /// <summary>
    /// 获取数据源分页列表
    /// </summary>
    Task<PageResultDtoBase<CodeGenDataSourceListItemDto>> GetPageAsync(CodeGenDataSourcePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取数据源详情
    /// </summary>
    Task<CodeGenDataSourceDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
