#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AiProviderAppService
// Guid:a11c0de0-4004-4a10-9a00-00000000ai44
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.AI.Application.Contracts;
using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.BasicApp.AI.Application.Mappers;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.Permissions;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.AI.Application.AppServices;

/// <summary>
/// AI Provider 命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.AI", GroupName = "AI 服务", Tag = "AI提供商")]
public sealed class AiProviderAppService : AiApplicationService, IAiProviderAppService
{
    private readonly IAiProviderDomainService _providerDomainService;

    /// <summary>
    /// 框架 provider 解析器（写入后使已缓存 IChatClient 失效，配置热切换）
    /// </summary>
    private readonly IAiChatClientResolver _chatClientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AiProviderAppService(
        IAiProviderDomainService providerDomainService,
        IAiChatClientResolver chatClientResolver)
    {
        _providerDomainService = providerDomainService;
        _chatClientResolver = chatClientResolver;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPermissionCodes.Create)]
    public async Task<AiProviderDetailDto> CreateAsync(AiProviderCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _providerDomainService.CreateProviderAsync(AiProviderApplicationMapper.ToCreateCommand(input), cancellationToken);
        _chatClientResolver.Invalidate();
        return AiProviderApplicationMapper.ToDetailDto(result.Provider);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPermissionCodes.Update)]
    public async Task<AiProviderDetailDto> UpdateAsync(AiProviderUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _providerDomainService.UpdateProviderAsync(AiProviderApplicationMapper.ToUpdateCommand(input), cancellationToken);
        _chatClientResolver.Invalidate();
        return AiProviderApplicationMapper.ToDetailDto(result.Provider);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPermissionCodes.Update)]
    public async Task<AiProviderDetailDto> UpdateStatusAsync(AiProviderStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _providerDomainService.UpdateProviderStatusAsync(AiProviderApplicationMapper.ToStatusCommand(input), cancellationToken);
        _chatClientResolver.Invalidate();
        return AiProviderApplicationMapper.ToDetailDto(result.Provider);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPermissionCodes.Update)]
    public async Task<AiProviderDetailDto> SetDefaultAsync(AiProviderActionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _providerDomainService.SetDefaultAsync(input.BasicId, cancellationToken);
        _chatClientResolver.Invalidate();
        return AiProviderApplicationMapper.ToDetailDto(result.Provider);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(AiPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _providerDomainService.DeleteProviderAsync(id, cancellationToken);
        _chatClientResolver.Invalidate();
    }

    /// <inheritdoc />
    [PermissionAuthorize(AiPermissionCodes.Execute)]
    [HttpPost]
    public async Task<AiProviderTestConnectionResultDto> TestConnectionAsync(AiProviderActionDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _providerDomainService.TestConnectionAsync(input.BasicId, cancellationToken);
        return AiProviderApplicationMapper.ToTestResultDto(result);
    }
}
