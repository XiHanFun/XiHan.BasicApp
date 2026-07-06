#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAiProviderDomainService
// Guid:f3a0654e-a34f-4959-a5fd-10f639312e40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.AI.Domain.DomainServices;

/// <summary>
/// AI Provider 领域服务接口
/// </summary>
public interface IAiProviderDomainService
{
    /// <summary>
    /// 创建 provider（配置编码租户内唯一；ApiKey 加密落库）
    /// </summary>
    Task<AiProviderCommandResult> CreateProviderAsync(AiProviderCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 provider（编码不可变；ApiKey 为空则保留原密钥）
    /// </summary>
    Task<AiProviderCommandResult> UpdateProviderAsync(AiProviderUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 provider 状态
    /// </summary>
    Task<AiProviderCommandResult> UpdateProviderStatusAsync(AiProviderStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设为默认 provider（清除其它行默认标记）
    /// </summary>
    Task<AiProviderCommandResult> SetDefaultAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 provider
    /// </summary>
    Task DeleteProviderAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试连接（对指定 provider 发起一次极简推理，验证端点/密钥/模型可用）
    /// </summary>
    Task<AiProviderTestResult> TestConnectionAsync(long id, CancellationToken cancellationToken = default);
}
