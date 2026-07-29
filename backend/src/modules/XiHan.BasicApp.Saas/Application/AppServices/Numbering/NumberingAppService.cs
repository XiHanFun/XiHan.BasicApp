// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 业务编号生成 Dynamic API；只暴露受权服务调用，不在管理页面提供真实发号按钮。
/// </summary>
/// <remarks>
/// 本层只负责权限入口、外部 DTO 到公共 DI 请求的映射以及结果 DTO 转换。
/// 作用域解析、幂等、事务和并发正确性统一由 <see cref="INumberGenerator"/> 实现，避免 API 与内部调用出现两套发号语义。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "业务编号生成")]
public sealed class NumberingAppService : SaasApplicationService, INumberingAppService
{
    /// <summary>
    /// 统一编号生成器；API 层不自行实现作用域、事务或幂等逻辑，确保内部 DI 调用与外部接口语义一致。
    /// </summary>
    private readonly INumberGenerator _numberGenerator;

    /// <summary>
    /// 初始化业务编号生成 API。
    /// </summary>
    /// <param name="numberGenerator">DI 编号生成器。</param>
    public NumberingAppService(INumberGenerator numberGenerator)
    {
        _numberGenerator = numberGenerator;
    }

    /// <summary>
    /// 将单号 Dynamic API 请求交给统一编号生成器执行。
    /// </summary>
    /// <param name="input">单号请求 DTO；规则编码和幂等键必填，且不允许携带租户 ID。</param>
    /// <param name="cancellationToken">用于取消规则解析、并发等待和发号事务的取消令牌。</param>
    /// <returns>包含实际规则、完整编号、流水区间和幂等重放标记的 API 结果。</returns>
    /// <remarks>调用方必须拥有 <c>saas:numbering:generate</c> 权限；所有成功或失败日志由生成器按原请求租户记录。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求、规则、幂等、容量或并发校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Generate)]
    public async Task<NumberGenerationResultDto> GenerateNumberAsync(NumberGenerateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        // 外部 DTO 不包含租户 ID；生成器只能从可信的当前租户上下文确定请求租户，防止越权代发号。
        var result = await _numberGenerator.GenerateAsync(
            new NumberGenerateRequest(input.RuleCode, input.Scope, input.IdempotencyKey, input.BusinessType, input.BusinessId),
            cancellationToken);
        return NumberingApplicationMapper.ToGenerationResultDto(result);
    }

    /// <summary>
    /// 将批量 Dynamic API 请求作为一个连续区间交给统一编号生成器执行。
    /// </summary>
    /// <param name="input">批量请求 DTO；数量必须在 1 至 1000 之间，规则编码和幂等键必填。</param>
    /// <param name="cancellationToken">用于取消规则解析、并发等待和发号事务的取消令牌。</param>
    /// <returns>包含连续编号列表、流水区间和幂等重放标记的 API 结果。</returns>
    /// <remarks>整批请求共享一条永久分配记录，失败时不会部分推进流水。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求、批量数量、规则、幂等、容量或并发校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    [PermissionAuthorize(SaasPermissionCodes.Numbering.Generate)]
    public async Task<NumberGenerationResultDto> GenerateNumberBatchAsync(NumberBatchGenerateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        // 单号与批量请求都进入同一核心流程，确保作用域、幂等和事务规则不会随入口不同而漂移。
        var result = await _numberGenerator.GenerateBatchAsync(
            new NumberBatchGenerateRequest(input.RuleCode, input.Scope, input.IdempotencyKey, input.Count, input.BusinessType, input.BusinessId),
            cancellationToken);
        return NumberingApplicationMapper.ToGenerationResultDto(result);
    }
}
