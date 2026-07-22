// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统版本命令应用服务接口
/// </summary>
public interface IVersionAppService : IApplicationService
{
    Task<VersionDetailDto> CreateVersionAsync(VersionCreateDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> UpdateVersionAsync(VersionUpdateDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> StartVersionUpgradeAsync(VersionUpgradeStartDto input, CancellationToken cancellationToken = default);

    Task<VersionDetailDto> FinishVersionUpgradeAsync(VersionUpgradeFinishDto input, CancellationToken cancellationToken = default);

    Task DeleteVersionAsync(long id, CancellationToken cancellationToken = default);
}
