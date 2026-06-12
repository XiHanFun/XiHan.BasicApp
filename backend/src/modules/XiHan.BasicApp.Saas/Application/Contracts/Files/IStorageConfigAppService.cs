#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IStorageConfigAppService
// Guid:c4e8b1d6-7f29-4a53-8b0e-2d9c5f7a3e61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
