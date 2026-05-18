#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictAppService
// Guid:7916f366-9883-4cf6-a61a-55e1bb99cfae
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
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
/// 系统字典命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统字典")]
public sealed class DictAppService
    : SaasApplicationService, IDictAppService
{
    private readonly IDictDomainService _dictDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictAppService(IDictDomainService dictDomainService)
    {
        _dictDomainService = dictDomainService;
    }
    /// <summary>
    /// 创建系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictDetailDto> CreateDictAsync(DictCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.CreateDictAsync(DictApplicationMapper.ToCreateCommand(input), cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }

    /// <summary>
    /// 创建系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Create)]
    public async Task<DictItemDetailDto> CreateDictItemAsync(DictItemCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.CreateDictItemAsync(DictApplicationMapper.ToItemCreateCommand(input), cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    /// <summary>
    /// 删除系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
    public async Task DeleteDictAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dictDomainService.DeleteDictAsync(id, cancellationToken);
    }

    /// <summary>
    /// 删除系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Delete)]
    public async Task DeleteDictItemAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dictDomainService.DeleteDictItemAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新系统字典
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictDetailDto> UpdateDictAsync(DictUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.UpdateDictAsync(DictApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }

    /// <summary>
    /// 更新系统字典项
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Update)]
    public async Task<DictItemDetailDto> UpdateDictItemAsync(DictItemUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.UpdateDictItemAsync(DictApplicationMapper.ToItemUpdateCommand(input), cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    /// <summary>
    /// 更新系统字典项状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictItemDetailDto> UpdateDictItemStatusAsync(DictItemStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.UpdateDictItemStatusAsync(DictApplicationMapper.ToItemStatusCommand(input), cancellationToken);
        return DictApplicationMapper.ToItemDetailDto(result.DictItem);
    }

    /// <summary>
    /// 更新系统字典状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Dict.Status)]
    public async Task<DictDetailDto> UpdateDictStatusAsync(DictStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dictDomainService.UpdateDictStatusAsync(DictApplicationMapper.ToStatusCommand(input), cancellationToken);
        return DictApplicationMapper.ToDetailDto(result.Dict);
    }
}
