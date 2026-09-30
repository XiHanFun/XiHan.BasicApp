// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// 字段级安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段级安全")]
public sealed class FieldLevelSecurityAppService
    : SaasApplicationService, IFieldLevelSecurityAppService
{
    private readonly IFieldSecurityEntityCatalog _catalog;

    private readonly IFieldLevelSecurityDomainService _fieldLevelSecurityDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityAppService(IFieldLevelSecurityDomainService fieldLevelSecurityDomainService, IFieldSecurityEntityCatalog catalog)
    {
        _fieldLevelSecurityDomainService = fieldLevelSecurityDomainService;
        _catalog = catalog;
    }

    /// <summary>
    /// 创建字段级安全规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Create)]
    public async Task<FieldLevelSecurityDetailDto> CreateFieldLevelSecurityAsync(FieldLevelSecurityCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.CreateAsync(FieldLevelSecurityApplicationMapper.ToCreateCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, _catalog, result.TargetCode, result.TargetName);
    }

    /// <summary>
    /// 删除字段级安全规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Delete)]
    public async Task DeleteFieldLevelSecurityAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldLevelSecurityDomainService.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新字段级安全规则
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Update)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityAsync(FieldLevelSecurityUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.UpdateAsync(FieldLevelSecurityApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, _catalog, result.TargetCode, result.TargetName);
    }

    /// <summary>
    /// 更新字段级安全规则状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Status)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityStatusAsync(FieldLevelSecurityStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.UpdateStatusAsync(FieldLevelSecurityApplicationMapper.ToStatusCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, _catalog, result.TargetCode, result.TargetName);
    }
}
