#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppAppService
// Guid:b7821328-26b9-4db9-ac48-27655caf6549
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
