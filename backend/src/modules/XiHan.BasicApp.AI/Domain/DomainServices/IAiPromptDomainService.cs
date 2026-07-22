// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI 提示词领域服务接口
/// </summary>
public interface IAiPromptDomainService
{
    /// <summary>
    /// 创建提示词（编码租户内唯一）
    /// </summary>
    Task<AiPromptCommandResult> CreatePromptAsync(AiPromptCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新提示词（编码不可变）
    /// </summary>
    Task<AiPromptCommandResult> UpdatePromptAsync(AiPromptUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新提示词状态
    /// </summary>
    Task<AiPromptCommandResult> UpdatePromptStatusAsync(AiPromptStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除提示词
    /// </summary>
    Task DeletePromptAsync(long id, CancellationToken cancellationToken = default);
}
