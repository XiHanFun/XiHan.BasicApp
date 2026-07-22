// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

public interface IOAuthAppAppService : IApplicationService
{
    Task<OAuthAppSecretDto> CreateOAuthAppAsync(OAuthAppCreateDto input, CancellationToken cancellationToken = default);

    Task<OAuthAppDetailDto> UpdateOAuthAppAsync(OAuthAppUpdateDto input, CancellationToken cancellationToken = default);

    Task<OAuthAppDetailDto> UpdateOAuthAppStatusAsync(OAuthAppStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task<OAuthAppSecretDto> RegenerateOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default);

    Task DeleteOAuthAppAsync(long id, CancellationToken cancellationToken = default);
}
