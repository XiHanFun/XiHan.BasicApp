// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// AI 助手命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "AI助手")]
public sealed class AiAssistantAppService : AiApplicationService, IAiAssistantAppService
{
    private readonly IAiAssistantDomainService _assistantDomainService;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiAssistantAppService(IAiAssistantDomainService assistantDomainService, IFieldSecurityService fieldSecurity)
    {
        _assistantDomainService = assistantDomainService;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 创建助手
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiAssistantPermissionCodes.Create)]
    public async Task<AiAssistantDetailDto> CreateAsync(AiAssistantCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysAiAssistant), input, cancellationToken);

        var result = await _assistantDomainService.CreateAssistantAsync(AiAssistantApplicationMapper.ToCreateCommand(input), cancellationToken);
        return AiAssistantApplicationMapper.ToDetailDto(result.Assistant);
    }

    /// <summary>
    /// 更新助手
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiAssistantPermissionCodes.Update)]
    public async Task<AiAssistantDetailDto> UpdateAsync(AiAssistantUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAiAssistant), input.BasicId, input, cancellationToken);

        var result = await _assistantDomainService.UpdateAssistantAsync(AiAssistantApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return AiAssistantApplicationMapper.ToDetailDto(result.Assistant);
    }

    /// <summary>
    /// 更新助手状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiAssistantPermissionCodes.Update)]
    public async Task<AiAssistantDetailDto> UpdateStatusAsync(AiAssistantStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAiAssistant), input.BasicId, input, cancellationToken);

        var result = await _assistantDomainService.UpdateAssistantStatusAsync(AiAssistantApplicationMapper.ToStatusCommand(input), cancellationToken);
        return AiAssistantApplicationMapper.ToDetailDto(result.Assistant);
    }

    /// <summary>
    /// 设为默认助手
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiAssistantPermissionCodes.Update)]
    public async Task<AiAssistantDetailDto> SetDefaultAsync(AiAssistantActionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _assistantDomainService.SetDefaultAsync(input.BasicId, cancellationToken);
        return AiAssistantApplicationMapper.ToDetailDto(result.Assistant);
    }

    /// <summary>
    /// 删除助手
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiAssistantPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _assistantDomainService.DeleteAssistantAsync(id, cancellationToken);
    }
}
