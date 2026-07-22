// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统配置命令应用服务接口
/// </summary>
public interface IConfigAppService : IApplicationService
{
    Task<ConfigDetailDto> CreateConfigAsync(ConfigCreateDto input, CancellationToken cancellationToken = default);

    Task<ConfigDetailDto> UpdateConfigAsync(ConfigUpdateDto input, CancellationToken cancellationToken = default);

    Task<ConfigDetailDto> UpdateConfigStatusAsync(ConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default);
}
