// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 新建参数。
/// </summary>
/// <remarks>
/// <c>POST /api/configs</c>。写在调用方这一侧；键在本侧唯一；值与缺省值按数据类型校验，加密的值落库前加密。
/// 租户新建与平台参数同键的参数即覆盖它；平台参数不允许覆盖时 <c>SETTINGS_DENIED_002</c>。
/// </remarks>
/// <param name="configs">参数仓储。</param>
/// <param name="values">参数值规则。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsCreate)]
public sealed class CreateConfig(IRepository<SysConfig, Guid> configs, ConfigValues values, IIdGenerator<Guid> ids) : IOperation<CreateConfigRequest, ConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "configs.create";

    /// <inheritdoc />
    public async ValueTask<Result<ConfigResponse>> HandleAsync(CreateConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var key = request.Key.Trim();
        if (await configs.CountAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.OwnerTenantId == owner && c.Key == key)), context.CancellationToken).ConfigureAwait(false) > 0)
        {
            return Error.Conflict(SettingsErrors.ConfigKeyTaken, SettingsMessages.ConfigKeyTaken, MessageArguments.Of(("key", key)));
        }

        var platform = SettingsScope.IsPlatform(owner);
        if (!platform)
        {
            const string platformOwner = TenantIds.Platform;
            var shared = await configs.ListAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.OwnerTenantId == platformOwner && c.Key == key)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
            if (shared.Count > 0 && !shared[0].AllowTenantOverride)
            {
                return Error.Denied(SettingsErrors.TenantOverrideForbidden, SettingsMessages.TenantOverrideForbidden, MessageArguments.Of(("key", key)));
            }
        }

        var value = string.IsNullOrEmpty(request.Value) ? null : request.Value;
        var defaultValue = string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue;
        var valid = ConfigValues.Validate(request.DataType, value, defaultValue);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var config = new SysConfig
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            Key = key,
            Name = request.Name.Trim(),
            Group = string.IsNullOrWhiteSpace(request.Group) ? null : request.Group.Trim(),
            Value = request.IsEncrypted ? values.Protect(value) : value,
            DefaultValue = defaultValue,
            DataType = request.DataType,
            IsEncrypted = request.IsEncrypted,
            AllowTenantOverride = platform && request.AllowTenantOverride,
            Order = request.Order,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        var added = await configs.AddAsync(config, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : ConfigResponse.From(config);
    }
}
