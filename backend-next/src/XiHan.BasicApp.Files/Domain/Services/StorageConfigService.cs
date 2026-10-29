// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.ObjectStorage.Abstractions.Keys;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Domain.Services;

/// <summary>
/// 存储配置的写入内容（编码只在新建时生效）。
/// </summary>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">存储类型。</param>
/// <param name="Endpoint">服务地址。</param>
/// <param name="Region">区域。</param>
/// <param name="Bucket">桶名。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="SecretAccessKey">访问密钥明文；修改时为空即保留原值。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record StorageConfigDefinition(string Code, string Name, StorageType Type, string? Endpoint, string? Region, string Bucket, string? AccessKeyId, string? SecretAccessKey, int Order, string? Remark);

/// <summary>
/// 存储配置（4.x 同）：编码同一侧唯一；S3 兼容存储要服务地址与访问密钥；每侧至多一条缺省配置，缺省的须启用、不能停用或删除；还有副本的配置不能删除。
/// 上传时取当前一侧的缺省配置，租户没有就取平台的缺省配置。
/// </summary>
/// <param name="configs">存储配置仓储。</param>
/// <param name="unitOfWork">工作单元（查副本引用）。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="ids">ID 生成器。</param>
public sealed class StorageConfigService(IRepository<SysStorageConfig, Guid> configs, SqlSugarUnitOfWork unitOfWork, ISecretProtector secrets, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 按 ID 取这一侧的配置。
    /// </summary>
    /// <param name="id">配置 ID。</param>
    /// <param name="owner">所属侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的配置。</returns>
    public async Task<Maybe<SysStorageConfig>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await configs.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var config) && string.Equals(config.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(config) : Maybe.None<SysStorageConfig>();
    }

    /// <summary>
    /// 按 ID 取配置，不看所属侧（副本记下的配置可能是平台的）。
    /// </summary>
    /// <param name="id">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的配置。</returns>
    public async Task<Maybe<SysStorageConfig>> FindAnyAsync(Guid id, CancellationToken cancellationToken) => await configs.FindAsync(id, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// 按编码取这一侧的配置。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="code">编码。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到的配置。</returns>
    public async Task<Maybe<SysStorageConfig>> FindByCodeAsync(string owner, string code, CancellationToken cancellationToken)
    {
        var rows = await configs.ListAsync(EntityQuery.All<SysStorageConfig>().Where(new Specification<SysStorageConfig>(c => c.OwnerTenantId == owner && c.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysStorageConfig>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 一批配置（列副本时补配置的编码与名称）。
    /// </summary>
    /// <param name="ids">配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按 ID 索引的配置。</returns>
    public async Task<IReadOnlyDictionary<Guid, SysStorageConfig>> ManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, SysStorageConfig>();
        }

        var list = ids.ToList();
        var rows = await configs.ListAsync(EntityQuery.All<SysStorageConfig>().Where(new Specification<SysStorageConfig>(c => list.Contains(c.Id))), cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static c => c.Id);
    }

    /// <summary>
    /// 上传落到哪条配置：这一侧启用的缺省配置，租户没有就取平台的。
    /// </summary>
    /// <param name="side">上传方所在的侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>配置；都没有时失败。</returns>
    public async Task<Result<SysStorageConfig>> UploadTargetAsync(string side, CancellationToken cancellationToken)
    {
        if ((await DefaultOfAsync(side, cancellationToken).ConfigureAwait(false)).TryGetValue(out var own))
        {
            return own;
        }

        if (!string.Equals(side, TenantIds.Platform, StringComparison.Ordinal) && (await DefaultOfAsync(TenantIds.Platform, cancellationToken).ConfigureAwait(false)).TryGetValue(out var platform))
        {
            return platform;
        }

        return Error.Unavailable(FilesErrors.NoStorageConfig, FilesMessages.NoStorageConfig);
    }

    /// <summary>
    /// 新建。
    /// </summary>
    /// <param name="owner">所属侧。</param>
    /// <param name="definition">写入内容。</param>
    /// <param name="isDefault">是否设为缺省。</param>
    /// <param name="status">状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新建的配置。</returns>
    public async Task<Result<SysStorageConfig>> CreateAsync(string owner, StorageConfigDefinition definition, bool isDefault, StorageConfigStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var code = definition.Code.Trim();
        if (code.Length == 0 || code.Any(char.IsWhiteSpace))
        {
            return Error.Validation(FilesErrors.StorageCodeInvalid, FilesMessages.StorageCodeInvalid, MessageArguments.Of(("code", definition.Code)));
        }

        if (isDefault && status != StorageConfigStatus.Active)
        {
            return Error.Conflict(FilesErrors.DefaultNeedsActive, FilesMessages.DefaultNeedsActive);
        }

        if ((await FindByCodeAsync(owner, code, cancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Conflict(FilesErrors.StorageCodeTaken, FilesMessages.StorageCodeTaken, MessageArguments.Of(("code", code)));
        }

        var config = new SysStorageConfig { Id = ids.NewId(), OwnerTenantId = owner, Code = code, Status = status, IsDefault = isDefault };
        var applied = Apply(config, definition);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var added = await configs.AddAsync(config, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        if (isDefault)
        {
            var cleared = await ClearOtherDefaultsAsync(config, cancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return cleared.Error!;
            }
        }

        return config;
    }

    /// <summary>
    /// 修改（编码不变；访问密钥为空即保留原值）。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="definition">写入内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> UpdateAsync(SysStorageConfig config, StorageConfigDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(definition);
        var applied = Apply(config, definition);
        return applied.IsFailure ? applied : await configs.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 启用或停用；缺省配置不能停用。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="status">状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetStatusAsync(SysStorageConfig config, StorageConfigStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.IsDefault && status != StorageConfigStatus.Active)
        {
            return Error.Conflict(FilesErrors.DefaultStorageLocked, FilesMessages.DefaultStorageLocked);
        }

        config.Status = status;
        return await configs.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 设为这一侧的缺省配置（须启用），同侧其余配置不再是缺省。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetDefaultAsync(SysStorageConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.Status != StorageConfigStatus.Active)
        {
            return Error.Conflict(FilesErrors.DefaultNeedsActive, FilesMessages.DefaultNeedsActive);
        }

        if (!config.IsDefault)
        {
            config.IsDefault = true;
            var updated = await configs.UpdateAsync(config, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return await ClearOtherDefaultsAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 删除；缺省配置与还有副本的配置不能删除。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> DeleteAsync(SysStorageConfig config, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.IsDefault)
        {
            return Error.Conflict(FilesErrors.DefaultStorageLocked, FilesMessages.DefaultStorageLocked);
        }

        // 平台的配置也会被租户的副本用到（租户没有自己的缺省配置时落在平台的上），所以不按侧过滤
        var copies = await unitOfWork.ConnectionFor(typeof(SysFileStorage)).Queryable<SysFileStorage>().Where(s => s.StorageConfigId == config.Id && !s.IsDeleted).CountAsync(cancellationToken).ConfigureAwait(false);
        if (copies > 0)
        {
            return Error.Conflict(FilesErrors.StorageConfigInUse, FilesMessages.StorageConfigInUse, MessageArguments.Of(("count", copies)));
        }

        return await configs.RemoveAsync(config, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Maybe<SysStorageConfig>> DefaultOfAsync(string owner, CancellationToken cancellationToken)
    {
        var rows = await configs.ListAsync(EntityQuery.All<SysStorageConfig>().Where(new Specification<SysStorageConfig>(c => c.OwnerTenantId == owner && c.IsDefault && c.Status == StorageConfigStatus.Active)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysStorageConfig>() : Maybe.Some(rows[0]);
    }

    private async Task<Result> ClearOtherDefaultsAsync(SysStorageConfig config, CancellationToken cancellationToken)
    {
        var owner = config.OwnerTenantId;
        var id = config.Id;
        var others = await configs.ListAsync(EntityQuery.All<SysStorageConfig>().Where(new Specification<SysStorageConfig>(c => c.OwnerTenantId == owner && c.IsDefault && c.Id != id)), cancellationToken).ConfigureAwait(false);
        foreach (var other in others)
        {
            other.IsDefault = false;
            var updated = await configs.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }

    private Result Apply(SysStorageConfig config, StorageConfigDefinition definition)
    {
        var bucket = definition.Bucket.Trim();
        if (!ContainerName.IsValid(bucket))
        {
            return Error.Validation(FilesErrors.BucketInvalid, FilesMessages.BucketInvalid, MessageArguments.Of(("bucket", definition.Bucket)));
        }

        config.Name = definition.Name.Trim();
        config.Type = definition.Type;
        config.Bucket = bucket;
        config.Order = definition.Order;
        config.Remark = string.IsNullOrWhiteSpace(definition.Remark) ? null : definition.Remark.Trim();
        if (definition.Type == StorageType.Local)
        {
            // 本地存储只用桶（根目录下的子目录），远程设置不留
            config.Endpoint = null;
            config.Region = null;
            config.AccessKeyId = null;
            config.SecretAccessKey = null;
            return Result.Success();
        }

        var endpoint = definition.Endpoint?.Trim();
        if (string.IsNullOrEmpty(endpoint))
        {
            return Error.Validation(FilesErrors.RemoteSettingsRequired, FilesMessages.RemoteSettingsRequired, MessageArguments.Of(("type", definition.Type)));
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Error.Validation(FilesErrors.EndpointInvalid, FilesMessages.EndpointInvalid, MessageArguments.Of(("endpoint", endpoint)));
        }

        var accessKeyId = definition.AccessKeyId?.Trim();
        var secret = string.IsNullOrWhiteSpace(definition.SecretAccessKey) ? null : definition.SecretAccessKey.Trim();
        if (string.IsNullOrEmpty(accessKeyId) || (secret is null && config.SecretAccessKey is null))
        {
            return Error.Validation(FilesErrors.RemoteSettingsRequired, FilesMessages.RemoteSettingsRequired, MessageArguments.Of(("type", definition.Type)));
        }

        config.Endpoint = endpoint;
        config.Region = string.IsNullOrWhiteSpace(definition.Region) ? null : definition.Region.Trim();
        config.AccessKeyId = accessKeyId;
        if (secret is not null)
        {
            config.SecretAccessKey = secrets.Protect(BlobStores.SecretPurpose, secret);
        }

        return Result.Success();
    }
}
