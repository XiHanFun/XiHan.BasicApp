// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限目录领域服务
/// </summary>
public interface IPermissionCatalogDomainService
{
    /// <summary>
    /// 创建权限定义
    /// </summary>
    Task<PermissionCatalogCommandResult> CreatePermissionAsync(PermissionCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除权限定义
    /// </summary>
    Task DeletePermissionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限定义
    /// </summary>
    Task<PermissionCatalogCommandResult> UpdatePermissionAsync(PermissionUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新权限状态
    /// </summary>
    Task<PermissionCatalogCommandResult> UpdatePermissionStatusAsync(PermissionStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建资源定义
    /// </summary>
    Task<ResourceCatalogCommandResult> CreateResourceAsync(ResourceCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除资源定义
    /// </summary>
    Task DeleteResourceAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新资源定义
    /// </summary>
    Task<ResourceCatalogCommandResult> UpdateResourceAsync(ResourceUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新资源状态
    /// </summary>
    Task<ResourceCatalogCommandResult> UpdateResourceStatusAsync(ResourceStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建操作定义
    /// </summary>
    Task<OperationCatalogCommandResult> CreateOperationAsync(OperationCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除操作定义
    /// </summary>
    Task DeleteOperationAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新操作定义
    /// </summary>
    Task<OperationCatalogCommandResult> UpdateOperationAsync(OperationUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新操作状态
    /// </summary>
    Task<OperationCatalogCommandResult> UpdateOperationStatusAsync(OperationStatusCommand command, CancellationToken cancellationToken = default);
}
