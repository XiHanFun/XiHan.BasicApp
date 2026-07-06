#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiPromptContracts
// Guid:a11c0de0-900a-4a10-9a00-00000000ai99
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.AI.Application.Contracts;

/// <summary>
/// AI 提示词命令应用服务接口
/// </summary>
public interface IAiPromptAppService : IApplicationService
{
    /// <summary>
    /// 创建提示词
    /// </summary>
    Task<AiPromptDetailDto> CreateAsync(AiPromptCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新提示词
    /// </summary>
    Task<AiPromptDetailDto> UpdateAsync(AiPromptUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新提示词状态
    /// </summary>
    Task<AiPromptDetailDto> UpdateStatusAsync(AiPromptStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除提示词
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI 提示词查询应用服务接口
/// </summary>
public interface IAiPromptQueryService : IApplicationService
{
    /// <summary>
    /// 获取提示词分页列表
    /// </summary>
    Task<PageResultDtoBase<AiPromptListItemDto>> GetPageAsync(AiPromptPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取提示词详情
    /// </summary>
    Task<AiPromptDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}
