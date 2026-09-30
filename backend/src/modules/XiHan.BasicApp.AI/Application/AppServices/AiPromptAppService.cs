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
/// AI 提示词命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "提示词")]
public sealed class AiPromptAppService : AiApplicationService, IAiPromptAppService
{
    private readonly IAiPromptDomainService _promptDomainService;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiPromptAppService(IAiPromptDomainService promptDomainService, IFieldSecurityService fieldSecurity)
    {
        _promptDomainService = promptDomainService;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 创建提示词
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Create)]
    public async Task<AiPromptDetailDto> CreateAsync(AiPromptCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysAiPrompt), input, cancellationToken);

        var result = await _promptDomainService.CreatePromptAsync(AiPromptApplicationMapper.ToCreateCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <summary>
    /// 更新提示词
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Update)]
    public async Task<AiPromptDetailDto> UpdateAsync(AiPromptUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAiPrompt), input.BasicId, input, cancellationToken);

        var result = await _promptDomainService.UpdatePromptAsync(AiPromptApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <summary>
    /// 更新提示词状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Update)]
    public async Task<AiPromptDetailDto> UpdateStatusAsync(AiPromptStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysAiPrompt), input.BasicId, input, cancellationToken);

        var result = await _promptDomainService.UpdatePromptStatusAsync(AiPromptApplicationMapper.ToStatusCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <summary>
    /// 删除提示词
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _promptDomainService.DeletePromptAsync(id, cancellationToken);
    }
}
