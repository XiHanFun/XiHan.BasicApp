// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Numbering;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 业务编号应用层 DTO、领域命令与实体映射器。
/// </summary>
/// <remarks>
/// 映射器只负责层间形状转换，不执行租户解析或业务校验。所有可能达到 18 位的流水边界在 API DTO 中转为字符串，
/// 防止 JavaScript <c>Number</c> 在前端传输或展示时丢失整数精度。
/// </remarks>
public static class NumberingApplicationMapper
{
    /// <summary>
    /// 把创建 DTO 映射为规则领域创建命令。
    /// </summary>
    /// <param name="input">未经领域归一化的创建 DTO。</param>
    /// <returns>不包含租户 ID 的领域创建命令。</returns>
    /// <remarks>租户归属由领域服务读取当前上下文，映射器不能从外部 DTO 注入租户身份。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    public static NumberingRuleCreateCommand ToCreateCommand(NumberingRuleCreateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new NumberingRuleCreateCommand(
            input.RuleCode, input.RuleName, input.Prefix, input.Separator, input.DateFormat, input.SerialLength,
            input.ResetCycle, input.TimeZoneId, input.AllowTenantUse, input.Status, input.Sort, input.Remark);
    }

    /// <summary>
    /// 把更新 DTO 映射为规则领域更新命令。
    /// </summary>
    /// <param name="input">未经领域归一化的更新 DTO。</param>
    /// <returns>保留规则主键和可编辑字段的领域更新命令。</returns>
    /// <remarks>规则编码、当前周期、当前流水和已发号标志不属于更新命令，避免应用层绕过领域冻结策略。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    public static NumberingRuleUpdateCommand ToUpdateCommand(NumberingRuleUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new NumberingRuleUpdateCommand(
            input.BasicId, input.RuleName, input.Prefix, input.Separator, input.DateFormat, input.SerialLength,
            input.ResetCycle, input.TimeZoneId, input.AllowTenantUse, input.Sort, input.Remark);
    }

    /// <summary>
    /// 把规则实体映射为列表 DTO。
    /// </summary>
    /// <param name="rule">当前查询作用域内的规则实体。</param>
    /// <returns>适合分页列表展示的规则 DTO。</returns>
    /// <remarks>当前流水以固定文化转换为字符串，保证 18 位边界在前端无精度损失。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> 为 <see langword="null"/>。</exception>
    public static NumberingRuleListItemDto ToListItemDto(SysNumberingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new NumberingRuleListItemDto
        {
            BasicId = rule.BasicId,
            RuleCode = rule.RuleCode,
            RuleName = rule.RuleName,
            Prefix = rule.Prefix,
            Separator = rule.Separator,
            DateFormat = rule.DateFormat,
            SerialLength = rule.SerialLength,
            ResetCycle = rule.ResetCycle,
            TimeZoneId = rule.TimeZoneId,
            CurrentValue = rule.CurrentValue.ToString(CultureInfo.InvariantCulture),
            CurrentPeriod = rule.CurrentPeriod,
            HasAllocated = rule.HasAllocated,
            AllowTenantUse = rule.AllowTenantUse,
            IsGlobal = rule.IsGlobal,
            Status = rule.Status,
            Sort = rule.Sort,
            Remark = rule.Remark
        };
    }

    /// <summary>
    /// 把规则实体映射为包含审计时间的详情 DTO。
    /// </summary>
    /// <param name="rule">当前查询作用域内的规则实体。</param>
    /// <returns>规则完整配置、流水状态和审计时间。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> 为 <see langword="null"/>。</exception>
    public static NumberingRuleDetailDto ToDetailDto(SysNumberingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var item = ToListItemDto(rule);
        return new NumberingRuleDetailDto
        {
            BasicId = item.BasicId,
            RuleCode = item.RuleCode,
            RuleName = item.RuleName,
            Prefix = item.Prefix,
            Separator = item.Separator,
            DateFormat = item.DateFormat,
            SerialLength = item.SerialLength,
            ResetCycle = item.ResetCycle,
            TimeZoneId = item.TimeZoneId,
            CurrentValue = item.CurrentValue,
            CurrentPeriod = item.CurrentPeriod,
            HasAllocated = item.HasAllocated,
            AllowTenantUse = item.AllowTenantUse,
            IsGlobal = item.IsGlobal,
            Status = item.Status,
            Sort = item.Sort,
            Remark = item.Remark,
            CreatedTime = rule.CreatedTime,
            ModifiedTime = rule.ModifiedTime
        };
    }

    /// <summary>
    /// 把永久分配记录映射为发号审计列表项。
    /// </summary>
    /// <param name="allocation">包含流水区间和格式快照的永久分配记录。</param>
    /// <param name="formatter">用于依据快照重建编号文本的格式器。</param>
    /// <returns>包含字符串流水边界以及首尾编号的审计 DTO。</returns>
    /// <remarks>
    /// 分页列表只重建首尾编号，不展开最多 1000 个完整结果，避免页大小与批量数量相乘造成不必要的内存分配。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="allocation"/> 或 <paramref name="formatter"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentOutOfRangeException">永久快照中的流水位数或边界无效。</exception>
    public static NumberingAllocationListItemDto ToAllocationListItemDto(SysNumberingAllocation allocation, INumberingFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        ArgumentNullException.ThrowIfNull(formatter);
        return new NumberingAllocationListItemDto
        {
            BasicId = allocation.BasicId,
            RuleId = allocation.RuleId,
            RuleCode = allocation.RuleCode,
            RequestTenantId = allocation.RequestTenantId,
            IdempotencyKey = allocation.IdempotencyKey,
            Count = allocation.Count,
            StartValue = allocation.StartValue.ToString(CultureInfo.InvariantCulture),
            EndValue = allocation.EndValue.ToString(CultureInfo.InvariantCulture),
            PeriodKey = allocation.PeriodKey,
            FirstNumber = formatter.Format(allocation.PrefixSnapshot, allocation.SeparatorSnapshot, allocation.DateTextSnapshot, allocation.SerialLengthSnapshot, allocation.StartValue),
            LastNumber = formatter.Format(allocation.PrefixSnapshot, allocation.SeparatorSnapshot, allocation.DateTextSnapshot, allocation.SerialLengthSnapshot, allocation.EndValue),
            GeneratedAtUtc = allocation.GeneratedAtUtc,
            BusinessType = allocation.BusinessType,
            BusinessId = allocation.BusinessId
        };
    }

    /// <summary>
    /// 把内部编号生成结果映射为 Dynamic API DTO。
    /// </summary>
    /// <param name="result">首次分配或幂等重放的内部结果。</param>
    /// <returns>保留完整编号列表并将流水边界转为字符串的 API DTO。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> 为 <see langword="null"/>。</exception>
    public static NumberGenerationResultDto ToGenerationResultDto(NumberGenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new NumberGenerationResultDto
        {
            RuleId = result.RuleId,
            RuleCode = result.RuleCode,
            ResolvedScope = result.ResolvedScope,
            IdempotencyKey = result.IdempotencyKey,
            PeriodKey = result.PeriodKey,
            StartValue = result.StartValue.ToString(CultureInfo.InvariantCulture),
            EndValue = result.EndValue.ToString(CultureInfo.InvariantCulture),
            Numbers = result.Numbers,
            GeneratedAtUtc = result.GeneratedAtUtc,
            IsIdempotentReplay = result.IsIdempotentReplay
        };
    }
}
