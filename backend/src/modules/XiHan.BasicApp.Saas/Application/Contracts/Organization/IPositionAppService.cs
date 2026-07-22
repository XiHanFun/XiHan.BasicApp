// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 岗位命令应用服务接口
/// </summary>
public interface IPositionAppService : IApplicationService
{
    Task<PositionDetailDto> CreatePositionAsync(PositionCreateDto input, CancellationToken cancellationToken = default);

    Task<PositionDetailDto> UpdatePositionAsync(PositionUpdateDto input, CancellationToken cancellationToken = default);

    Task<PositionDetailDto> UpdatePositionStatusAsync(PositionStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeletePositionAsync(long id, CancellationToken cancellationToken = default);
}
