// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// AI 提示词命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "提示词")]
public sealed class AiPromptAppService : AiApplicationService, IAiPromptAppService
{
    private readonly IAiPromptDomainService _promptDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiPromptAppService(IAiPromptDomainService promptDomainService)
    {
        _promptDomainService = promptDomainService;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Create)]
    public async Task<AiPromptDetailDto> CreateAsync(AiPromptCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _promptDomainService.CreatePromptAsync(AiPromptApplicationMapper.ToCreateCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Update)]
    public async Task<AiPromptDetailDto> UpdateAsync(AiPromptUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _promptDomainService.UpdatePromptAsync(AiPromptApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Update)]
    public async Task<AiPromptDetailDto> UpdateStatusAsync(AiPromptStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _promptDomainService.UpdatePromptStatusAsync(AiPromptApplicationMapper.ToStatusCommand(input), cancellationToken);
        return AiPromptApplicationMapper.ToDetailDto(result.Prompt);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPromptPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _promptDomainService.DeletePromptAsync(id, cancellationToken);
    }
}
