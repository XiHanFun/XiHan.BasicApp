// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 打印模板查询 Dynamic API，负责字段安全、租户范围、全局开放限制和按编码解析。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "打印模板")]
public sealed class PrintTemplateQueryService : SaasApplicationService, IPrintTemplateQueryService
{
    private readonly IPrintTemplateRepository _repository;
    private readonly IPrintTemplateResolver _resolver;
    private readonly IFieldSecurityService _fieldSecurity;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 初始化打印模板查询服务。
    /// </summary>
    /// <param name="repository">打印模板仓储。</param>
    /// <param name="resolver">租户感知模板解析器。</param>
    /// <param name="fieldSecurity">字段过滤与排序安全服务。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    public PrintTemplateQueryService(
        IPrintTemplateRepository repository,
        IPrintTemplateResolver resolver,
        IFieldSecurityService fieldSecurity,
        ICurrentTenant currentTenant)
    {
        _repository = repository;
        _resolver = resolver;
        _fieldSecurity = fieldSecurity;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.PrintTemplate.Read)]
    public async Task<PageResultDtoBase<PrintTemplateListItemDto>> GetPrintTemplatePageAsync(
        PrintTemplatePageQueryDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        var scope = ResolveQueryScope(input.Scope);
        return await ExecuteInScopeAsync(
            scope,
            () => QueryPageCoreAsync(input, scope, availableGlobalOnly: scope.IsGlobal && scope.RequestTenantId > 0, cancellationToken));
    }

    /// <inheritdoc />
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.PrintTemplate.Read)]
    public async Task<PageResultDtoBase<PrintTemplateListItemDto>> GetAvailableGlobalPrintTemplatePageAsync(
        PrintTemplatePageQueryDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        var scope = new PrintTemplateQueryScope(0, true, _currentTenant.Id ?? 0);
        return await ExecuteInScopeAsync(
            scope,
            () => QueryPageCoreAsync(input, scope, availableGlobalOnly: true, cancellationToken));
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.PrintTemplate.Read)]
    public async Task<PrintTemplateDetailDto?> GetPrintTemplateDetailAsync(
        long id,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new UserFriendlyException("打印模板主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var queryScope = ResolveQueryScope(scope);
        var template = await ExecuteInScopeAsync(
            queryScope,
            () => _repository.FindByIdInScopeAsync(queryScope.OwnerTenantId, id, cancellationToken));
        if (template is null)
        {
            return null;
        }

        if (queryScope.IsGlobal
            && queryScope.RequestTenantId > 0
            && (template.Status != EnableStatus.Enabled || !template.AllowTenantUse))
        {
            throw new UserFriendlyException("该全局打印模板未向当前租户开放。");
        }

        return PrintTemplateApplicationMapper.ToDetailDto(template);
    }

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.PrintTemplate.Use)]
    public async Task<ResolvedPrintTemplateDto> GetResolvedPrintTemplateByCodeAsync(
        string templateCode,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();
        return await _resolver.ResolveAsync(templateCode, scope, cancellationToken)
            ?? throw new UserFriendlyException("未找到可用的打印模板；请检查模板编码、作用域、启停状态和全局开放状态。");
    }

    /// <summary>
    /// 执行分页查询，并在客户端字段安全检查后追加不可覆盖的租户与开放状态约束。
    /// </summary>
    private async Task<PageResultDtoBase<PrintTemplateListItemDto>> QueryPageCoreAsync(
        PrintTemplatePageQueryDto input,
        PrintTemplateQueryScope scope,
        bool availableGlobalOnly,
        CancellationToken cancellationToken)
    {
        var request = BuildPageRequest(input);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, nameof(SysPrintTemplate), cancellationToken);
        await _fieldSecurity.GuardSortsAsync(request.Conditions, nameof(SysPrintTemplate), cancellationToken);

        // 内部约束在字段安全处理后追加，前端无法通过自定义 filters 覆盖租户边界。
        request.Conditions.AddFilter((SysPrintTemplate template) => template.TenantId, scope.OwnerTenantId);
        if (availableGlobalOnly)
        {
            request.Conditions.AddFilter((SysPrintTemplate template) => template.AllowTenantUse, true);
            request.Conditions.AddFilter((SysPrintTemplate template) => template.Status, EnableStatus.Enabled);
        }

        if (request.Conditions.Sorts.Count == 0)
        {
            request.Conditions.AddSort((SysPrintTemplate template) => template.Sort, SortDirection.Ascending, 0);
            request.Conditions.AddSort((SysPrintTemplate template) => template.TemplateCode, SortDirection.Ascending, 1);
        }

        var page = await _repository.GetPagedAsync(request, cancellationToken);
        return new PageResultDtoBase<PrintTemplateListItemDto>(
            page.Items.Select(PrintTemplateApplicationMapper.ToListItemDto).ToList(),
            page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <summary>
    /// 构建尚未包含服务端作用域约束的分页请求。
    /// </summary>
    private static BasicAppPRDto BuildPageRequest(PrintTemplatePageQueryDto input)
    {
        var request = new BasicAppPRDto { Page = input.Page, Conditions = new QueryConditions() };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysPrintTemplate>(
                input.Keyword.Trim(),
                template => template.TemplateCode,
                template => template.DataSourceCode,
                template => template.TemplateName,
                template => template.Remark);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysPrintTemplate template) => template.Status, input.Status.Value);
        }

        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        return request;
    }

    /// <summary>
    /// 把外部作用域解析为实体所属租户和数据库位置。
    /// </summary>
    private PrintTemplateQueryScope ResolveQueryScope(PrintTemplateScope scope)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("打印模板作用域无效。");
        }

        var requestTenantId = _currentTenant.Id ?? 0;
        if (requestTenantId == 0)
        {
            if (scope == PrintTemplateScope.Tenant)
            {
                throw new UserFriendlyException("平台上下文不能查询未指定租户的私有打印模板。");
            }

            return new PrintTemplateQueryScope(0, true, 0);
        }

        return scope == PrintTemplateScope.Global
            ? new PrintTemplateQueryScope(0, true, requestTenantId)
            : new PrintTemplateQueryScope(requestTenantId, false, requestTenantId);
    }

    /// <summary>
    /// 必要时切换到平台上下文执行完整异步查询，并在完成后恢复原租户。
    /// </summary>
    private async Task<TResult> ExecuteInScopeAsync<TResult>(
        PrintTemplateQueryScope scope,
        Func<Task<TResult>> action)
    {
        if (scope.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            using var platformScope = _currentTenant.Change(null);
            return await action();
        }

        return await action();
    }

    /// <summary>
    /// 查询作用域内部值对象。
    /// </summary>
    /// <param name="OwnerTenantId">实体所属租户，0 表示全局。</param>
    /// <param name="IsGlobal">是否读取平台数据库。</param>
    /// <param name="RequestTenantId">原始请求租户，0 表示平台。</param>
    private sealed record PrintTemplateQueryScope(long OwnerTenantId, bool IsGlobal, long RequestTenantId);
}
