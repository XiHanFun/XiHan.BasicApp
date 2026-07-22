// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// OAuth 应用领域服务
/// </summary>
public interface IOAuthAppDomainService
{
    /// <summary>
    /// 创建 OAuth 应用
    /// </summary>
    Task<OAuthAppCommandResult> CreateOAuthAppAsync(OAuthAppCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除 OAuth 应用
    /// </summary>
    Task DeleteOAuthAppAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新生成 OAuth 应用密钥
    /// </summary>
    Task<OAuthAppCommandResult> RegenerateOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 OAuth 应用
    /// </summary>
    Task<OAuthAppCommandResult> UpdateOAuthAppAsync(OAuthAppUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新 OAuth 应用状态
    /// </summary>
    Task<OAuthAppCommandResult> UpdateOAuthAppStatusAsync(OAuthAppStatusChangeCommand command, CancellationToken cancellationToken = default);
}
