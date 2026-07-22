// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 资源定义命令应用服务接口
/// </summary>
public interface IResourceAppService : IApplicationService
{
    /// <summary>
    /// 创建资源定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    Task<ResourceDetailDto> CreateResourceAsync(ResourceCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新资源定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    Task<ResourceDetailDto> UpdateResourceAsync(ResourceUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新资源定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    Task<ResourceDetailDto> UpdateResourceStatusAsync(ResourceStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除资源定义
    /// </summary>
    /// <param name="id">资源主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteResourceAsync(long id, CancellationToken cancellationToken = default);
}
