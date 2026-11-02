// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.BasicApp.CodeGeneration.Queries;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;

/// <summary>
/// 数据源列表。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-datasources:list</c>。筛选与排序见 <see cref="CodegenDatasourceQueries"/>。
/// </remarks>
/// <param name="queries">数据源查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesRead)]
public sealed class ListCodegenDatasources(CodegenDatasourceQueries queries) : IOperation<PageRequest, PageResult<CodegenDatasourceResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<CodegenDatasourceResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 数据源下拉选项。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-datasources:options</c>。全部数据源（含停用的，照 4.x），按排序号与名称。
/// </remarks>
/// <param name="queries">数据源查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesRead)]
public sealed class ListCodegenDatasourceOptions(CodegenDatasourceQueries queries) : IOperation<ListCodegenDatasourceOptionsRequest, CodegenDatasourceOptionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.options";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceOptionsResponse>> HandleAsync(ListCodegenDatasourceOptionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return new CodegenDatasourceOptionsResponse(await queries.OptionsAsync(context).ConfigureAwait(false));
    }
}

/// <summary>
/// 数据源详情。
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-datasources/{id}</c>。口令与连接串不回显。
/// </remarks>
/// <param name="service">数据源规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesRead)]
public sealed class GetCodegenDatasource(CodeGenDataSourceService service) : IOperation<CodegenDatasourceIdRequest, CodegenDatasourceDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.get";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceDetailResponse>> HandleAsync(CodegenDatasourceIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var source) ? CodegenDatasourceDetailResponse.From(source) : found.Error!;
    }
}

/// <summary>
/// 新建数据源。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-datasources</c>。名称已被占用 <c>CODEGENERATION_CONFLICT_001</c>（并发重复由部分唯一索引兜住）；口令与显式连接串加密存储；
/// 设为缺省时其余数据源取消缺省（照 4.x）。
/// </remarks>
/// <param name="dataSources">数据源仓储。</param>
/// <param name="service">数据源规则。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesCreate)]
public sealed class CreateCodegenDatasource(IRepository<SysCodeGenDataSource, Guid> dataSources, CodeGenDataSourceService service, IIdGenerator<Guid> ids) : IOperation<CreateCodegenDatasourceRequest, CodegenDatasourceDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.create";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceDetailResponse>> HandleAsync(CreateCodegenDatasourceRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var name = request.SourceName.Trim();
        if (await service.NameTakenAsync(name, null, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(CodeGenerationErrors.DataSourceNameTaken, CodeGenerationMessages.DataSourceNameTaken, MessageArguments.Of(("name", name)));
        }

        var source = new SysCodeGenDataSource
        {
            Id = ids.NewId(),
            SourceName = name,
            SourceDescription = CodeGenDataSourceService.Trimmed(request.SourceDescription),
            DatabaseType = request.DatabaseType,
            Host = request.Host.Trim(),
            Port = request.Port,
            DatabaseName = request.DatabaseName.Trim(),
            UserName = request.UserName.Trim(),
            Password = service.Protect(CodeGenDataSourceService.PasswordPurpose, request.Password),
            ConnectionString = service.Protect(CodeGenDataSourceService.ConnectionStringPurpose, request.ConnectionString),
            ExtraParams = CodeGenDataSourceService.Trimmed(request.ExtraParams),
            ConnectionTimeout = request.ConnectionTimeout <= 0 ? 30 : request.ConnectionTimeout,
            IsDefault = request.IsDefault,
            Status = request.Status,
            Order = request.Order,
            Remark = CodeGenDataSourceService.Trimmed(request.Remark),
        };
        if (source.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(source.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        var added = await dataSources.AddAsync(source, context.CancellationToken).ConfigureAwait(false);
        return added.IsSuccess ? CodegenDatasourceDetailResponse.From(source) : added.Error!;
    }
}

/// <summary>
/// 修改数据源。
/// </summary>
/// <remarks>
/// <c>PUT /api/codegen-datasources/{id}</c>。口令与显式连接串留空即不改（照 4.x）；交回的并发戳与当前的不同即冲突。
/// 写入后注销这个数据源的连接，下次使用按新定义重建。
/// </remarks>
/// <param name="dataSources">数据源仓储。</param>
/// <param name="service">数据源规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesUpdate)]
public sealed class UpdateCodegenDatasource(IRepository<SysCodeGenDataSource, Guid> dataSources, CodeGenDataSourceService service) : IOperation<UpdateCodegenDatasourceRequest, CodegenDatasourceDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.update";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceDetailResponse>> HandleAsync(UpdateCodegenDatasourceRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var source))
        {
            return found.Error!;
        }

        var name = request.SourceName.Trim();
        if (await service.NameTakenAsync(name, source.Id, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(CodeGenerationErrors.DataSourceNameTaken, CodeGenerationMessages.DataSourceNameTaken, MessageArguments.Of(("name", name)));
        }

        // 带上读到的并发戳：与当前行不同即由仓储报冲突
        source.ConcurrencyStamp = request.ConcurrencyStamp;
        source.SourceName = name;
        source.SourceDescription = CodeGenDataSourceService.Trimmed(request.SourceDescription);
        source.DatabaseType = request.DatabaseType;
        source.Host = request.Host.Trim();
        source.Port = request.Port;
        source.DatabaseName = request.DatabaseName.Trim();
        source.UserName = request.UserName.Trim();
        source.Password = service.Protect(CodeGenDataSourceService.PasswordPurpose, request.Password) ?? source.Password;
        source.ConnectionString = service.Protect(CodeGenDataSourceService.ConnectionStringPurpose, request.ConnectionString) ?? source.ConnectionString;
        source.ExtraParams = CodeGenDataSourceService.Trimmed(request.ExtraParams);
        source.ConnectionTimeout = request.ConnectionTimeout <= 0 ? 30 : request.ConnectionTimeout;
        source.Order = request.Order;
        source.Remark = CodeGenDataSourceService.Trimmed(request.Remark);
        if (request.IsDefault && !source.IsDefault)
        {
            var cleared = await service.ClearOtherDefaultsAsync(source.Id, context.CancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        source.IsDefault = request.IsDefault;
        var updated = await dataSources.UpdateAsync(source, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await service.ForgetAsync([source.Id], context.CancellationToken).ConfigureAwait(false);
        return CodegenDatasourceDetailResponse.From(source);
    }
}

/// <summary>
/// 启用或停用数据源。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-datasources/{id}:set-status</c>。交回的并发戳与当前的不同即冲突；给了备注即替换备注（4.x 同）。写入后注销这个数据源的连接。
/// </remarks>
/// <param name="dataSources">数据源仓储。</param>
/// <param name="service">数据源规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesUpdate)]
public sealed class SetCodegenDatasourceStatus(IRepository<SysCodeGenDataSource, Guid> dataSources, CodeGenDataSourceService service) : IOperation<SetCodegenDatasourceStatusRequest, CodegenDatasourceDetailResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceDetailResponse>> HandleAsync(SetCodegenDatasourceStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var source))
        {
            return found.Error!;
        }

        source.ConcurrencyStamp = request.ConcurrencyStamp;
        source.Status = request.Status;
        source.Remark = CodeGenDataSourceService.Trimmed(request.Remark) ?? source.Remark;
        var updated = await dataSources.UpdateAsync(source, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await service.ForgetAsync([source.Id], context.CancellationToken).ConfigureAwait(false);
        return CodegenDatasourceDetailResponse.From(source);
    }
}

/// <summary>
/// 批量删除数据源。
/// </summary>
/// <remarks>
/// <c>DELETE /api/codegen-datasources?ids=...</c>。逐项：不存在 <c>CODEGENERATION_NOTFOUND_001</c>，仍被表配置引用 <c>CODEGENERATION_CONFLICT_002</c>（4.x 不查引用，方案 C2）。
/// 软删除，同名可以重建；删除后注销连接。
/// </remarks>
/// <param name="dataSources">数据源仓储。</param>
/// <param name="service">数据源规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesDelete)]
public sealed class DeleteCodegenDatasources(IRepository<SysCodeGenDataSource, Guid> dataSources, CodeGenDataSourceService service) : IOperation<CodegenDatasourceIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(CodegenDatasourceIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = (await dataSources.FindManyAsync(request.Ids, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static s => s.Id);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!found.TryGetValue(id, out var source))
            {
                failed.Add(new BatchFailure(id, CodeGenerationErrors.DataSourceNotFound));
                continue;
            }

            if (await service.InUseAsync(id, context.CancellationToken).ConfigureAwait(false))
            {
                failed.Add(new BatchFailure(id, CodeGenerationErrors.DataSourceInUse));
                continue;
            }

            var removed = await dataSources.RemoveAsync(source, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            succeeded.Add(id);
        }

        await service.ForgetAsync(succeeded, context.CancellationToken).ConfigureAwait(false);
        return new BatchResult(succeeded, failed);
    }
}

/// <summary>
/// 测试数据源的连接。
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-datasources/{id}:test-connection</c>。按当前定义临时连一次（停用的也能测，照 4.x），把结果写回最近一次测试的时间、结果与说明；
/// 连不上不是失败的调用，结果里 <c>success</c> 为否、说明是框架给的原因（脱敏）。要修改权限：4.x 只要读权限却写库（方案 C2）。
/// 口令或连接串解不开即 <c>CODEGENERATION_UNAVAILABLE_001</c>。
/// </remarks>
/// <param name="dataSources">数据源仓储。</param>
/// <param name="service">数据源规则。</param>
/// <param name="clock">时钟。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.DatasourcesUpdate)]
public sealed class TestCodegenDatasource(IRepository<SysCodeGenDataSource, Guid> dataSources, CodeGenDataSourceService service, TimeProvider clock) : IOperation<CodegenDatasourceIdRequest, CodegenDatasourceTestResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "codegen-datasources.test-connection";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenDatasourceTestResponse>> HandleAsync(CodegenDatasourceIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var source))
        {
            return found.Error!;
        }

        var definition = service.DefinitionOf(source);
        if (!definition.TryGetValue(out var defined))
        {
            return definition.Error!;
        }

        var started = clock.GetTimestamp();
        var tested = await service.TestAsync(defined, context.CancellationToken).ConfigureAwait(false);
        var elapsed = (long)clock.GetElapsedTime(started).TotalMilliseconds;
        var message = tested.IsSuccess ? context.Localize(CodeGenerationMessages.ConnectionSucceeded) : context.Localize(tested.Error!);
        source.LastTestAt = clock.GetUtcNow();
        source.LastTestResult = tested.IsSuccess;
        source.LastTestMessage = message.Length > 500 ? message[..500] : message;
        var updated = await dataSources.UpdateAsync(source, context.CancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? new CodegenDatasourceTestResponse(tested.IsSuccess, message, elapsed) : updated.Error!;
    }
}
