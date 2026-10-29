// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 新建编号规则。
/// </summary>
/// <remarks>
/// <c>POST /api/numbering-rules</c>。写在调用方这一侧；编码本侧唯一；日期格式要能区分重置周期（<c>SETTINGS_VALIDATION_005</c>），时区要解析得了（<c>SETTINGS_VALIDATION_006</c>）。
/// 只有平台规则能供租户使用。
/// </remarks>
/// <param name="rules">规则仓储。</param>
/// <param name="service">规则域服务。</param>
/// <param name="ids">标识生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesCreate)]
public sealed class CreateNumberingRule(IRepository<SysNumberingRule, Guid> rules, NumberingRuleService service, IIdGenerator<Guid> ids) : IOperation<CreateNumberingRuleRequest, NumberingRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.create";

    /// <inheritdoc />
    public async ValueTask<Result<NumberingRuleResponse>> HandleAsync(CreateNumberingRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        var code = request.Code.Trim();
        if (await service.IsCodeTakenAsync(code, owner, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(SettingsErrors.NumberingRuleCodeTaken, SettingsMessages.NumberingRuleCodeTaken, MessageArguments.Of(("code", code)));
        }

        var timeZoneId = request.TimeZoneId.Trim();
        var valid = NumberingFormatter.Validate(request.DateFormat, request.ResetCycle, timeZoneId);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var rule = new SysNumberingRule
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            Code = code,
            Name = request.Name.Trim(),
            Prefix = string.IsNullOrEmpty(request.Prefix) ? null : request.Prefix,
            Separator = request.Separator ?? string.Empty,
            DateFormat = request.DateFormat,
            SerialLength = request.SerialLength,
            ResetCycle = request.ResetCycle,
            TimeZoneId = timeZoneId,
            AllowTenantUse = SettingsScope.IsPlatform(owner) && request.AllowTenantUse,
            Order = request.Order,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };
        var added = await rules.AddAsync(rule, context.CancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : NumberingRuleResponse.From(rule);
    }
}
