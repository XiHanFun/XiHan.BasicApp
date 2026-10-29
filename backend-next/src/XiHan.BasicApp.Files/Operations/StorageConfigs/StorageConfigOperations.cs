// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Queries;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Operations.StorageConfigs;

/// <summary>
/// 分页列出当前一侧的存储配置。
/// </summary>
/// <remarks>
/// <c>POST /api/storage-configs:list</c>。缺省配置在前，访问密钥不回显。
/// </remarks>
/// <param name="queries">配置查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsRead)]
public sealed class ListStorageConfigs(StorageConfigQueries queries) : IOperation<PageRequest, PageResult<StorageConfigResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<StorageConfigResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的存储配置。
/// </summary>
/// <remarks>
/// <c>GET /api/storage-configs/{id}</c>。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsRead)]
public sealed class GetStorageConfig(StorageConfigService service) : IOperation<StorageConfigIdRequest, StorageConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.get";

    /// <inheritdoc />
    public async ValueTask<Result<StorageConfigResponse>> HandleAsync(StorageConfigIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, SideRows.SideOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return NotFound(request.Id);
        }

        return StorageConfigResponse.From(config);
    }

    internal static Error NotFound(Guid id) => Error.NotFound(FilesErrors.StorageConfigNotFound, FilesMessages.StorageConfigNotFound, MessageArguments.Of(("id", id)));
}

/// <summary>
/// 在当前一侧新建存储配置。
/// </summary>
/// <remarks>
/// <c>POST /api/storage-configs</c>。设为缺省时同侧原来的缺省配置不再是缺省。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsCreate)]
public sealed class CreateStorageConfig(StorageConfigService service) : IOperation<CreateStorageConfigRequest, StorageConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.create";

    /// <inheritdoc />
    public async ValueTask<Result<StorageConfigResponse>> HandleAsync(CreateStorageConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var created = await service.CreateAsync(SideRows.SideOf(context), request.ToDefinition(), request.IsDefault, request.Status, context.CancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var config))
        {
            return created.Error!;
        }

        return StorageConfigResponse.From(config);
    }
}

/// <summary>
/// 修改当前一侧的存储配置。
/// </summary>
/// <remarks>
/// <c>PUT /api/storage-configs/{id}</c>。编码不改，访问密钥不填即保留原值；已有副本照旧按这条配置读写。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsUpdate)]
public sealed class UpdateStorageConfig(StorageConfigService service) : IOperation<UpdateStorageConfigRequest, StorageConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.update";

    /// <inheritdoc />
    public async ValueTask<Result<StorageConfigResponse>> HandleAsync(UpdateStorageConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, SideRows.SideOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return GetStorageConfig.NotFound(request.Id);
        }

        var updated = await service.UpdateAsync(config, request.ToDefinition(config.Code), context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return StorageConfigResponse.From(config);
    }
}

/// <summary>
/// 启用或停用存储配置。
/// </summary>
/// <remarks>
/// <c>POST /api/storage-configs/{id}:set-status</c>。缺省配置不能停用；停用后不再接收上传，已有副本照常读取。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsUpdate)]
public sealed class SetStorageConfigStatus(StorageConfigService service) : IOperation<SetStorageConfigStatusRequest, StorageConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<StorageConfigResponse>> HandleAsync(SetStorageConfigStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, SideRows.SideOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return GetStorageConfig.NotFound(request.Id);
        }

        var updated = await service.SetStatusAsync(config, request.Status, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return StorageConfigResponse.From(config);
    }
}

/// <summary>
/// 设为当前一侧的缺省存储配置。
/// </summary>
/// <remarks>
/// <c>POST /api/storage-configs/{id}:set-default</c>。之后的上传都落在它上面。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsUpdate)]
public sealed class SetDefaultStorageConfig(StorageConfigService service) : IOperation<StorageConfigIdRequest, StorageConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.set-default";

    /// <inheritdoc />
    public async ValueTask<Result<StorageConfigResponse>> HandleAsync(StorageConfigIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, SideRows.SideOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return GetStorageConfig.NotFound(request.Id);
        }

        var updated = await service.SetDefaultAsync(config, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        return StorageConfigResponse.From(config);
    }
}

/// <summary>
/// 删除存储配置。
/// </summary>
/// <remarks>
/// <c>DELETE /api/storage-configs?ids=</c>。缺省配置与还有副本的配置逐项拒绝。
/// </remarks>
/// <param name="service">配置服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.StorageConfigsDelete)]
public sealed class DeleteStorageConfigs(StorageConfigService service) : IOperation<StorageConfigIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "storage-configs.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(StorageConfigIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = SideRows.SideOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            if (!(await service.FindAsync(id, side, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
            {
                failed.Add(new BatchFailure(id, FilesErrors.StorageConfigNotFound));
                continue;
            }

            var removed = await service.DeleteAsync(config, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                failed.Add(new BatchFailure(id, removed.Error!.Code));
                continue;
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
