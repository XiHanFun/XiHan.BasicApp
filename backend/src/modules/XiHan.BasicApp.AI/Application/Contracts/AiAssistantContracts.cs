// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.AI.Application.Contracts;

/// <summary>
/// AI 助手命令应用服务接口
/// </summary>
public interface IAiAssistantAppService : IApplicationService
{
    /// <summary>
    /// 创建助手
    /// </summary>
    Task<AiAssistantDetailDto> CreateAsync(AiAssistantCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新助手
    /// </summary>
    Task<AiAssistantDetailDto> UpdateAsync(AiAssistantUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新助手状态
    /// </summary>
    Task<AiAssistantDetailDto> UpdateStatusAsync(AiAssistantStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设为默认助手
    /// </summary>
    Task<AiAssistantDetailDto> SetDefaultAsync(AiAssistantActionDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除助手
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI 助手查询应用服务接口
/// </summary>
public interface IAiAssistantQueryService : IApplicationService
{
    /// <summary>
    /// 获取助手分页列表
    /// </summary>
    Task<PageResultDtoBase<AiAssistantListItemDto>> GetPageAsync(AiAssistantPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取助手详情
    /// </summary>
    Task<AiAssistantDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可用助手列表（聊天页选择用，仅启用项）
    /// </summary>
    Task<IReadOnlyList<AiAssistantOptionDto>> GetAvailableAsync(CancellationToken cancellationToken = default);
}
