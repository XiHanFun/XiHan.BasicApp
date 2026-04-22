#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityAppService
// Guid:13572468-2468-1357-2468-135724681357
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:34:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 字段级安全应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("field_level_security:read")]
public class FieldLevelSecurityAppService
    : CrudApplicationServiceBase<SysFieldLevelSecurity, FieldLevelSecurityDto, long, FieldLevelSecurityCreateDto, FieldLevelSecurityUpdateDto, BasicAppPRDto>,
        IFieldLevelSecurityAppService
{
    private readonly IFieldLevelSecurityRepository _repository;

    public FieldLevelSecurityAppService(IFieldLevelSecurityRepository repository)
        : base(repository)
    {
        _repository = repository;
    }

    [PermissionAuthorize("field_level_security:create")]
    public override async Task<FieldLevelSecurityDto> CreateAsync(FieldLevelSecurityCreateDto input)
    {
        input.ValidateAnnotations();
        var entity = await MapDtoToEntityAsync(input);
        var created = await _repository.AddAsync(entity);
        return created.Adapt<FieldLevelSecurityDto>()!;
    }

    [PermissionAuthorize("field_level_security:update")]
    public override async Task<FieldLevelSecurityDto> UpdateAsync(FieldLevelSecurityUpdateDto input)
    {
        input.ValidateAnnotations();

        var entity = await _repository.GetByIdAsync(input.BasicId)
                     ?? throw new KeyNotFoundException($"未找到字段级安全策略: {input.BasicId}");

        await MapDtoToEntityAsync(input, entity);
        var updated = await _repository.UpdateAsync(entity);
        return updated.Adapt<FieldLevelSecurityDto>()!;
    }

    [PermissionAuthorize("field_level_security:delete")]
    public override async Task<bool> DeleteAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
        {
            return false;
        }

        return await _repository.DeleteAsync(entity);
    }

    protected override Task<SysFieldLevelSecurity> MapDtoToEntityAsync(FieldLevelSecurityCreateDto createDto)
    {
        var entity = new SysFieldLevelSecurity
        {
            TenantId = createDto.TenantId ?? 0,
            TargetType = createDto.TargetType,
            TargetId = createDto.TargetId,
            ResourceId = createDto.ResourceId,
            FieldName = createDto.FieldName.Trim(),
            IsReadable = createDto.IsReadable,
            IsEditable = createDto.IsEditable,
            MaskStrategy = createDto.MaskStrategy,
            MaskPattern = createDto.MaskPattern?.Trim(),
            Priority = createDto.Priority,
            Description = createDto.Description?.Trim(),
            Remark = createDto.Remark?.Trim(),
            Status = YesOrNo.Yes
        };

        return Task.FromResult(entity);
    }

    protected override Task MapDtoToEntityAsync(FieldLevelSecurityUpdateDto updateDto, SysFieldLevelSecurity entity)
    {
        entity.TargetType = updateDto.TargetType;
        entity.TargetId = updateDto.TargetId;
        entity.ResourceId = updateDto.ResourceId;
        entity.FieldName = updateDto.FieldName.Trim();
        entity.IsReadable = updateDto.IsReadable;
        entity.IsEditable = updateDto.IsEditable;
        entity.MaskStrategy = updateDto.MaskStrategy;
        entity.MaskPattern = updateDto.MaskPattern?.Trim();
        entity.Priority = updateDto.Priority;
        entity.Description = updateDto.Description?.Trim();
        entity.Remark = updateDto.Remark?.Trim();
        entity.Status = updateDto.Status;
        return Task.CompletedTask;
    }
}
