// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Services;
using XiHan.BasicApp.Printing.Queries;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Printing.Operations.PrintTemplates;

/// <summary>
/// 打印模板列表
/// </summary>
/// <remarks>
/// <c>POST /api/print-templates:list</c>。作用域用筛选 <c>scope</c>（<c>eq</c>，<c>Tenant</c> / <c>Global</c>）：平台一侧只有全局模板（全部状态，用 <c>Tenant</c> 即 <c>PRINTING_VALIDATION_004</c>）；
/// 租户一侧缺省看本租户的模板，<c>Global</c> 看启用且开放的全局模板。筛选与排序见 <see cref="PrintTemplateQueries"/>
/// </remarks>
/// <param name="queries">模板查询</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesRead)]
public sealed class ListPrintTemplates(PrintTemplateQueries queries) : IOperation<PageRequest, PageResult<PrintTemplateResponse>>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<PrintTemplateResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 打印模板详情
/// </summary>
/// <remarks>
/// <c>GET /api/print-templates/{id}</c>。本侧的模板都能看；租户看全局模板须已启用且向租户开放，否则 <c>PRINTING_DENIED_002</c>；别的租户的当作不存在
/// </remarks>
/// <param name="templates">模板仓储</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesRead)]
public sealed class GetPrintTemplate(IRepository<SysPrintTemplate, Guid> templates) : IOperation<GetPrintTemplateRequest, PrintTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.get";

    /// <inheritdoc />
    public async ValueTask<Result<PrintTemplateDetailResponse>> HandleAsync(GetPrintTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = PrintTemplateService.SideOf(context);
        if (!(await templates.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var template))
        {
            return PrintTemplateService.NotFound(request.Id);
        }

        if (string.Equals(template.OwnerTenantId, side, StringComparison.Ordinal))
        {
            return PrintTemplateDetailResponse.From(template);
        }

        if (!PrintTemplateService.IsPlatform(template.OwnerTenantId))
        {
            return PrintTemplateService.NotFound(request.Id);
        }

        return template is { Status: PrintTemplateStatus.Active, AllowTenantUse: true }
            ? PrintTemplateDetailResponse.From(template)
            : Error.Denied(PrintingErrors.GlobalNotOpen, PrintingMessages.GlobalNotOpen, MessageArguments.Of(("code", template.TemplateCode)));
    }
}

/// <summary>
/// 按编码与作用域解析可打印的模板
/// </summary>
/// <remarks>
/// <c>GET /api/print-templates:resolve?templateCode=&amp;scope=</c>。业务打印与预览经它取模板（前端打印包的 <c>resolveTemplate</c>），解析规则与缓存见 <see cref="PrintTemplateResolver"/>
/// 平台用 <c>Tenant</c> 即 <c>PRINTING_VALIDATION_004</c>，解析不到 <c>PRINTING_NOTFOUND_002</c>
/// </remarks>
/// <param name="resolver">解析器</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesUse)]
public sealed class ResolvePrintTemplate(PrintTemplateResolver resolver) : IOperation<ResolvePrintTemplateRequest, ResolvedPrintTemplateResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.resolve";

    /// <inheritdoc />
    public async ValueTask<Result<ResolvedPrintTemplateResponse>> HandleAsync(ResolvePrintTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await resolver.ResolveAsync(request.TemplateCode.Trim(), request.Scope, PrintTemplateService.SideOf(context), context.CancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var hit))
        {
            return resolved.Error!;
        }

        var (template, scope) = hit;
        return new ResolvedPrintTemplateResponse(template.Id, template.TemplateCode, template.DataSourceCode, template.TemplateName, template.TemplateJson, request.Scope, scope, template.ConcurrencyStamp);
    }
}

/// <summary>
/// 新建打印模板
/// </summary>
/// <remarks>
/// <c>POST /api/print-templates</c>。写在调用方这一侧，平台一侧还要全局管理权限（<c>PRINTING_DENIED_001</c>）；编码本侧已有 <c>PRINTING_CONFLICT_001</c>（并发重复由部分唯一索引兜住）；
/// 数据源没登记 <c>PRINTING_VALIDATION_003</c>，模板 JSON 不合格式 <c>PRINTING_VALIDATION_002</c>；租户模板的「向租户开放」恒为否
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
/// <param name="cache">共享缓存（失效「没有」的那一项）</param>
/// <param name="ids">标识生成器</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesCreate)]
public sealed class CreatePrintTemplate(IRepository<SysPrintTemplate, Guid> templates, PrintTemplateService service, ISharedCache cache, IIdGenerator<Guid> ids) : IOperation<CreatePrintTemplateRequest, PrintTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.create";

    /// <inheritdoc />
    public async ValueTask<Result<PrintTemplateDetailResponse>> HandleAsync(CreatePrintTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var writable = await service.EnsureWritableAsync(context).ConfigureAwait(false);
        if (writable.IsFailure)
        {
            return writable.Error!;
        }

        var side = PrintTemplateService.SideOf(context);
        var code = request.TemplateCode.Trim();
        var dataSource = service.DataSourceOf(request.DataSourceCode);
        if (!dataSource.TryGetValue(out var dataSourceCode))
        {
            return dataSource.Error!;
        }

        var json = PrintTemplateService.ValidateTemplateJson(request.TemplateJson);
        if (json.IsFailure)
        {
            return json.Error!;
        }

        // 预检给出友好的冲突，两个并发的新建之间以部分唯一索引为准
        if (await service.CodeTakenAsync(side, code, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(PrintingErrors.TemplateCodeTaken, PrintingMessages.TemplateCodeTaken, MessageArguments.Of(("code", code)));
        }

        var template = new SysPrintTemplate
        {
            Id = ids.NewId(),
            OwnerTenantId = side,
            TemplateCode = code,
            DataSourceCode = dataSourceCode.TryGetValue(out var source) ? source : null,
            TemplateName = request.TemplateName.Trim(),
            TemplateJson = request.TemplateJson,
            // 租户模板不带「向租户开放」，免得误判可见范围
            AllowTenantUse = PrintTemplateService.IsPlatform(side) && request.AllowTenantUse,
            Status = request.Status,
            Order = request.Order,
            Remark = PrintTemplateService.Trimmed(request.Remark),
        };
        var added = await templates.AddAsync(template, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await PrintTemplateResolver.InvalidateAsync(cache, template, context.CancellationToken).ConfigureAwait(false);
        return PrintTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 修改打印模板
/// </summary>
/// <remarks>
/// <c>PUT /api/print-templates/{id}</c>。只改本侧的模板（别的一侧的当作不存在），平台一侧还要全局管理权限；编码不可改
/// 交回的并发戳与当前的不同即冲突（框架仓储判定）；写入后失效解析缓存
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
/// <param name="cache">共享缓存</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesUpdate)]
public sealed class UpdatePrintTemplate(IRepository<SysPrintTemplate, Guid> templates, PrintTemplateService service, ISharedCache cache) : IOperation<UpdatePrintTemplateRequest, PrintTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.update";

    /// <inheritdoc />
    public async ValueTask<Result<PrintTemplateDetailResponse>> HandleAsync(UpdatePrintTemplateRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var writable = await service.EnsureWritableAsync(context).ConfigureAwait(false);
        if (writable.IsFailure)
        {
            return writable.Error!;
        }

        var found = await service.FindOwnedAsync(request.Id, PrintTemplateService.SideOf(context), context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var template))
        {
            return found.Error!;
        }

        var dataSource = service.DataSourceOf(request.DataSourceCode);
        if (!dataSource.TryGetValue(out var dataSourceCode))
        {
            return dataSource.Error!;
        }

        var json = PrintTemplateService.ValidateTemplateJson(request.TemplateJson);
        if (json.IsFailure)
        {
            return json.Error!;
        }

        // 带上读到的并发戳：与当前行不同即由仓储报冲突
        template.ConcurrencyStamp = request.ConcurrencyStamp;
        template.DataSourceCode = dataSourceCode.TryGetValue(out var source) ? source : null;
        template.TemplateName = request.TemplateName.Trim();
        template.TemplateJson = request.TemplateJson;
        template.AllowTenantUse = PrintTemplateService.IsPlatform(template.OwnerTenantId) && request.AllowTenantUse;
        template.Order = request.Order;
        template.Remark = PrintTemplateService.Trimmed(request.Remark);
        var updated = await templates.UpdateAsync(template, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await PrintTemplateResolver.InvalidateAsync(cache, template, context.CancellationToken).ConfigureAwait(false);
        return PrintTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 启用或停用打印模板
/// </summary>
/// <remarks>
/// <c>POST /api/print-templates/{id}:set-status</c>。只改本侧的模板，平台一侧还要全局管理权限；交回的并发戳与当前的不同即冲突；
/// 给了备注即替换备注。写入后失效解析缓存
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
/// <param name="cache">共享缓存</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesStatus)]
public sealed class SetPrintTemplateStatus(IRepository<SysPrintTemplate, Guid> templates, PrintTemplateService service, ISharedCache cache) : IOperation<SetPrintTemplateStatusRequest, PrintTemplateDetailResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<PrintTemplateDetailResponse>> HandleAsync(SetPrintTemplateStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var writable = await service.EnsureWritableAsync(context).ConfigureAwait(false);
        if (writable.IsFailure)
        {
            return writable.Error!;
        }

        var found = await service.FindOwnedAsync(request.Id, PrintTemplateService.SideOf(context), context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var template))
        {
            return found.Error!;
        }

        template.ConcurrencyStamp = request.ConcurrencyStamp;
        template.Status = request.Status;
        template.Remark = PrintTemplateService.Trimmed(request.Remark) ?? template.Remark;
        var updated = await templates.UpdateAsync(template, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await PrintTemplateResolver.InvalidateAsync(cache, template, context.CancellationToken).ConfigureAwait(false);
        return PrintTemplateDetailResponse.From(template);
    }
}

/// <summary>
/// 批量删除打印模板
/// </summary>
/// <remarks>
/// <c>DELETE /api/print-templates?ids=...</c>。平台一侧要全局管理权限（整批拒绝）；逐项：不是本侧的或不存在 <c>PRINTING_NOTFOUND_001</c>，
/// 仍在启用 <c>PRINTING_CONFLICT_002</c>（须先停用）。软删除，同编码可以重建；删除后失效解析缓存
/// </remarks>
/// <param name="templates">模板仓储</param>
/// <param name="service">模板规则</param>
/// <param name="cache">共享缓存</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = PrintingPermissions.PrintTemplatesDelete)]
public sealed class DeletePrintTemplates(IRepository<SysPrintTemplate, Guid> templates, PrintTemplateService service, ISharedCache cache) : IOperation<PrintTemplateIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "print-templates.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(PrintTemplateIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var writable = await service.EnsureWritableAsync(context).ConfigureAwait(false);
        if (writable.IsFailure)
        {
            return writable.Error!;
        }

        var side = PrintTemplateService.SideOf(context);
        var found = (await templates.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var template) || !string.Equals(template.OwnerTenantId, side, StringComparison.Ordinal))
            {
                failed.Add(new BatchFailure(id, PrintingErrors.TemplateNotFound, PrintingMessages.TemplateNotFound.L(("id", id))));
                continue;
            }

            if (template.Status != PrintTemplateStatus.Disabled)
            {
                failed.Add(new BatchFailure(id, PrintingErrors.TemplateEnabled, PrintingMessages.TemplateEnabled.L()));
                continue;
            }

            var removed = await templates.RemoveAsync(template, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            await PrintTemplateResolver.InvalidateAsync(cache, template, context.CancellationToken).ConfigureAwait(false);
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
