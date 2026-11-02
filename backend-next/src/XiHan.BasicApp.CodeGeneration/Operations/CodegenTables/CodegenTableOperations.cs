// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTableColumns;
using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;

/// <summary>
/// 表配置列表。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-tables:list</c>。筛选与排序见 <see cref="CodegenTableQueries"/>。
/// </remarks>
/// <param name="queries">表配置查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesRead)]
public sealed class ListCodegenTables(CodegenTableQueries queries) : IOperation<PageRequest, PageResult<CodegenTableResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<CodegenTableResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 表配置下拉选项。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-tables:options</c>。全部表配置，按表名；列配置的关联表与表配置的主表从这里选（照 4.x）。
/// </remarks>
/// <param name="queries">表配置查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesRead)]
public sealed class ListCodegenTableOptions(CodegenTableQueries queries) : IOperation<ListCodegenTableOptionsRequest, CodegenTableOptionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.options";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableOptionsResponse>> HandleAsync(ListCodegenTableOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new CodegenTableOptionsResponse(await queries.OptionsAsync(context).ConfigureAwait(false));
    }
}

/// <summary>
/// 表配置详情。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-tables/{id}</c>。带列配置（照 4.x）。
/// </remarks>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesRead)]
public sealed class GetCodegenTable(CodeGenTableService service) : IOperation<CodegenTableIdRequest, CodegenTableDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.get";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableDetailResponse>> HandleAsync(CodegenTableIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var table))
        {
            return found.Error!;
        }

        var columns = await service.ColumnsOfAsync(table.Id, context.CancellationToken).ConfigureAwait(false);
        return new CodegenTableDetailResponse(CodegenTableResponse.From(table), [.. columns.Select(CodegenTableColumnResponse.From)]);
    }
}

/// <summary>
/// 列出库里可以导入的表。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-tables:db-tables</c>。数据源为空即主库；外部库要存在且启用（停用 <c>CODEGENERATION_CONFLICT_003</c>，照 4.x）。
/// 关键字匹配表名与注释，分表按逻辑名折叠；页码换算成跳过与取的条数交给框架，越界错误原样返回（修订（八十三））。
/// 照 4.x 只要读权限。
/// </remarks>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesRead)]
public sealed class ListCodegenDbTables(CodeGenTableService service) : IOperation<ListCodegenDbTablesRequest, PageResult<CodegenDbTableResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.db-tables";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<CodegenDbTableResponse>>> HandleAsync(ListCodegenDbTablesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await service.SourceOfAsync(request.DataSourceId, context.CancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var source))
        {
            return resolved.Error!;
        }

        var listed = await service.ListDatabaseTablesAsync(source, CodeGenDataSourceService.Trimmed(request.Keyword), request.Page.Skip, request.Page.Size, context.CancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var page))
        {
            return listed.Error!;
        }

        return new PageResult<CodegenDbTableResponse>([.. page.Items.Select(static t => new CodegenDbTableResponse(t.Name, t.Comment, t.IsSplit))], page.Total, request.Page);
    }
}

/// <summary>
/// 导入一批库表。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-tables:import</c>。照 4.x 按表隔离：读不到、已导入（<c>CODEGENERATION_CONFLICT_004</c>）或没有列（<c>CODEGENERATION_VALIDATION_002</c>）的表记一条失败，其余照常导入；
/// 类名、业务名、类型、控件与查询方式按库结构推断，「沿用已有实体」按登记的实体识别（只认主库）。整批读不了（连不上、来源停用）即失败。
/// </remarks>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesImport)]
public sealed class ImportCodegenTables(CodeGenTableService service) : IOperation<ImportCodegenTablesRequest, CodegenImportResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.import";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenImportResponse>> HandleAsync(ImportCodegenTablesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var resolved = await service.SourceOfAsync(request.DataSourceId, context.CancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var source))
        {
            return resolved.Error!;
        }

        var names = request.TableNames.Where(static n => !string.IsNullOrWhiteSpace(n)).Select(static n => n.Trim()).Distinct(StringComparer.Ordinal).ToList();
        var imported = await service.ImportAsync(source, names, context.GetSecurity().Principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(), context.CancellationToken).ConfigureAwait(false);
        if (!imported.TryGetValue(out var outcomes))
        {
            return imported.Error!;
        }

        var succeeded = new List<string>();
        var failed = new List<CodegenImportFailure>();
        foreach (var (tableName, outcome) in outcomes)
        {
            if (outcome.IsSuccess)
            {
                succeeded.Add(tableName);
            }
            else
            {
                failed.Add(new CodegenImportFailure(tableName, outcome.Error!.Code, context.Localize(outcome.Error!)));
            }
        }

        return new CodegenImportResponse(succeeded, failed);
    }
}

/// <summary>
/// 按最新库结构同步表配置。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-tables/{id}:sync-schema</c>。照 4.x：表注释与主键、列的结构字段以库为准；类名等与列的类型、控件、查询方式没被人工改过才跟随新的推断；
/// 新列补进来，库里已删的列删掉列配置。来源库连不上或表不在了即失败、配置不变。
/// </remarks>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesImport)]
public sealed class SyncCodegenTableSchema(CodeGenTableService service) : IOperation<CodegenTableIdRequest, CodegenSyncResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.sync-schema";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenSyncResponse>> HandleAsync(CodegenTableIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var table))
        {
            return found.Error!;
        }

        var synced = await service.SyncAsync(table, context.GetSecurity().Principal.ClaimValues(IdentityClaims.UserName).FirstOrDefault(), context.CancellationToken).ConfigureAwait(false);
        return synced.TryGetValue(out var result)
            ? new CodegenSyncResponse(result.Added.Count, result.Updated, result.Removed.Count, result.Added, result.Removed)
            : synced.Error!;
    }
}

/// <summary>
/// 修改表配置。
/// </summary>
/// <remarks>
/// <c>PUT /api/codegen-tables/{id}</c>。照 4.x 整体修改：表名唯一（<c>CODEGENERATION_CONFLICT_004</c>），生成模式不能是通用（<c>CODEGENERATION_VALIDATION_003</c>），
/// 勾了导入必须勾新建（<c>CODEGENERATION_VALIDATION_004</c>）；值变了的被跟踪字段记为人工改过。交回的并发戳与当前的不同即冲突。
/// </remarks>
/// <param name="tables">表配置仓储。</param>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesUpdate)]
public sealed class UpdateCodegenTable(IRepository<SysCodeGenTable, Guid> tables, CodeGenTableService service) : IOperation<UpdateCodegenTableRequest, CodegenTableResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.update";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableResponse>> HandleAsync(UpdateCodegenTableRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (request.TemplateType == CodeGenTemplateType.Universal)
        {
            return Error.Validation(CodeGenerationErrors.TemplateTypeUniversal, CodeGenerationMessages.TemplateTypeUniversal);
        }

        var actions = CodeGenActions.Normalize(request.EnabledActions);
        if (!CodeGenActions.IsValid(actions))
        {
            return Error.Validation(CodeGenerationErrors.ActionsConflict, CodeGenerationMessages.ActionsConflict);
        }

        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var table))
        {
            return found.Error!;
        }

        var tableName = request.TableName.Trim();
        if (await service.TableNameTakenAsync(tableName, table.Id, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(CodeGenerationErrors.TableAlreadyImported, CodeGenerationMessages.TableAlreadyImported, MessageArguments.Of(("table", tableName)));
        }

        var before = CodeGenModifiedFields.Snapshot(table, CodeGenTableService.TrackedTableFields);
        table.ConcurrencyStamp = request.ConcurrencyStamp;
        table.TableName = tableName;
        table.TableComment = CodeGenDataSourceService.Trimmed(request.TableComment);
        table.ClassName = request.ClassName.Trim();
        table.Namespace = CodeGenDataSourceService.Trimmed(request.Namespace);
        table.ModuleName = CodeGenDataSourceService.Trimmed(request.ModuleName);
        table.BusinessName = CodeGenDataSourceService.Trimmed(request.BusinessName);
        table.FunctionName = CodeGenDataSourceService.Trimmed(request.FunctionName);
        table.Author = CodeGenDataSourceService.Trimmed(request.Author);
        table.TemplateType = request.TemplateType;
        table.DeliveryType = request.DeliveryType;
        table.Scope = request.Scope;
        table.Target = request.Target;
        table.EnabledActions = CodeGenDataSourceService.Trimmed(request.EnabledActions);
        table.PrimaryKeyColumn = CodeGenDataSourceService.Trimmed(request.PrimaryKeyColumn);
        table.TreeParentColumn = CodeGenDataSourceService.Trimmed(request.TreeParentColumn);
        table.TreeNameColumn = CodeGenDataSourceService.Trimmed(request.TreeNameColumn);
        table.MasterTableId = request.MasterTableId;
        table.MasterForeignKey = CodeGenDataSourceService.Trimmed(request.MasterForeignKey);
        table.DatabaseType = request.DatabaseType;
        table.DataSourceId = request.DataSourceId;
        table.Options = CodeGenDataSourceService.Trimmed(request.Options);
        table.Status = request.Status;
        table.Remark = CodeGenDataSourceService.Trimmed(request.Remark);
        table.UserModifiedFields = CodeGenModifiedFields.Merge(table.UserModifiedFields, table, before);
        var updated = await tables.UpdateAsync(table, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? CodegenTableResponse.From(table) : updated.Error!;
    }
}

/// <summary>
/// 启用或停用表配置。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-tables/{id}:set-status</c>。交回的并发戳与当前的不同即冲突；给了备注即替换备注（4.x 同）。停用的表配置不能用于零代码运行时。
/// </remarks>
/// <param name="tables">表配置仓储。</param>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesUpdate)]
public sealed class SetCodegenTableStatus(IRepository<SysCodeGenTable, Guid> tables, CodeGenTableService service) : IOperation<SetCodegenTableStatusRequest, CodegenTableResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableResponse>> HandleAsync(SetCodegenTableStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var table))
        {
            return found.Error!;
        }

        table.ConcurrencyStamp = request.ConcurrencyStamp;
        table.Status = request.Status;
        table.Remark = CodeGenDataSourceService.Trimmed(request.Remark) ?? table.Remark;
        var updated = await tables.UpdateAsync(table, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? CodegenTableResponse.From(table) : updated.Error!;
    }
}

/// <summary>
/// 批量删除表配置。
/// </summary>
/// <remarks>
/// <c>DELETE /api/codegen-tables?ids=...</c>。连带软删它的列配置（照 4.x）；逐项：不存在 <c>CODEGENERATION_NOTFOUND_002</c>。
/// </remarks>
/// <param name="tables">表配置仓储。</param>
/// <param name="service">表配置规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesDelete)]
public sealed class DeleteCodegenTables(IRepository<SysCodeGenTable, Guid> tables, CodeGenTableService service) : IOperation<CodegenTableIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(CodegenTableIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await tables.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static t => t.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var table))
            {
                failed.Add(new BatchFailure(id, CodeGenerationErrors.TableNotFound));
                continue;
            }

            var removed = await service.RemoveAsync(table, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 预览一张表生成的代码。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-tables/{id}:preview</c>。只渲染、不打包、不写历史（照 4.x），要读权限；配置检查不过 <c>CODEGENERATION_VALIDATION_007</c>，模板渲染失败 <c>CODEGENERATION_VALIDATION_008</c>（带模板编码），没有可用模板 <c>CODEGENERATION_VALIDATION_009</c>。
/// </remarks>
/// <param name="engine">生成管线。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = CodeGenerationPermissions.TablesRead)]
public sealed class PreviewCodegenTable(CodeGenEngine engine) : IOperation<PreviewCodegenTableRequest, CodegenPreviewResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-tables.preview";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenPreviewResponse>> HandleAsync(PreviewCodegenTableRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var rendered = await engine.RenderAsync(request.Id, request.TemplateCodes, context.CancellationToken).ConfigureAwait(false);
        return rendered.TryGetValue(out var artifacts) ? new CodegenPreviewResponse([.. artifacts.Select(CodegenArtifactResponse.From)]) : rendered.Error!;
    }
}
