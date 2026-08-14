// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using XiHan.BasicApp.Printing.Application.Caching;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Printing.Domain.Repositories;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Printing.Application.Services;

/// <summary>
/// 租户感知的打印模板解析器，负责 Auto/Tenant/Global 解析顺序和分布式缓存。
/// </summary>
/// <remarks>
/// 本服务不缓存打印数据，只缓存模板元数据与设计 JSON。键同时包含请求租户、请求作用域和模板编码，防止跨租户串用。
/// </remarks>
public sealed class PrintTemplateResolver : IPrintTemplateResolver
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(30);

    private readonly IPrintTemplateRepository _repository;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDistributedCache<PrintTemplateCacheItem, string> _cache;

    /// <summary>
    /// 初始化打印模板解析器。
    /// </summary>
    /// <param name="repository">打印模板仓储。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="cache">打印模板分布式缓存。</param>
    public PrintTemplateResolver(
        IPrintTemplateRepository repository,
        ICurrentTenant currentTenant,
        IDistributedCache<PrintTemplateCacheItem, string> cache)
    {
        _repository = repository;
        _currentTenant = currentTenant;
        _cache = cache;
    }

    /// <summary>
    /// 按当前租户、请求作用域和模板编码解析启用模板。
    /// </summary>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="scope">解析作用域。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>命中的模板；不存在时返回 <see langword="null"/>。</returns>
    public async Task<ResolvedPrintTemplateDto?> ResolveAsync(
        string templateCode,
        PrintTemplateScope scope = PrintTemplateScope.Auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("打印模板作用域无效。");
        }

        var normalizedCode = templateCode.Trim();
        if (normalizedCode.Any(char.IsWhiteSpace) || normalizedCode.Length > 100)
        {
            throw new UserFriendlyException("打印模板编码无效。");
        }

        var requestTenantId = _currentTenant.Id ?? 0;
        if (requestTenantId == 0 && scope == PrintTemplateScope.Tenant)
        {
            throw new UserFriendlyException("平台上下文不能解析未指定租户的私有打印模板。");
        }

        var cacheKey = PrintingCacheKeys.PrintTemplate(requestTenantId, (int)scope, normalizedCode);
        var item = await _cache.GetOrAddAsync(
            cacheKey,
            () => ResolveCacheItemAsync(requestTenantId, normalizedCode, scope, cancellationToken),
            static () => new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheLifetime },
            hideErrors: true,
            token: cancellationToken);

        if (item is null || !item.Found)
        {
            return null;
        }

        return new ResolvedPrintTemplateDto
        {
            BasicId = item.BasicId,
            TemplateCode = item.TemplateCode,
            DataSourceCode = item.DataSourceCode,
            TemplateName = item.TemplateName,
            TemplateJson = item.TemplateJson,
            EngineVersion = item.EngineVersion,
            RequestedScope = scope,
            ResolvedScope = item.ResolvedScope,
            RowVersion = item.RowVersion.ToString(CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// 按明确语义读取数据库并构造正缓存或负缓存项。
    /// </summary>
    private async Task<PrintTemplateCacheItem> ResolveCacheItemAsync(
        long requestTenantId,
        string templateCode,
        PrintTemplateScope scope,
        CancellationToken cancellationToken)
    {
        SysPrintTemplate? template;
        PrintTemplateScope resolvedScope;

        if (requestTenantId == 0)
        {
            // 平台上下文的 Auto 与 Global 含义一致，且不要求 AllowTenantUse。
            template = await FindGlobalAsync(templateCode, requireTenantOpen: false, cancellationToken);
            resolvedScope = PrintTemplateScope.Global;
        }
        else if (scope == PrintTemplateScope.Tenant)
        {
            template = await _repository.FindByCodeInScopeAsync(requestTenantId, templateCode, true, cancellationToken);
            resolvedScope = PrintTemplateScope.Tenant;
        }
        else if (scope == PrintTemplateScope.Global)
        {
            template = await FindGlobalAsync(templateCode, requireTenantOpen: true, cancellationToken);
            resolvedScope = PrintTemplateScope.Global;
        }
        else
        {
            // Auto 明确允许租户模板不存在或停用时回退；显式 Tenant/Global 分支不会发生回退。
            template = await _repository.FindByCodeInScopeAsync(requestTenantId, templateCode, true, cancellationToken);
            resolvedScope = PrintTemplateScope.Tenant;
            if (template is null)
            {
                template = await FindGlobalAsync(templateCode, requireTenantOpen: true, cancellationToken);
                resolvedScope = PrintTemplateScope.Global;
            }
        }

        if (template is null)
        {
            return new PrintTemplateCacheItem
            {
                Found = false,
                TemplateCode = templateCode,
                CachedAt = DateTimeOffset.UtcNow
            };
        }

        return new PrintTemplateCacheItem
        {
            Found = true,
            BasicId = template.BasicId,
            TemplateCode = template.TemplateCode,
            DataSourceCode = template.DataSourceCode,
            TemplateName = template.TemplateName,
            TemplateJson = template.TemplateJson,
            EngineVersion = template.EngineVersion,
            ResolvedScope = resolvedScope,
            RowVersion = template.RowVersion,
            CachedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 在平台数据库上下文中查找启用的全局模板，并按调用来源执行开放状态限制。
    /// </summary>
    private async Task<SysPrintTemplate?> FindGlobalAsync(
        string templateCode,
        bool requireTenantOpen,
        CancellationToken cancellationToken)
    {
        async Task<SysPrintTemplate?> QueryAsync()
        {
            var template = await _repository.FindByCodeInScopeAsync(0, templateCode, true, cancellationToken);
            return template is not null && (!requireTenantOpen || template.AllowTenantUse) ? template : null;
        }

        if (_currentTenant.IsPlatformOperation())
        {
            return await QueryAsync();
        }

        // 独立租户库必须把平台上下文保持到查询 await 完成，避免仓储解析回租户连接。
        using var platformScope = _currentTenant.Change(null);
        return await QueryAsync();
    }
}
