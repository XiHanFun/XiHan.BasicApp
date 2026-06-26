#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StorageConfigAppService
// Guid:e2b7d5f9-8a14-4c63-b0e8-7f3a9c1d5e26
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 存储配置命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "存储配置")]
public sealed class StorageConfigAppService
    : SaasApplicationService, IStorageConfigAppService
{
    private readonly IStorageConfigDomainService _storageConfigDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StorageConfigAppService(IStorageConfigDomainService storageConfigDomainService)
    {
        _storageConfigDomainService = storageConfigDomainService;
    }

    /// <summary>
    /// 创建存储配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Create)]
    public async Task<StorageConfigDetailDto> CreateStorageConfigAsync(StorageConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _storageConfigDomainService.CreateStorageConfigAsync(
            StorageConfigApplicationMapper.ToCreateCommand(input),
            cancellationToken);

        return StorageConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新存储配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Update)]
    public async Task<StorageConfigDetailDto> UpdateStorageConfigAsync(StorageConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _storageConfigDomainService.UpdateStorageConfigAsync(
            StorageConfigApplicationMapper.ToUpdateCommand(input),
            cancellationToken);

        return StorageConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新存储配置启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Status)]
    public async Task<StorageConfigDetailDto> UpdateStorageConfigStatusAsync(StorageConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _storageConfigDomainService.UpdateStorageConfigStatusAsync(
            StorageConfigApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        return StorageConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 设置默认存储配置
    /// </summary>
    /// <remarks>Set 前缀不在动词约定表，保留完整方法名，默认 POST</remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Update)]
    public async Task<StorageConfigDetailDto> SetDefaultStorageConfigAsync(StorageConfigDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _storageConfigDomainService.SetDefaultStorageConfigAsync(
            StorageConfigApplicationMapper.ToDefaultCommand(input),
            cancellationToken);

        return StorageConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 删除存储配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.StorageConfig.Delete)]
    public async Task DeleteStorageConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _storageConfigDomainService.DeleteStorageConfigAsync(id, cancellationToken);
    }
}
