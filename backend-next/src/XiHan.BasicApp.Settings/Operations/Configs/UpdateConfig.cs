// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.Configs;

/// <summary>
/// 修改参数。
/// </summary>
/// <remarks>
/// <c>PUT /api/configs/{id}</c>。只能改本侧的（另一侧 <c>SETTINGS_DENIED_001</c>）；键不可改，内建参数不能改数据类型。
/// 加密：仍加密且没给新值即保留原密文；新开加密即加密现值；关掉加密且没给新值即解密现值。值按（新的）数据类型校验。
/// </remarks>
/// <param name="configs">参数仓储。</param>
/// <param name="values">参数值规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.ConfigsUpdate)]
public sealed class UpdateConfig(IRepository<SysConfig, Guid> configs, ConfigValues values) : IOperation<UpdateConfigRequest, ConfigResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "configs.update";

    /// <inheritdoc />
    public async ValueTask<Result<ConfigResponse>> HandleAsync(UpdateConfigRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await configs.FindAsync(request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var config) || !SettingsScope.IsVisible(config, owner))
        {
            return Error.NotFound(SettingsErrors.ConfigNotFound, SettingsMessages.ConfigNotFound, MessageArguments.Of(("id", request.Id)));
        }

        if (!SettingsScope.IsOwnedBy(config, owner))
        {
            return Error.Denied(SettingsErrors.OtherSide, SettingsMessages.OtherSide, MessageArguments.Of(("id", request.Id)));
        }

        if (config.IsBuiltIn && request.DataType != config.DataType)
        {
            return Error.Conflict(SettingsErrors.BuiltInProtected, SettingsMessages.BuiltInProtected, MessageArguments.Of(("id", request.Id)));
        }

        var provided = string.IsNullOrEmpty(request.Value) ? null : request.Value;
        var next = Next(config, request, provided);
        if (!next.TryGetValue(out var plan))
        {
            return next.Error!;
        }

        var defaultValue = string.IsNullOrEmpty(request.DefaultValue) ? null : request.DefaultValue;
        var valid = ConfigValues.Validate(request.DataType, plan.Plaintext.ValueOr(string.Empty), defaultValue);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        config.Name = request.Name.Trim();
        config.Group = string.IsNullOrWhiteSpace(request.Group) ? null : request.Group.Trim();
        config.Value = plan.Stored;
        config.DefaultValue = defaultValue;
        config.DataType = request.DataType;
        config.IsEncrypted = request.IsEncrypted;
        config.AllowTenantOverride = SettingsScope.IsPlatform(owner) && request.AllowTenantOverride;
        config.Order = request.Order;
        config.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        config.Status = request.Status;
        var updated = await configs.UpdateAsync(config, context.CancellationToken).ConfigureAwait(false);
        return updated.IsFailure ? updated.Error! : ConfigResponse.From(config);
    }

    /// <summary>
    /// 算出要存的值与用来校验的明文（明文为 <c>None</c> 表示沿用原值且不需要再校验）。
    /// </summary>
    private Result<(string? Stored, Maybe<string> Plaintext)> Next(SysConfig config, UpdateConfigRequest request, string? provided)
    {
        var typeChanged = request.DataType != config.DataType;
        if (config.IsEncrypted && request.IsEncrypted)
        {
            if (provided is not null)
            {
                return (values.Protect(provided), Maybe.Some(provided));
            }

            if (!typeChanged)
            {
                return (config.Value, Maybe.None<string>());
            }

            var current = values.PlaintextOf(config);
            if (!current.TryGetValue(out var kept))
            {
                return current.Error!;
            }

            return (config.Value, kept);
        }

        if (!config.IsEncrypted && request.IsEncrypted)
        {
            var plaintext = provided ?? config.Value;
            return (values.Protect(plaintext), Maybe.From(plaintext));
        }

        if (config.IsEncrypted && !request.IsEncrypted)
        {
            if (provided is not null)
            {
                return (provided, Maybe.Some(provided));
            }

            var current = values.PlaintextOf(config);
            if (!current.TryGetValue(out var decrypted))
            {
                return current.Error!;
            }

            return (decrypted.TryGetValue(out var text) ? text : null, decrypted);
        }

        return (provided, Maybe.From(provided));
    }
}
