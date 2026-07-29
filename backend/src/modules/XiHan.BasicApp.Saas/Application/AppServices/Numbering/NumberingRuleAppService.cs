// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 业务编号规则命令 Dynamic API，负责权限、平台边界、事务和操作人审计编排。
/// </summary>
/// <remarks>
/// 本层不重复实现规则不变量，而是先根据当前租户和权限确定可写范围，再在工作单元内调用领域服务。
/// 每个非取消异常都会记录结构化失败日志后原样抛出，统一异常中间件负责生成对外响应。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "业务编号规则")]
public sealed class NumberingRuleAppService : SaasApplicationService, INumberingRuleAppService
{
    /// <summary>
    /// 规则领域服务，集中维护格式冻结、安全重置、删除限制和作用域内唯一性等业务不变量。
    /// </summary>
    private readonly INumberingRuleDomainService _domainService;

    /// <summary>
    /// 当前租户上下文，用于把外部作用域请求约束为当前租户或平台范围，禁止调用方指定任意租户。
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 当前用户上下文，为规则敏感操作提供结构化审计中的操作人标识。
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 权限检查器，用于在方法级基础权限之外追加平台全局规则管理权限校验。
    /// </summary>
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 结构化日志记录器，记录规则命令的作用域、操作人、结果以及安全重置原因等审计字段。
    /// </summary>
    private readonly ILogger<NumberingRuleAppService> _logger;

    /// <summary>
    /// 初始化业务编号规则命令服务。
    /// </summary>
    /// <param name="domainService">规则领域服务。</param>
    /// <param name="currentTenant">当前租户上下文。</param>
    /// <param name="currentUser">当前用户。</param>
    /// <param name="permissionChecker">权限检查器。</param>
    /// <param name="logger">结构化日志记录器。</param>
    public NumberingRuleAppService(
        INumberingRuleDomainService domainService,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IPermissionChecker permissionChecker,
        ILogger<NumberingRuleAppService> logger)
    {
        _domainService = domainService;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    /// <summary>
    /// 在当前调用方可写的租户或平台作用域内创建编号规则。
    /// </summary>
    /// <param name="input">创建 DTO；规则归属由当前租户上下文和 <see cref="NumberingScope"/> 决定。</param>
    /// <param name="cancellationToken">用于取消权限校验、领域校验和持久化事务的取消令牌。</param>
    /// <returns>事务中已经持久化的规则详情。</returns>
    /// <remarks>
    /// 方法需要规则创建权限；平台创建全局规则时还需要全局管理权限。领域写入与框架审计字段处于同一个工作单元。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">作用域、权限、规则格式或编码唯一性校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Create)]
    public async Task<NumberingRuleDetailDto> CreateNumberingRuleAsync(NumberingRuleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        // 在调用领域服务前固定可写边界，避免平台/租户仅凭 DTO 中的 Scope 跨范围写入规则。
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.CreateAsync(NumberingApplicationMapper.ToCreateCommand(input), cancellationToken);
            LogCommandResult("Create", result.Rule.BasicId, result.Rule.RuleCode, input.Scope, true, null);
            return NumberingApplicationMapper.ToDetailDto(result.Rule);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 取消属于调用方主动终止，不记录为业务操作失败，避免审计和告警噪声。
            LogCommandResult("Create", 0, input.RuleCode, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 更新当前可写作用域中的编号规则。
    /// </summary>
    /// <param name="input">更新 DTO；主键必须属于当前租户作用域，首次发号后不得修改冻结格式字段。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则查询和持久化事务的取消令牌。</param>
    /// <returns>更新后的规则详情。</returns>
    /// <remarks>方法只负责命令编排；格式冻结、容量和全局开放标志归一由领域服务保证。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">作用域、权限、规则存在性、格式冻结或容量校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Update)]
    public async Task<NumberingRuleDetailDto> UpdateNumberingRuleAsync(NumberingRuleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.UpdateAsync(NumberingApplicationMapper.ToUpdateCommand(input), cancellationToken);
            LogCommandResult("Update", result.Rule.BasicId, result.Rule.RuleCode, input.Scope, true, null);
            return NumberingApplicationMapper.ToDetailDto(result.Rule);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCommandResult("Update", input.BasicId, null, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 启用或停用当前可写作用域中的编号规则。
    /// </summary>
    /// <param name="input">状态变更 DTO，可附加写入规则备注的说明。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则查询和持久化事务的取消令牌。</param>
    /// <returns>状态变更后的规则详情。</returns>
    /// <remarks>停用只阻止后续发号，不删除规则及其永久分配记录。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">作用域、权限、规则存在性或状态值校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Status)]
    public async Task<NumberingRuleDetailDto> UpdateNumberingRuleStatusAsync(NumberingRuleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            var result = await _domainService.UpdateStatusAsync(
                new NumberingRuleStatusChangeCommand(input.BasicId, input.Status, input.Remark), cancellationToken);
            LogCommandResult("Status", result.Rule.BasicId, result.Rule.RuleCode, input.Scope, true, null);
            return NumberingApplicationMapper.ToDetailDto(result.Rule);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCommandResult("Status", input.BasicId, null, input.Scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 在防重复约束下调整规则当前周期的下一流水值。
    /// </summary>
    /// <param name="input">安全重置 DTO；下一流水值使用字符串承载，原因必填，全局规则需要编码二次确认。</param>
    /// <param name="cancellationToken">用于取消权限校验、历史区间查询和持久化事务的取消令牌。</param>
    /// <returns>重置后的规则详情。</returns>
    /// <remarks>
    /// 该操作允许主动前移形成空洞，但领域服务会读取永久分配记录并禁止回退到可能重复的范围。
    /// 成功与失败日志都记录原因和目标流水，便于审计敏感操作。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="UserFriendlyException">流水字符串、原因、全局确认编码、权限或防重复校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Reset)]
    public async Task<NumberingRuleDetailDto> ResetNumberingRuleAsync(NumberingRuleResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        await EnsureWritableScopeAsync(input.Scope, cancellationToken);
        try
        {
            // API 使用字符串承载最多 18 位流水值，避免 JavaScript Number 在进入后端前丢失精度。
            if (!long.TryParse(input.NextValue, NumberStyles.None, CultureInfo.InvariantCulture, out var nextValue))
            {
                throw new UserFriendlyException("下一流水值必须是 1 至 18 位正整数。");
            }

            var result = await _domainService.ResetAsync(
                new NumberingRuleResetCommand(input.BasicId, nextValue, input.Reason, input.ConfirmRuleCode), cancellationToken);
            LogCommandResult(
                "Reset",
                result.Rule.BasicId,
                result.Rule.RuleCode,
                input.Scope,
                true,
                null,
                input.Reason.Trim(),
                $"NextValue={input.NextValue}");
            return NumberingApplicationMapper.ToDetailDto(result.Rule);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCommandResult(
                "Reset",
                input.BasicId,
                input.ConfirmRuleCode,
                input.Scope,
                false,
                exception,
                input.Reason?.Trim(),
                $"NextValue={input.NextValue}");
            throw;
        }
    }

    /// <summary>
    /// 删除当前可写作用域中从未发号的编号规则。
    /// </summary>
    /// <param name="id">规则主键，必须为正数。</param>
    /// <param name="scope">请求作用域；平台或单体上下文中的 <see cref="NumberingScope.Auto"/> 按全局规则处理。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则查询和删除事务的取消令牌。</param>
    /// <remarks>领域服务会拒绝删除已经发号的规则，以保留审计链和历史格式快照。</remarks>
    /// <exception cref="UserFriendlyException">作用域、权限、规则存在性、发号状态或删除结果校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Delete)]
    public async Task DeleteNumberingRuleAsync(
        long id,
        NumberingScope scope = NumberingScope.Auto,
        CancellationToken cancellationToken = default)
    {
        await EnsureWritableScopeAsync(scope, cancellationToken);
        try
        {
            await _domainService.DeleteAsync(id, cancellationToken);
            LogCommandResult("Delete", id, null, scope, true, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogCommandResult("Delete", id, null, scope, false, exception);
            throw;
        }
    }

    /// <summary>
    /// 校验命令作用域。租户只能维护私有规则；平台只能维护全局规则且必须额外拥有全局管理权限。
    /// </summary>
    /// <param name="scope">调用方请求写入的编号作用域。</param>
    /// <param name="cancellationToken">用于取消平台全局管理权限查询的取消令牌。</param>
    /// <returns>作用域和权限均满足时完成的任务。</returns>
    /// <exception cref="UserFriendlyException">作用域未定义、上下文与作用域冲突、用户未登录或缺少全局管理权限。</exception>
    /// <exception cref="OperationCanceledException">权限查询被 <paramref name="cancellationToken"/> 取消。</exception>
    private async Task EnsureWritableScopeAsync(NumberingScope scope, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new UserFriendlyException("编号作用域无效。");
        }

        var isPlatform = _currentTenant.IsPlatformOperation();
        if (!isPlatform && scope == NumberingScope.Global)
        {
            throw new UserFriendlyException("租户上下文只能维护租户私有编号规则。");
        }

        if (isPlatform && scope == NumberingScope.Tenant)
        {
            throw new UserFriendlyException("平台上下文不能维护未指定租户的私有编号规则。");
        }

        if (!isPlatform)
        {
            // 租户写操作的所属租户由当前上下文确定，不需要也不允许调用方额外提供租户标识。
            return;
        }

        // 平台规则影响所有租户的共享序列，因此在普通命令权限之外再执行平台专属权限检查。
        var userId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        if (!await _permissionChecker.IsGrantedAsync(userId.ToString(), SaasPermissionCodes.Numbering.GlobalManage, cancellationToken))
        {
            throw new UserFriendlyException("缺少全局编号规则管理权限。");
        }
    }

    /// <summary>
    /// 记录规则写操作结果，包含操作人、请求租户、规则和异常结果。
    /// </summary>
    /// <param name="operation">稳定的操作名称，用于日志检索与聚合。</param>
    /// <param name="ruleId">规则主键；创建失败且尚未生成主键时为 0。</param>
    /// <param name="ruleCode">已知规则编码；加载规则前失败时可以为空。</param>
    /// <param name="scope">调用方请求的规则作用域。</param>
    /// <param name="success">操作是否成功完成。</param>
    /// <param name="exception">失败异常；成功时为空。</param>
    /// <param name="reason">安全重置等敏感操作的业务原因。</param>
    /// <param name="serialRange">敏感操作涉及的关键流水信息。</param>
    private void LogCommandResult(
        string operation,
        long ruleId,
        string? ruleCode,
        NumberingScope scope,
        bool success,
        Exception? exception,
        string? reason = null,
        string? serialRange = null)
    {
        var userId = _currentUser.UserId;
        var requestTenantId = _currentTenant.Id ?? 0;
        if (success)
        {
            // 使用结构化占位符而不是字符串拼接，便于按租户、操作人和规则建立审计查询。
            _logger.LogInformation(
                "业务编号规则操作成功：Operation={Operation}, RuleId={RuleId}, RuleCode={RuleCode}, Scope={Scope}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Reason={Reason}, SerialRange={SerialRange}, Result=Success",
                operation, ruleId, ruleCode, scope, requestTenantId, userId, reason, serialRange);
            return;
        }

        _logger.LogWarning(
            exception,
            "业务编号规则操作失败：Operation={Operation}, RuleId={RuleId}, RuleCode={RuleCode}, Scope={Scope}, RequestTenantId={RequestTenantId}, OperatorId={OperatorId}, Reason={Reason}, SerialRange={SerialRange}, Result=Failed",
            operation, ruleId, ruleCode, scope, requestTenantId, userId, reason, serialRange);
    }
}
