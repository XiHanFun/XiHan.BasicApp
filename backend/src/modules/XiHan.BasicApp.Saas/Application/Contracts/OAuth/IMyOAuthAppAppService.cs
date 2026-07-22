// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 我的 OAuth 应用应用服务（个人中心开发者设置：用户自助注册/管理自己的 OAuth 应用）
/// </summary>
public interface IMyOAuthAppAppService : IApplicationService
{
    Task<List<MyOAuthAppItemDto>> GetMyOAuthAppsAsync(CancellationToken cancellationToken = default);

    Task<MyOAuthAppSecretDto> CreateMyOAuthAppAsync(MyOAuthAppCreateDto input, CancellationToken cancellationToken = default);

    Task<MyOAuthAppItemDto> UpdateMyOAuthAppAsync(MyOAuthAppUpdateDto input, CancellationToken cancellationToken = default);

    Task<MyOAuthAppSecretDto> RegenerateMyOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default);

    Task<MyOAuthAppItemDto> UpdateMyOAuthAppStatusAsync(MyOAuthAppStatusDto input, CancellationToken cancellationToken = default);

    Task DeleteMyOAuthAppAsync(long id, CancellationToken cancellationToken = default);
}
