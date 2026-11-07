// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Printing.Domain.Services;

/// <summary>
/// 按编码与作用域解析可打印的模板
/// </summary>
/// <remarks>
/// 解析顺序（见 <see cref="PrintTemplateScope"/>）：平台一侧只取启用的全局模板、不要求开放；租户一侧的 <c>Auto</c> 先取本租户启用的、没有再取启用且开放的全局模板，
/// <c>Tenant</c> 与 <c>Global</c> 不回退。各侧「某编码的启用模板」进共享缓存（键 <c>printing:templates:&lt;侧&gt;:&lt;编码&gt;</c>，30 分钟，「没有」也缓存），
/// 解析时按请求一侧合成；模板写入后按所属一侧与编码失效（推到工作单元之后，Access 各缓存同一写法）
/// 缓存的是模板元数据与设计 JSON，不缓存打印数据
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="cache">共享缓存</param>
public sealed class PrintTemplateResolver(IRepository<SysPrintTemplate, Guid> templates, ISharedCache cache)
{
    /// <summary>
    /// 缓存存活时间
    /// </summary>
    public static readonly TimeSpan CacheTimeToLive = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 解析出模板
    /// </summary>
    /// <param name="code">模板编码</param>
    /// <param name="scope">作用域</param>
    /// <param name="side">请求所在的一侧</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模板与实际命中的作用域；平台用 <c>Tenant</c> 即 <c>PRINTING_VALIDATION_004</c>，解析不到 <c>PRINTING_NOTFOUND_002</c></returns>
    public async ValueTask<Result<(CachedPrintTemplate Template, PrintTemplateScope Resolved)>> ResolveAsync(string code, PrintTemplateScope scope, string side, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        if (PrintTemplateService.IsPlatform(side))
        {
            if (scope == PrintTemplateScope.Tenant)
            {
                return Error.Validation(PrintingErrors.ScopeInvalid, PrintingMessages.ScopeInvalid);
            }

            // 平台一侧的 Auto 与 Global 相同，且不要求向租户开放
            var global = await FindAsync(TenantIds.Platform, code, cancellationToken).ConfigureAwait(false);
            return global is null ? NoUsable(code, scope) : (global, PrintTemplateScope.Global);
        }

        if (scope != PrintTemplateScope.Global)
        {
            var own = await FindAsync(side, code, cancellationToken).ConfigureAwait(false);
            if (own is not null)
            {
                return (own, PrintTemplateScope.Tenant);
            }

            if (scope == PrintTemplateScope.Tenant)
            {
                return NoUsable(code, scope);
            }
        }

        var shared = await FindAsync(TenantIds.Platform, code, cancellationToken).ConfigureAwait(false);
        return shared is { AllowTenantUse: true } ? (shared, PrintTemplateScope.Global) : NoUsable(code, scope);
    }

    /// <summary>
    /// 失效一个模板在缓存里的那一项
    /// </summary>
    /// <remarks>
    /// 新建、修改、启停、删除之后调用；调用里推到工作单元结束之后再删除
    /// </remarks>
    /// <param name="cache">共享缓存</param>
    /// <param name="template">模板（编码与所属一侧不会变）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static ValueTask InvalidateAsync(ISharedCache cache, SysPrintTemplate template, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        return CacheInvalidation.RemoveAsync(cache, KeyOf(template.OwnerTenantId, template.TemplateCode), cancellationToken);
    }

    private static CacheKey KeyOf(string side, string code) => new($"printing:templates:{side}:{code}");

    private static Error NoUsable(string code, PrintTemplateScope scope)
        => Error.NotFound(PrintingErrors.NoUsableTemplate, PrintingMessages.NoUsableTemplate, MessageArguments.Of(("code", code), ("scope", scope.ToString())));

    /// <summary>
    /// 某一侧某编码的启用模板
    /// </summary>
    /// <remarks>
    /// 缓存值是模板的 JSON，没有即空串
    /// </remarks>
    private async ValueTask<CachedPrintTemplate?> FindAsync(string side, string code, CancellationToken cancellationToken)
    {
        var raw = await cache.GetOrCreateAsync(KeyOf(side, code), ct => LoadAsync(side, code, ct), CacheEntryOptions.Expires(CacheTimeToLive), cancellationToken).ConfigureAwait(false);
        return raw.Length == 0 ? null : JsonSerializer.Deserialize(raw, PrintingJsonContext.Default.CachedPrintTemplate);
    }

    private async ValueTask<string> LoadAsync(string side, string code, CancellationToken cancellationToken)
    {
        var rows = await templates.ListAsync(
            EntityQuery.All<SysPrintTemplate>().Where(new Specification<SysPrintTemplate>(t => t.OwnerTenantId == side && t.TemplateCode == code && t.Status == PrintTemplateStatus.Active)).Page(0, 1),
            cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? string.Empty : JsonSerializer.Serialize(CachedPrintTemplate.From(rows[0]), PrintingJsonContext.Default.CachedPrintTemplate);
    }
}

/// <summary>
/// 缓存里的一张启用模板
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="TemplateCode">模板编码</param>
/// <param name="DataSourceCode">数据源编码</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="TemplateJson">模板 JSON</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放</param>
/// <param name="ConcurrencyStamp">并发戳（前端据此判断打开样例表单之后模板有没有变）</param>
public sealed record CachedPrintTemplate(Guid Id, string TemplateCode, string? DataSourceCode, string TemplateName, string TemplateJson, bool AllowTenantUse, string ConcurrencyStamp)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="template">模板</param>
    public static CachedPrintTemplate From(SysPrintTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new(template.Id, template.TemplateCode, template.DataSourceCode, template.TemplateName, template.TemplateJson, template.AllowTenantUse, template.ConcurrencyStamp);
    }
}
