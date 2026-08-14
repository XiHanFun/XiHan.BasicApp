// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Application.Mappers;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Printing.Application.AppServices;

/// <summary>
/// 打印模板命令 Dynamic API，负责权限、作用域、事务、缓存失效和结构化审计编排。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "打印模板")]
public sealed class PrintTemplateAppService : PrintingApplicationService, IPrintTemplateAppService
{
    private readonly IPrintTemplateDomainService _domainService;
    private readonly IPrintingCacheInvalidator _cacheInvalidator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionChecker _permissionChecker;
    private readonly ILogger<PrintTemplateAppService> _logger;

    /// <summary>
    /// 初始化打印模板命令服务。
    /// </summary>
    /// <param name="domainService">打印模板领域服务。</param>
    /// <param name="cacheInvalidator">事务感知缓存失效器。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="currentUser">当前用户上下文。</param>
    /// <param name="permissionChecker">平台全局管理权限检查器。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public PrintTemplateAppService(
        IPrintTemplateDomainService domainService,
        IPrintingCacheInvalidator cacheInvalidator,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IPermissionChecker permissionChecker,
        ILogger<PrintTemplateAppService> logger)
    {
        _domainService = domainService;
        _cacheInvalidator = cacheInvalidator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    /// <summary>
    /// 在当前可写作用域创建打印模板。
    /// </summary>
    /// <param name="input">创建参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、JSON 或唯一性校验失败。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(PrintingPermissionCodes.Create)]
    public async Task<PrintTemplateDetailDto> CreatePrintTemplateAsync(
        PrintTemplateCreateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.CreateAsync(
                PrintTemplateApplicationMapper.ToCreateCommand(input), cancellationToken);
            // considerUow:true 使清理在事务提交后执行，不会把未提交数据提前暴露给其它请求。
            await _cacheInvalidator.InvalidatePrintTemplateAsync(cancellationToken);
            LogResult("Create", result.Template.BasicId, result.Template.TemplateCode, input.Scope, true, null);
            return PrintTemplateApplicationMapper.ToDetailDto(result.Template);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Create", 0, input.TemplateCode, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 使用客户端行版本更新打印模板。
    /// </summary>
    /// <param name="input">更新参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、JSON 或并发校验失败。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(PrintingPermissionCodes.Update)]
    public async Task<PrintTemplateDetailDto> UpdatePrintTemplateAsync(
        PrintTemplateUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.UpdateAsync(
                PrintTemplateApplicationMapper.ToUpdateCommand(input), cancellationToken);
            await _cacheInvalidator.InvalidatePrintTemplateAsync(cancellationToken);
            LogResult("Update", result.Template.BasicId, result.Template.TemplateCode, input.Scope, true, null);
            return PrintTemplateApplicationMapper.ToDetailDto(result.Template);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Update", input.BasicId, null, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 使用客户端行版本启用或停用打印模板。
    /// </summary>
    /// <param name="input">状态变更参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>变更后的模板详情。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、状态或并发校验失败。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(PrintingPermissionCodes.Status)]
    public async Task<PrintTemplateDetailDto> UpdatePrintTemplateStatusAsync(
        PrintTemplateStatusUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.UpdateStatusAsync(
                PrintTemplateApplicationMapper.ToStatusCommand(input), cancellationToken);
            await _cacheInvalidator.InvalidatePrintTemplateAsync(cancellationToken);
            LogResult("Status", result.Template.BasicId, result.Template.TemplateCode, input.Scope, true, null);
            return PrintTemplateApplicationMapper.ToDetailDto(result.Template);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Status", input.BasicId, null, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 使用客户端行版本软删除已经停用的打印模板。
    /// </summary>
    /// <param name="input">删除参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板仍启用、无权操作或并发冲突。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(PrintingPermissionCodes.Delete)]
    public async Task DeletePrintTemplateAsync(
        PrintTemplateDeleteDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            await _domainService.DeleteAsync(PrintTemplateApplicationMapper.ToDeleteCommand(input), cancellationToken);
            await _cacheInvalidator.InvalidatePrintTemplateAsync(cancellationToken);
            LogResult("Delete", input.BasicId, null, input.Scope, true, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogResult("Delete", input.BasicId, null, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 限定命令写作用域，并为平台全局模板追加专属权限检查。
    /// </summary>
    private async Task EnsureWritableScopeAsync(PrintTemplateScope scope, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("打印模板作用域无效。");
        }

        var isPlatform = _currentTenant.IsPlatformOperation();
        if (!isPlatform && scope == PrintTemplateScope.Global)
        {
            throw new UserFriendlyException("租户上下文只能维护租户私有打印模板。");
        }

        if (isPlatform && scope == PrintTemplateScope.Tenant)
        {
            throw new UserFriendlyException("平台上下文不能维护未指定租户的私有打印模板。");
        }

        if (!isPlatform)
        {
            return;
        }

        var userId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        if (!await _permissionChecker.IsGrantedAsync(
                userId.ToString(), PrintingPermissionCodes.GlobalManage, cancellationToken))
        {
            throw new UserFriendlyException("缺少全局打印模板管理权限。");
        }
    }

    /// <summary>
    /// 记录不含模板全文、打印数据或客户端令牌的命令审计日志。
    /// </summary>
    private void LogResult(
        string operation,
        long templateId,
        string? templateCode,
        PrintTemplateScope scope,
        bool success,
        Exception? exception)
    {
        var tenantId = _currentTenant.Id ?? 0;
        var operatorId = _currentUser.UserId;
        if (success)
        {
            _logger.LogInformation(
                "打印模板命令成功。Operation={Operation}, TemplateId={TemplateId}, TemplateCode={TemplateCode}, Scope={Scope}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Result=Success",
                operation, templateId, templateCode, scope, tenantId, operatorId);
            return;
        }

        _logger.LogWarning(
            exception,
            "打印模板命令失败。Operation={Operation}, TemplateId={TemplateId}, TemplateCode={TemplateCode}, Scope={Scope}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Result=Failed",
            operation, templateId, templateCode, scope, tenantId, operatorId);
    }
}
