#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentAppService
// Guid:c67b39a5-836c-4adc-bc20-d8bf5a26e82c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
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
/// 部门命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "部门")]
public sealed class DepartmentAppService
    : SaasApplicationService, IDepartmentAppService
{
    private readonly IDepartmentDomainService _departmentDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentAppService(IDepartmentDomainService departmentDomainService, ISaasCacheInvalidator cacheInvalidator)
    {
        _departmentDomainService = departmentDomainService;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Create)]
    public async Task<DepartmentDetailDto> CreateDepartmentAsync(DepartmentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _departmentDomainService.CreateAsync(DepartmentApplicationMapper.ToCreateCommand(input), cancellationToken);

        // 部门结构变更影响部门树缓存，统一失效
        await _cacheInvalidator.InvalidateOrganizationAsync(cancellationToken);

        return DepartmentApplicationMapper.ToDetailDto(result.Department);
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Delete)]
    public async Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _departmentDomainService.DeleteAsync(id, cancellationToken);

        // 部门删除影响部门树缓存，统一失效
        await _cacheInvalidator.InvalidateOrganizationAsync(cancellationToken);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Update)]
    public async Task<DepartmentDetailDto> UpdateDepartmentAsync(DepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _departmentDomainService.UpdateAsync(DepartmentApplicationMapper.ToUpdateCommand(input), cancellationToken);

        // 部门结构变更影响部门树缓存，统一失效
        await _cacheInvalidator.InvalidateOrganizationAsync(cancellationToken);

        return DepartmentApplicationMapper.ToDetailDto(result.Department);
    }

    /// <summary>
    /// 更新部门状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Status)]
    public async Task<DepartmentDetailDto> UpdateDepartmentStatusAsync(DepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _departmentDomainService.UpdateStatusAsync(DepartmentApplicationMapper.ToStatusCommand(input), cancellationToken);

        // 部门启停影响部门树缓存，统一失效
        await _cacheInvalidator.InvalidateOrganizationAsync(cancellationToken);

        return DepartmentApplicationMapper.ToDetailDto(result.Department);
    }
}
