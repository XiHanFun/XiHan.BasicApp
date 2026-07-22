// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 存储配置命令应用服务接口
/// </summary>
public interface IStorageConfigAppService : IApplicationService
{
    /// <summary>
    /// 创建存储配置
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    Task<StorageConfigDetailDto> CreateStorageConfigAsync(StorageConfigCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新存储配置
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    Task<StorageConfigDetailDto> UpdateStorageConfigAsync(StorageConfigUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新存储配置启停状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    Task<StorageConfigDetailDto> UpdateStorageConfigStatusAsync(StorageConfigStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认存储配置
    /// </summary>
    /// <param name="input">默认更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存储配置详情</returns>
    Task<StorageConfigDetailDto> SetDefaultStorageConfigAsync(StorageConfigDefaultUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除存储配置
    /// </summary>
    /// <param name="id">存储配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteStorageConfigAsync(long id, CancellationToken cancellationToken = default);
}
