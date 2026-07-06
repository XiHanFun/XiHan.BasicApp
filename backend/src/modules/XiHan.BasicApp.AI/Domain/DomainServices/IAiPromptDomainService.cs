#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAiPromptDomainService
// Guid:f317a1d9-b54d-4358-b083-3dc3ba1283fd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/06 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
