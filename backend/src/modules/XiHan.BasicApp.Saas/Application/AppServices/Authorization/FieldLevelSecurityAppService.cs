#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityAppService
// Guid:88ff96f5-6968-4021-9c5b-310928906521
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
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
/// 字段级安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段级安全")]
public sealed class FieldLevelSecurityAppService
    : SaasApplicationService, IFieldLevelSecurityAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityAppService(IFieldLevelSecurityDomainService fieldLevelSecurityDomainService)
    {
        _fieldLevelSecurityDomainService = fieldLevelSecurityDomainService;
    }

    private readonly IFieldLevelSecurityDomainService _fieldLevelSecurityDomainService;

    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Create)]
    public async Task<FieldLevelSecurityDetailDto> CreateFieldLevelSecurityAsync(FieldLevelSecurityCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.CreateAsync(ToCreateCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, result.Resource, result.TargetCode, result.TargetName);
    }

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Delete)]
    public async Task DeleteFieldLevelSecurityAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _fieldLevelSecurityDomainService.DeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Update)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityAsync(FieldLevelSecurityUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.UpdateAsync(ToUpdateCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, result.Resource, result.TargetCode, result.TargetName);
    }

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Status)]
    public async Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityStatusAsync(FieldLevelSecurityStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _fieldLevelSecurityDomainService.UpdateStatusAsync(ToStatusCommand(input), cancellationToken);
        return FieldLevelSecurityApplicationMapper.ToDetailDto(result.Policy, result.Resource, result.TargetCode, result.TargetName);
    }

    private static FieldLevelSecurityCreateCommand ToCreateCommand(FieldLevelSecurityCreateDto input)
    {
        return new FieldLevelSecurityCreateCommand(
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.MaskPattern,
            input.Priority,
            input.Description,
            input.Status,
            input.Remark);
    }

    private static FieldLevelSecurityUpdateCommand ToUpdateCommand(FieldLevelSecurityUpdateDto input)
    {
        return new FieldLevelSecurityUpdateCommand(
            input.BasicId,
            input.TargetType,
            input.TargetId,
            input.ResourceId,
            input.FieldName,
            input.IsReadable,
            input.IsEditable,
            input.MaskStrategy,
            input.MaskPattern,
            input.Priority,
            input.Description,
            input.Remark);
    }

    private static FieldLevelSecurityStatusChangeCommand ToStatusCommand(FieldLevelSecurityStatusUpdateDto input)
    {
        return new FieldLevelSecurityStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }
}
