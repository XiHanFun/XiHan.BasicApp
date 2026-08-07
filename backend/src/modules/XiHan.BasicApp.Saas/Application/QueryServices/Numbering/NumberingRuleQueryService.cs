// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 业务编号规则、格式预览和永久发号记录查询 Dynamic API。
/// </summary>
/// <remarks>
/// 查询服务负责把外部 <see cref="NumberingScope"/> 解析为明确的规则所属租户和数据库位置。
/// 所有客户端过滤与排序先经过字段安全校验，再追加调用方无法覆盖的租户、规则和全局开放条件。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "业务编号查询")]
public sealed class NumberingRuleQueryService : SaasApplicationService, INumberingRuleQueryService
{
    /// <summary>
    /// 规则仓储，用于在已解析的租户或平台数据库中执行显式作用域过滤的分页与详情查询。
    /// </summary>
    private readonly INumberingRuleRepository _ruleRepository;

    /// <summary>
    /// 永久分配记录仓储，用于按实际规则和请求租户隔离发号审计查询。
    /// </summary>
    private readonly INumberingAllocationRepository _allocationRepository;

    /// <summary>
    /// 格式器，用于无副作用预览、时区目录查询以及从快照重建首尾编号。
    /// </summary>
    private readonly INumberingFormatter _formatter;

    /// <summary>
    /// 当前租户上下文，用于解析 <see cref="NumberingScope.Auto"/> 并在全局查询期间切换平台数据库。
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 字段安全服务，先校验客户端过滤与排序字段，再由本服务追加不可覆盖的租户过滤条件。
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 可替换时间源，保证格式预览和测试中的规则本地时间计算可重复。
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// 初始化业务编号查询服务。
    /// </summary>
    /// <param name="ruleRepository">规则仓储。</param>
    /// <param name="allocationRepository">分配记录仓储。</param>
    /// <param name="formatter">格式策略。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="fieldSecurity">字段安全服务。</param>
    /// <param name="timeProvider">时间提供器。</param>
    public NumberingRuleQueryService(
        INumberingRuleRepository ruleRepository,
        INumberingAllocationRepository allocationRepository,
        INumberingFormatter formatter,
        ICurrentTenant currentTenant,
        IFieldSecurityService fieldSecurity,
        TimeProvider timeProvider)
    {
        _ruleRepository = ruleRepository;
        _allocationRepository = allocationRepository;
        _formatter = formatter;
        _currentTenant = currentTenant;
        _fieldSecurity = fieldSecurity;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 按当前上下文与请求作用域分页查询编号规则。
    /// </summary>
    /// <param name="input">分页查询 DTO，包含关键字、状态以及可选的通用过滤和排序条件。</param>
    /// <param name="cancellationToken">用于取消字段安全校验和数据库查询的取消令牌。</param>
    /// <returns>仅包含当前作用域可见规则的分页结果。</returns>
    /// <remarks>租户查询全局作用域时只返回已启用且允许租户使用的规则；平台或单体上下文中的自动作用域解析为全局规则。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">作用域无效，或平台上下文请求了未指定租户的私有规则。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Read)]
    public async Task<PageResultDtoBase<NumberingRuleListItemDto>> GetNumberingRulePageAsync(
        NumberingRulePageQueryDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        // 先把模糊的 Auto 作用域解析为明确数据库和所属租户，后续查询不再自行猜测作用域。
        var scope = ResolveQueryScope(input.Scope);
        return await ExecuteInScopeAsync(
            scope,
            () => QueryRulePageCoreAsync(input, scope, availableGlobalOnly: scope.IsGlobal && scope.RequestTenantId > 0, cancellationToken));
    }

    /// <summary>
    /// 分页查询当前租户可以调用的全局编号规则。
    /// </summary>
    /// <param name="input">分页查询 DTO，包含关键字和经过字段安全检查的通用条件。</param>
    /// <param name="cancellationToken">用于取消字段安全校验和平台库查询的取消令牌。</param>
    /// <returns>平台库中已启用且向租户开放的全局规则分页结果。</returns>
    /// <remarks>原请求租户会保留在内部查询作用域中，用于表达“租户查看全局规则”，数据库读取则在平台上下文执行。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Read)]
    public async Task<PageResultDtoBase<NumberingRuleListItemDto>> GetAvailableGlobalNumberingRulePageAsync(
        NumberingRulePageQueryDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var scope = new NumberingQueryScope(0, true, _currentTenant.Id ?? 0);
        return await ExecuteInScopeAsync(
            scope,
            () => QueryRulePageCoreAsync(input, scope, availableGlobalOnly: true, cancellationToken));
    }

    /// <summary>
    /// 查询明确作用域内的编号规则详情。
    /// </summary>
    /// <param name="id">规则主键，必须为正数。</param>
    /// <param name="scope">请求作用域；租户使用全局作用域时切换到平台库。</param>
    /// <param name="cancellationToken">用于取消规则查询的取消令牌。</param>
    /// <returns>规则详情；当前作用域内不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="UserFriendlyException">主键或作用域无效，或者全局规则未向当前租户开放。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Read)]
    public async Task<NumberingRuleDetailDto?> GetNumberingRuleDetailAsync(
        long id,
        NumberingScope scope = NumberingScope.Auto,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new UserFriendlyException("规则主键无效。");
        }

        var queryScope = ResolveQueryScope(scope);
        return await ExecuteInScopeAsync(queryScope, async () =>
        {
            var rule = await _ruleRepository.FindByIdInScopeAsync(queryScope.OwnerTenantId, id, cancellationToken);
            if (rule is null)
            {
                return null;
            }

            if (queryScope.IsGlobal && queryScope.RequestTenantId > 0
                && (rule.Status != EnableStatus.Enabled || !rule.AllowTenantUse))
            {
                // 租户读取全局详情也必须执行开放状态校验，不能仅依赖列表页过滤保障安全边界。
                throw new UserFriendlyException("该全局编号规则未向当前租户开放。");
            }

            return NumberingApplicationMapper.ToDetailDto(rule);
        });
    }

    /// <summary>
    /// 分页查询指定规则的永久发号记录。
    /// </summary>
    /// <param name="input">包含规则主键、作用域、分页和筛选条件的查询 DTO。</param>
    /// <param name="cancellationToken">用于取消规则校验、字段安全校验和记录查询的取消令牌。</param>
    /// <returns>使用格式快照重建首尾编号的发号记录分页结果。</returns>
    /// <remarks>租户查看共享全局规则时，内部会强制追加原请求租户条件，防止读取其他租户的业务关联和幂等信息。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">规则主键、作用域、规则存在性或全局开放状态校验失败。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.AllocationRead)]
    public async Task<PageResultDtoBase<NumberingAllocationListItemDto>> GetNumberingAllocationPageAsync(
        NumberingAllocationPageQueryDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.RuleId <= 0)
        {
            throw new UserFriendlyException("规则主键无效。");
        }

        var scope = ResolveQueryScope(input.Scope);
        return await ExecuteInScopeAsync(scope, () => QueryAllocationPageCoreAsync(input, scope, cancellationToken));
    }

    /// <summary>
    /// 以指定示例流水值预览编号格式，不消耗真实流水。
    /// </summary>
    /// <param name="input">包含格式、时区和字符串形式示例流水值的预览 DTO。</param>
    /// <param name="cancellationToken">执行纯计算前检查的取消令牌。</param>
    /// <returns>示例编号、规则本地时间和当前周期键。</returns>
    /// <remarks>预览只调用纯格式计算器，不查询规则、不创建工作单元，也不会插入永久分配记录。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">格式组合、时区、流水位数或示例流水值无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Read)]
    public Task<NumberingPreviewResultDto> PreviewNumberingFormatAsync(
        NumberingPreviewDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var context = CreatePreviewContext(input);

            var result = new NumberingPreviewResultDto
            {
                Number = _formatter.Format(
                    input.Prefix,
                    input.Separator ?? string.Empty,
                    context.DateText,
                    input.SerialLength,
                    context.SampleValue),
                RuleLocalTime = context.RuleLocalTime,
                PeriodKey = context.PeriodKey
            };
            return Task.FromResult(result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new UserFriendlyException(exception.Message, innerException: exception);
        }
    }

    /// <summary>
    /// 从指定示例流水开始连续预览 1 至 50 个编号，不消耗真实流水。
    /// </summary>
    /// <param name="input">包含格式、时区、字符串形式起始流水和批量数量的预览 DTO。</param>
    /// <param name="cancellationToken">执行纯计算前检查的取消令牌。</param>
    /// <returns>连续编号、流水区间、规则本地时间和周期键。</returns>
    /// <remarks>
    /// 数量上限在调用格式器前校验，避免异常请求产生不必要的列表分配；格式计算仍复用真实发号的格式器，
    /// 因而预览能够覆盖固定位数耗尽和时区周期边界，但不会查询数据库或产生审计分配记录。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">数量、格式组合、时区、流水位数、起始流水或区间容量无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [HttpPost]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Read)]
    public Task<NumberingBatchPreviewResultDto> PreviewNumberingBatchAsync(
        NumberingBatchPreviewDto input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (input.Count is < 1 or > NumberingBatchPreviewDto.MaximumCount)
        {
            throw new UserFriendlyException($"单次批量预览数量必须在 1 至 {NumberingBatchPreviewDto.MaximumCount} 之间。");
        }

        try
        {
            var context = CreatePreviewContext(input);
            // Count 已限制为 50，且示例流水最多 18 位；checked 仍明确保护未来上限调整导致的整数溢出。
            var endValue = checked(context.SampleValue + input.Count - 1L);
            var numbers = _formatter.FormatRange(
                input.Prefix,
                input.Separator ?? string.Empty,
                context.DateText,
                input.SerialLength,
                context.SampleValue,
                endValue);
            var result = new NumberingBatchPreviewResultDto
            {
                StartValue = context.SampleValue.ToString(CultureInfo.InvariantCulture),
                EndValue = endValue.ToString(CultureInfo.InvariantCulture),
                Numbers = numbers,
                RuleLocalTime = context.RuleLocalTime,
                PeriodKey = context.PeriodKey
            };
            return Task.FromResult(result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            throw new UserFriendlyException(exception.Message, innerException: exception);
        }
    }

    /// <summary>
    /// 校验公共预览参数并计算单个与批量预览共享的时间、日期段、周期和起始流水上下文。
    /// </summary>
    /// <param name="input">格式参数和字符串形式示例流水值。</param>
    /// <returns>已经通过格式与时区校验的纯计算上下文。</returns>
    /// <exception cref="UserFriendlyException">示例流水不是 1 至 18 位正整数。</exception>
    /// <exception cref="ArgumentException">格式器判定参数超出范围。</exception>
    /// <exception cref="InvalidOperationException">格式组合或时区不可用。</exception>
    private NumberingPreviewContext CreatePreviewContext(NumberingPreviewDto input)
    {
        // 使用与规则创建/更新相同的格式校验入口，确保预览成功的配置能够被真实规则接受。
        _formatter.Validate(input.DateFormat, input.ResetCycle, input.SerialLength, input.TimeZoneId);
        var localTime = _formatter.ConvertToRuleTime(_timeProvider.GetUtcNow(), input.TimeZoneId);
        var dateText = _formatter.GetDateText(localTime, input.DateFormat);
        // 示例流水以字符串传输，避免浏览器对 18 位整数进行不安全的浮点转换。
        if (!long.TryParse(input.SampleValue, NumberStyles.None, CultureInfo.InvariantCulture, out var sampleValue)
            || sampleValue < 1)
        {
            throw new UserFriendlyException("预览流水值必须是 1 至 18 位正整数。");
        }

        return new NumberingPreviewContext(
            localTime,
            dateText,
            _formatter.GetPeriodKey(localTime, input.ResetCycle),
            sampleValue);
    }

    /// <summary>
    /// 在已选数据库内执行规则分页，并在字段安全校验后追加不可被前端覆盖的作用域约束。
    /// </summary>
    /// <param name="input">原始规则分页 DTO。</param>
    /// <param name="scope">已经解析的规则所属租户、数据库位置和原请求租户。</param>
    /// <param name="availableGlobalOnly">是否强制仅返回已启用且允许租户使用的全局规则。</param>
    /// <param name="cancellationToken">用于取消字段安全校验和数据库查询的取消令牌。</param>
    /// <returns>映射为列表 DTO 的规则分页结果。</returns>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<PageResultDtoBase<NumberingRuleListItemDto>> QueryRulePageCoreAsync(
        NumberingRulePageQueryDto input,
        NumberingQueryScope scope,
        bool availableGlobalOnly,
        CancellationToken cancellationToken)
    {
        var request = BuildRulePageRequest(input);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, nameof(SysNumberingRule), cancellationToken);
        await _fieldSecurity.GuardSortsAsync(request.Conditions, nameof(SysNumberingRule), cancellationToken);

        // 内部强制过滤必须放在字段安全处理后，避免租户输入覆盖所属范围或全局开放状态。
        request.Conditions.AddFilter((SysNumberingRule rule) => rule.TenantId, scope.OwnerTenantId);
        if (availableGlobalOnly)
        {
            request.Conditions.AddFilter((SysNumberingRule rule) => rule.AllowTenantUse, true);
            request.Conditions.AddFilter((SysNumberingRule rule) => rule.Status, EnableStatus.Enabled);
        }

        if (request.Conditions.Sorts.Count == 0)
        {
            // 默认排序使用稳定的二级键，避免相同 Sort 值在翻页时产生不确定顺序。
            request.Conditions.AddSort((SysNumberingRule rule) => rule.Sort, SortDirection.Ascending, 0);
            request.Conditions.AddSort((SysNumberingRule rule) => rule.RuleCode, SortDirection.Ascending, 1);
        }

        var page = await _ruleRepository.GetPagedAsync(request, cancellationToken);
        return new PageResultDtoBase<NumberingRuleListItemDto>(
            page.Items.Select(NumberingApplicationMapper.ToListItemDto).ToList(),
            page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <summary>
    /// 在已选数据库内执行发号记录分页，并校验租户只能查看自己的全局规则调用记录。
    /// </summary>
    /// <param name="input">包含规则主键的发号记录分页 DTO。</param>
    /// <param name="scope">已经解析的规则所属租户、数据库位置和原请求租户。</param>
    /// <param name="cancellationToken">用于取消规则查询、字段安全校验和分页查询的取消令牌。</param>
    /// <returns>映射为审计列表 DTO 的永久分配记录分页结果。</returns>
    /// <exception cref="UserFriendlyException">规则不存在或全局规则未向当前租户开放。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task<PageResultDtoBase<NumberingAllocationListItemDto>> QueryAllocationPageCoreAsync(
        NumberingAllocationPageQueryDto input,
        NumberingQueryScope scope,
        CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.FindByIdInScopeAsync(scope.OwnerTenantId, input.RuleId, cancellationToken)
            ?? throw new UserFriendlyException("业务编号规则不存在。");
        if (scope.IsGlobal && scope.RequestTenantId > 0 && !rule.AllowTenantUse)
        {
            throw new UserFriendlyException("该全局编号规则未向当前租户开放。");
        }

        var request = BuildAllocationPageRequest(input);
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, nameof(SysNumberingAllocation), cancellationToken);
        await _fieldSecurity.GuardSortsAsync(request.Conditions, nameof(SysNumberingAllocation), cancellationToken);
        request.Conditions.AddFilter((SysNumberingAllocation allocation) => allocation.TenantId, scope.OwnerTenantId);
        request.Conditions.AddFilter((SysNumberingAllocation allocation) => allocation.RuleId, input.RuleId);
        if (scope.IsGlobal && scope.RequestTenantId > 0)
        {
            // 全局序列由多个租户共享，但租户只能查看自己产生的审计记录。
            request.Conditions.AddFilter((SysNumberingAllocation allocation) => allocation.RequestTenantId, scope.RequestTenantId);
        }

        if (request.Conditions.Sorts.Count == 0)
        {
            // UTC 发号时间与主键共同形成稳定倒序，保证相同时刻写入的记录分页顺序确定。
            request.Conditions.AddSort((SysNumberingAllocation allocation) => allocation.GeneratedTime, SortDirection.Descending, 0);
            request.Conditions.AddSort((SysNumberingAllocation allocation) => allocation.BasicId, SortDirection.Descending, 1);
        }

        var page = await _allocationRepository.GetPagedAsync(request, cancellationToken);
        return new PageResultDtoBase<NumberingAllocationListItemDto>(
            page.Items.Select(allocation => NumberingApplicationMapper.ToAllocationListItemDto(allocation, _formatter)).ToList(),
            page.Page)
        {
            ExtendDatas = page.ExtendDatas
        };
    }

    /// <summary>
    /// 构建规则分页请求并复制前端通用过滤与排序条件。
    /// </summary>
    /// <param name="input">原始规则分页 DTO。</param>
    /// <returns>尚未追加内部租户约束的通用分页请求。</returns>
    private static BasicAppPRDto BuildRulePageRequest(NumberingRulePageQueryDto input)
    {
        var request = new BasicAppPRDto { Page = input.Page, Conditions = new QueryConditions() };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysNumberingRule>(
                input.Keyword.Trim(), rule => rule.RuleCode, rule => rule.RuleName, rule => rule.Remark);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysNumberingRule rule) => rule.Status, input.Status.Value);
        }

        CopyClientConditions(input, request);
        return request;
    }

    /// <summary>
    /// 构建发号记录分页请求。
    /// </summary>
    /// <param name="input">原始发号记录分页 DTO。</param>
    /// <returns>尚未追加规则与租户约束的通用分页请求。</returns>
    private static BasicAppPRDto BuildAllocationPageRequest(NumberingAllocationPageQueryDto input)
    {
        var request = new BasicAppPRDto { Page = input.Page, Conditions = new QueryConditions() };
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysNumberingAllocation>(
                input.Keyword.Trim(), allocation => allocation.IdempotencyKey, allocation => allocation.BusinessType, allocation => allocation.BusinessId);
        }

        CopyClientConditions(input, request);
        return request;
    }

    /// <summary>
    /// 复制调用方通用过滤和排序；字段安全服务会在追加内部作用域条件前审查这些输入。
    /// </summary>
    /// <param name="input">包含调用方通用条件的分页 DTO。</param>
    /// <param name="request">接收条件的内部分页请求。</param>
    private static void CopyClientConditions(BasicAppPRDto input, BasicAppPRDto request)
    {
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }
    }

    /// <summary>
    /// 把外部作用域解析为明确规则所属租户和数据库位置。
    /// </summary>
    /// <param name="scope">调用方请求的规则作用域。</param>
    /// <returns>包含规则所属租户、是否使用平台库和原请求租户的内部作用域。</returns>
    /// <exception cref="UserFriendlyException">作用域未定义，或平台/单体上下文请求了未指定租户的私有规则。</exception>
    private NumberingQueryScope ResolveQueryScope(NumberingScope scope)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("编号作用域无效。");
        }

        var requestTenantId = _currentTenant.Id ?? 0;
        if (requestTenantId == 0)
        {
            // 没有当前租户既覆盖平台管理，也覆盖普通单体部署；两者都把 Auto 解释为全局规则。
            if (scope == NumberingScope.Tenant)
            {
                throw new UserFriendlyException("平台或单体上下文不能查询未指定租户的私有规则。");
            }

            return new NumberingQueryScope(0, true, 0);
        }

        return scope == NumberingScope.Global
            ? new NumberingQueryScope(0, true, requestTenantId)
            : new NumberingQueryScope(requestTenantId, false, requestTenantId);
    }

    /// <summary>
    /// 必要时切换平台数据库后执行查询；切换范围覆盖完整异步查询，结束后自动恢复原租户。
    /// </summary>
    /// <typeparam name="TResult">查询结果类型。</typeparam>
    /// <param name="scope">已经解析的查询作用域。</param>
    /// <param name="action">必须在目标数据库上下文内完整执行的异步查询。</param>
    /// <returns>查询委托返回的结果。</returns>
    /// <exception cref="OperationCanceledException">查询委托内部操作被取消。</exception>
    private async Task<TResult> ExecuteInScopeAsync<TResult>(NumberingQueryScope scope, Func<Task<TResult>> action)
    {
        if (scope.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            // Change 的生命周期必须覆盖 await；提前释放会让独立数据库租户在错误连接上继续执行查询。
            using var platformScope = _currentTenant.Change(null);
            return await action();
        }

        return await action();
    }

    /// <summary>
    /// 查询作用域内部值对象。
    /// </summary>
    /// <param name="OwnerTenantId">规则实体的所属租户；0 表示平台全局规则。</param>
    /// <param name="IsGlobal">是否需要读取平台全局规则库。</param>
    /// <param name="RequestTenantId">发起查询的原租户；平台或单体上下文为 0。</param>
    private sealed record NumberingQueryScope(long OwnerTenantId, bool IsGlobal, long RequestTenantId);

    /// <summary>
    /// 单个与批量格式预览共享的纯计算上下文。
    /// </summary>
    /// <param name="RuleLocalTime">执行预览时的规则时区本地时间。</param>
    /// <param name="DateText">按规则日期格式生成的日期段；无日期格式时为 <see langword="null"/>。</param>
    /// <param name="PeriodKey">按规则重置周期生成的周期键。</param>
    /// <param name="SampleValue">已经安全解析的示例起始流水值。</param>
    private sealed record NumberingPreviewContext(
        DateTimeOffset RuleLocalTime,
        string? DateText,
        string PeriodKey,
        long SampleValue);
}
