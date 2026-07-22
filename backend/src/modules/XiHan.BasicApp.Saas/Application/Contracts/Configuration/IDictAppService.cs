// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统字典命令应用服务接口
/// </summary>
public interface IDictAppService : IApplicationService
{
    Task<DictDetailDto> CreateDictAsync(DictCreateDto input, CancellationToken cancellationToken = default);

    Task<DictDetailDto> UpdateDictAsync(DictUpdateDto input, CancellationToken cancellationToken = default);

    Task<DictDetailDto> UpdateDictStatusAsync(DictStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteDictAsync(long id, CancellationToken cancellationToken = default);

    Task<DictItemDetailDto> CreateDictItemAsync(DictItemCreateDto input, CancellationToken cancellationToken = default);

    Task<DictItemDetailDto> UpdateDictItemAsync(DictItemUpdateDto input, CancellationToken cancellationToken = default);

    Task<DictItemDetailDto> UpdateDictItemStatusAsync(DictItemStatusUpdateDto input, CancellationToken cancellationToken = default);

    Task DeleteDictItemAsync(long id, CancellationToken cancellationToken = default);
}
