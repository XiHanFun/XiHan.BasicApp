// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Domain.Services;

/// <summary>
/// <see cref="INumberGenerator"/> 的实现。
/// </summary>
/// <remarks>
/// 在独立作用域与事务里（<see cref="StoreScope"/>）发号并提交，外层调用失败也不回收。
/// 先对规则行做一次加锁更新，同一规则的发号由此串行：之后再查幂等、翻转周期（只前进）、推进流水、写发号记录，按格式快照拼出号码。
/// </remarks>
/// <param name="scopes">作用域工厂。</param>
/// <param name="clock">时间源。</param>
/// <param name="ids">标识生成器。</param>
public sealed class NumberGenerator(IServiceScopeFactory scopes, TimeProvider clock, IIdGenerator<Guid> ids) : INumberGenerator
{
    /// <inheritdoc />
    public async ValueTask<Result<NumberAllocation>> GenerateAsync(NumberRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RuleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.Count, NumberingFormatter.MaxBatch);
        var requester = SettingsScope.OwnerOf(context);
        if (request.Scope == NumberingScope.Tenant && SettingsScope.IsPlatform(requester))
        {
            return Error.Validation(SettingsErrors.NumberingScopeInvalid, SettingsMessages.NumberingScopeInvalid);
        }

        NumberAllocation? allocation = null;
        var outcome = await StoreScope.WriteAsync(scopes, async (services, cancellationToken) =>
        {
            var produced = await AllocateAsync(services, request, requester, cancellationToken).ConfigureAwait(false);
            if (!produced.TryGetValue(out var value))
            {
                return produced.Error!;
            }

            allocation = value;
            return Result.Success();
        }, context.CancellationToken).ConfigureAwait(false);
        return outcome.IsFailure ? outcome.Error! : allocation!;
    }

    /// <summary>
    /// 请求参数的指纹：同一幂等键下参数不同即冲突。
    /// </summary>
    public static string FingerprintOf(SysNumberingRule rule, NumberRequest request)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(request);
        var canonical = string.Join('|', rule.Id.ToString("N", CultureInfo.InvariantCulture), rule.Code, request.Scope.ToString(), request.Count.ToString(CultureInfo.InvariantCulture), request.BusinessType ?? string.Empty, request.BusinessId ?? string.Empty);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>
    /// 按发号记录的格式快照重建结果。
    /// </summary>
    public static NumberAllocation Rebuild(SysNumberingAllocation record, bool isReplay)
    {
        ArgumentNullException.ThrowIfNull(record);
        var numbers = NumberingFormatter.FormatRange(record.PrefixSnapshot, record.SeparatorSnapshot, record.DateTextSnapshot, record.SerialLengthSnapshot, record.StartValue, record.EndValue);
        var scope = string.Equals(record.OwnerTenantId, TenantIds.Platform, StringComparison.Ordinal) ? NumberingScope.Platform : NumberingScope.Tenant;
        return new NumberAllocation(record.RuleId, record.RuleCode, scope, record.RequestTenantId, record.IdempotencyKey, record.PeriodKey, record.StartValue, record.EndValue, numbers, record.GeneratedAt, isReplay);
    }

    private async Task<Result<NumberAllocation>> AllocateAsync(IServiceProvider services, NumberRequest request, string requester, CancellationToken cancellationToken)
    {
        var rules = services.GetRequiredService<IRepository<SysNumberingRule, Guid>>();
        var records = services.GetRequiredService<IRepository<SysNumberingAllocation, Guid>>();
        var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
        var resolved = await ResolveAsync(rules, request, requester, cancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var found))
        {
            return Error.NotFound(SettingsErrors.NumberingRuleNotFound, SettingsMessages.NumberingRuleCodeNotFound, MessageArguments.Of(("code", request.RuleCode)));
        }

        // 加锁：对规则行做一次不改值的更新，同一规则的并发发号在这里排队；锁持有到本事务提交
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        var ruleId = found.Id;
        await unitOfWork.ConnectionFor(typeof(SysNumberingRule)).Updateable<SysNumberingRule>()
            .SetColumns(static r => r.ConcurrencyStamp == r.ConcurrencyStamp)
            .Where(r => r.Id == ruleId)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        if (!(await rules.FindAsync(ruleId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(SettingsErrors.NumberingRuleNotFound, SettingsMessages.NumberingRuleCodeNotFound, MessageArguments.Of(("code", request.RuleCode)));
        }

        var fingerprint = FingerprintOf(rule, request);
        var owner = rule.OwnerTenantId;
        var key = request.IdempotencyKey;
        var previous = await records.ListAsync(
            EntityQuery.All<SysNumberingAllocation>().Where(new Specification<SysNumberingAllocation>(a => a.OwnerTenantId == owner && a.RuleId == ruleId && a.RequestTenantId == requester && a.IdempotencyKey == key)).Page(0, 1),
            cancellationToken).ConfigureAwait(false);
        if (previous.Count > 0)
        {
            return string.Equals(previous[0].Fingerprint, fingerprint, StringComparison.Ordinal)
                ? Rebuild(previous[0], isReplay: true)
                : Error.Conflict(SettingsErrors.IdempotencyMismatch, SettingsMessages.IdempotencyMismatch, MessageArguments.Of(("key", key)));
        }

        if (rule.Status != SettingStatus.Active)
        {
            return Error.Conflict(SettingsErrors.NumberingRuleDisabled, SettingsMessages.NumberingRuleDisabled, MessageArguments.Of(("code", rule.Code)));
        }

        if (!NumberingFormatter.TryResolveTimeZone(rule.TimeZoneId, out var timeZone))
        {
            return Error.Validation(SettingsErrors.TimeZoneInvalid, SettingsMessages.TimeZoneInvalid, MessageArguments.Of(("timeZone", rule.TimeZoneId)));
        }

        var now = clock.GetUtcNow();
        var local = NumberingFormatter.ToRuleTime(now, timeZone);
        var ordinal = NumberingFormatter.PeriodOrdinal(local, rule.ResetCycle);
        if (ordinal < rule.CurrentPeriodOrdinal)
        {
            return Error.Conflict(SettingsErrors.NumberingClockBehind, SettingsMessages.NumberingClockBehind, MessageArguments.Of(("code", rule.Code), ("period", rule.CurrentPeriod)));
        }

        if (ordinal > rule.CurrentPeriodOrdinal)
        {
            rule.CurrentValue = 0;
            rule.CurrentPeriod = NumberingFormatter.PeriodKey(local, rule.ResetCycle);
            rule.CurrentPeriodOrdinal = ordinal;
        }

        var maximum = NumberingFormatter.MaxValue(rule.SerialLength);
        if (rule.CurrentValue > maximum - request.Count)
        {
            return Error.Conflict(SettingsErrors.NumberingExhausted, SettingsMessages.NumberingExhausted, MessageArguments.Of(("code", rule.Code), ("max", maximum)));
        }

        var start = rule.CurrentValue + 1;
        var end = rule.CurrentValue + request.Count;
        rule.CurrentValue = end;
        rule.HasAllocated = true;
        var updated = await rules.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var record = new SysNumberingAllocation
        {
            Id = ids.NewId(),
            OwnerTenantId = owner,
            RuleId = rule.Id,
            RuleCode = rule.Code,
            RequestTenantId = requester,
            IdempotencyKey = key,
            Fingerprint = fingerprint,
            Count = request.Count,
            StartValue = start,
            EndValue = end,
            PeriodKey = rule.CurrentPeriod,
            PrefixSnapshot = rule.Prefix,
            SeparatorSnapshot = rule.Separator,
            DateTextSnapshot = NumberingFormatter.DateText(local, rule.DateFormat),
            SerialLengthSnapshot = rule.SerialLength,
            GeneratedAt = now,
            BusinessType = request.BusinessType,
            BusinessId = request.BusinessId,
        };
        var added = await records.AddAsync(record, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : Rebuild(record, isReplay: false);
    }

    private static async Task<Maybe<SysNumberingRule>> ResolveAsync(IRepository<SysNumberingRule, Guid> rules, NumberRequest request, string requester, CancellationToken cancellationToken)
    {
        var code = request.RuleCode;
        async Task<Maybe<SysNumberingRule>> FindAsync(string owner)
        {
            var rows = await rules.ListAsync(EntityQuery.All<SysNumberingRule>().Where(new Specification<SysNumberingRule>(r => r.OwnerTenantId == owner && r.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
            return rows.Count == 0 ? Maybe.None<SysNumberingRule>() : Maybe.Some(rows[0]);
        }

        async Task<Maybe<SysNumberingRule>> FindPlatformAsync(bool forTenant)
        {
            var found = await FindAsync(TenantIds.Platform).ConfigureAwait(false);
            return found.TryGetValue(out var rule) && (!forTenant || rule.AllowTenantUse) ? found : Maybe.None<SysNumberingRule>();
        }

        if (SettingsScope.IsPlatform(requester))
        {
            return await FindPlatformAsync(forTenant: false).ConfigureAwait(false);
        }

        return request.Scope switch
        {
            NumberingScope.Tenant => await FindAsync(requester).ConfigureAwait(false),
            NumberingScope.Platform => await FindPlatformAsync(forTenant: true).ConfigureAwait(false),
            _ => (await FindAsync(requester).ConfigureAwait(false)) is { HasValue: true } own ? own : await FindPlatformAsync(forTenant: true).ConfigureAwait(false),
        };
    }
}
