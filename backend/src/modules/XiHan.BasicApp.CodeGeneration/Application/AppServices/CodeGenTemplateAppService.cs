// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.CodeGeneration.Application.AppServices;

/// <summary>
/// 代码生成模板命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "模板")]
public sealed class CodeGenTemplateAppService : CodeGenerationApplicationService, ICodeGenTemplateAppService
{
    private readonly ICodeGenTemplateDomainService _templateDomainService;

    private readonly ITemplateRendererResolver _rendererResolver;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTemplateAppService(
        ICodeGenTemplateDomainService templateDomainService,
        ITemplateRendererResolver rendererResolver,
        IFieldSecurityService fieldSecurity)
    {
        _templateDomainService = templateDomainService;
        _rendererResolver = rendererResolver;
        _fieldSecurity = fieldSecurity;
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Create)]
    public async Task<CodeGenTemplateDetailDto> CreateAsync(CodeGenTemplateCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysCodeGenTemplate), input, cancellationToken);

        var result = await _templateDomainService.CreateTemplateAsync(CodeGenTemplateApplicationMapper.ToCreateCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTemplateDetailDto> UpdateAsync(CodeGenTemplateUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysCodeGenTemplate), input.BasicId, input, cancellationToken);

        var result = await _templateDomainService.UpdateTemplateAsync(CodeGenTemplateApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 更新模板状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTemplateDetailDto> UpdateStatusAsync(CodeGenTemplateStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysCodeGenTemplate), input.BasicId, input, cancellationToken);

        var result = await _templateDomainService.UpdateTemplateStatusAsync(CodeGenTemplateApplicationMapper.ToStatusCommand(input), cancellationToken);
        return CodeGenTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _templateDomainService.DeleteTemplateAsync(id, cancellationToken);
    }

    /// <summary>
    /// 校验模板语法（保存前预检）
    /// </summary>
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
