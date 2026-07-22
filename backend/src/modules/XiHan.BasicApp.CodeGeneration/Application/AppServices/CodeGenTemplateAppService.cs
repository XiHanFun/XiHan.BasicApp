// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.CodeGeneration.Application.AppServices;

/// <summary>
/// 代码生成模板命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "模板")]
public sealed class CodeGenTemplateAppService : CodeGenerationApplicationService, ICodeGenTemplateAppService
{
    private readonly ICodeGenTemplateDomainService _templateDomainService;

    private readonly ITemplateRendererResolver _rendererResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTemplateAppService(
        ICodeGenTemplateDomainService templateDomainService,
        ITemplateRendererResolver rendererResolver)
    {
        _templateDomainService = templateDomainService;
        _rendererResolver = rendererResolver;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Create)]
    public async Task<CodeGenTemplateDetailDto> CreateAsync(CodeGenTemplateCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _templateDomainService.CreateTemplateAsync(CodeGenTemplateApplicationMapper.ToCreateCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTemplateDetailDto> UpdateAsync(CodeGenTemplateUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _templateDomainService.UpdateTemplateAsync(CodeGenTemplateApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTemplateDetailDto> UpdateStatusAsync(CodeGenTemplateStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _templateDomainService.UpdateTemplateStatusAsync(CodeGenTemplateApplicationMapper.ToStatusCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _templateDomainService.DeleteTemplateAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public Task<CodeGenTemplateValidateResultDto> ValidateAsync(CodeGenTemplateValidateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = _rendererResolver.Resolve(input.TemplateEngine).Validate(input.TemplateContent ?? string.Empty);
        var result = new CodeGenTemplateValidateResultDto
        {
            IsValid = validation.IsValid,
            Errors = validation.Errors
        };
        return Task.FromResult(result);
    }
}
