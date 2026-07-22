// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 存储配置领域服务
/// </summary>
/// <remarks>
/// 不变式：
/// - ConfigCode 在租户内唯一
/// - 同一租户有且仅有一条 IsDefault=true 的记录（设默认时清其它默认位）
/// - 默认配置必须处于启用状态；默认配置不可停用、不可删除
/// - 被 SysFileStorage 引用的配置禁止删除
/// </remarks>
public interface IStorageConfigDomainService
{
    /// <summary>
    /// 创建存储配置
    /// </summary>
    Task<StorageConfigCommandResult> CreateStorageConfigAsync(StorageConfigCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新存储配置
    /// </summary>
    Task<StorageConfigCommandResult> UpdateStorageConfigAsync(StorageConfigUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新存储配置启停状态
    /// </summary>
    Task<StorageConfigCommandResult> UpdateStorageConfigStatusAsync(StorageConfigStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认存储配置
    /// </summary>
    Task<StorageConfigCommandResult> SetDefaultStorageConfigAsync(StorageConfigDefaultChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除存储配置
    /// </summary>
    Task<StorageConfigCommandResult> DeleteStorageConfigAsync(long id, CancellationToken cancellationToken = default);
}
