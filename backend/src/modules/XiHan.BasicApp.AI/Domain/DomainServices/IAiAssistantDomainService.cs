// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI 助手领域服务：助手配置的生命周期与默认互斥
/// </summary>
public interface IAiAssistantDomainService
{
    /// <summary>
    /// 创建助手（编码租户内唯一）
    /// </summary>
    Task<AiAssistantCommandResult> CreateAssistantAsync(AiAssistantCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新助手（编码不可变）
    /// </summary>
    Task<AiAssistantCommandResult> UpdateAssistantAsync(AiAssistantUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新助手状态
    /// </summary>
    Task<AiAssistantCommandResult> UpdateAssistantStatusAsync(AiAssistantStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设为默认助手（禁用的助手拒绝设默认）
    /// </summary>
    Task<AiAssistantCommandResult> SetDefaultAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除助手
    /// </summary>
    Task DeleteAssistantAsync(long id, CancellationToken cancellationToken = default);
}
