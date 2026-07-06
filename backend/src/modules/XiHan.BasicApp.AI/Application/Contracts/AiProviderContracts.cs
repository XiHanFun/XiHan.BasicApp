#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderContracts
// Guid:c4c714e5-d44a-4139-b5c6-0808738cd192
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.AI.Application.Contracts;

/// <summary>
/// AI Provider 命令应用服务接口
/// </summary>
public interface IAiProviderAppService : IApplicationService
{
    /// <summary>
    /// 创建 provider
    /// </summary>
    Task<AiProviderDetailDto> CreateAsync(AiProviderCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 provider
    /// </summary>
    Task<AiProviderDetailDto> UpdateAsync(AiProviderUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 provider 状态
    /// </summary>
    Task<AiProviderDetailDto> UpdateStatusAsync(AiProviderStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设为默认 provider
    /// </summary>
    Task<AiProviderDetailDto> SetDefaultAsync(AiProviderActionDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 provider
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试连接（对指定 provider 发起一次极简推理）
    /// </summary>
    Task<AiProviderTestConnectionResultDto> TestConnectionAsync(AiProviderActionDto input, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI Provider 查询应用服务接口
/// </summary>
public interface IAiProviderQueryService : IApplicationService
{
    /// <summary>
    /// 获取 provider 分页列表
    /// </summary>
    Task<PageResultDtoBase<AiProviderListItemDto>> GetPageAsync(AiProviderPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 provider 详情
    /// </summary>
    Task<AiProviderDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
