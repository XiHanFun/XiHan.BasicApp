// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 业务编号规则命令应用服务契约。
/// </summary>
public interface INumberingRuleAppService : IApplicationService
{
    /// <summary>
    /// 在当前调用上下文允许写入的作用域中创建业务编号规则。
    /// </summary>
    /// <param name="input">创建参数；不包含租户 ID，规则归属由当前租户上下文和作用域共同确定。</param>
    /// <param name="cancellationToken">用于取消权限校验和持久化操作的取消令牌。</param>
    /// <returns>已经持久化的规则详情，包括数据库生成的主键和初始流水状态。</returns>
    /// <remarks>
    /// 租户上下文只能创建租户私有规则；平台上下文只能创建全局规则，并且需要全局编号管理权限。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域、权限、格式配置或规则编码唯一性校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleDetailDto> CreateNumberingRuleAsync(NumberingRuleCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前作用域中的业务编号规则配置。
    /// </summary>
    /// <param name="input">更新参数；规则主键必须属于当前可写作用域。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则加载和持久化操作的取消令牌。</param>
    /// <returns>更新后的规则详情。</returns>
    /// <remarks>
    /// 规则首次发号后，影响编号唯一性的格式字段会被冻结；本方法仍允许修改名称、备注、排序、状态外配置及全局开放状态。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不存在、无权操作、格式无效或修改了已冻结字段。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleDetailDto> UpdateNumberingRuleAsync(NumberingRuleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用或停用当前作用域中的业务编号规则。
    /// </summary>
    /// <param name="input">状态变更参数，可同时附加操作备注。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则加载和持久化操作的取消令牌。</param>
    /// <returns>状态变更后的规则详情。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域无效、规则不存在、无权操作或状态值无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleDetailDto> UpdateNumberingRuleStatusAsync(NumberingRuleStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 安全调整规则在当前周期中的下一流水值。
    /// </summary>
    /// <param name="input">重置参数；原因必填，全局规则还必须提供匹配的规则编码作为二次确认。</param>
    /// <param name="cancellationToken">用于取消权限校验、审计区间查询和持久化操作的取消令牌。</param>
    /// <returns>安全重置后的规则详情。</returns>
    /// <remarks>
    /// 允许向前跳号形成空洞，但禁止回退到当前周期已经分配过的流水范围；流水边界以字符串传输以避免前端精度损失。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">下一流水值、重置原因、全局确认编码或防重复区间校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleDetailDto> ResetNumberingRuleAsync(NumberingRuleResetDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除当前作用域中从未发号的业务编号规则。
    /// </summary>
    /// <param name="id">待删除规则的主键，必须为正数。</param>
    /// <param name="scope">规则作用域；平台上下文中的 <see cref="NumberingScope.Auto"/> 按全局规则处理。</param>
    /// <param name="cancellationToken">用于取消权限校验、规则加载和删除操作的取消令牌。</param>
    /// <remarks>已经产生分配记录的规则必须停用并保留，不能通过本方法破坏发号审计链。</remarks>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域无效、规则不存在、无权操作、已经发号或删除失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task DeleteNumberingRuleAsync(long id, NumberingScope scope = NumberingScope.Auto, CancellationToken cancellationToken = default);
}

/// <summary>
/// 业务编号规则查询应用服务契约。
/// </summary>
public interface INumberingRuleQueryService : IApplicationService
{
    /// <summary>
    /// 按当前租户上下文和请求作用域分页查询业务编号规则。
    /// </summary>
    /// <param name="input">分页、关键字、状态、过滤和排序参数。</param>
    /// <param name="cancellationToken">用于取消字段安全校验和数据库查询的取消令牌。</param>
    /// <returns>经过作用域隔离和字段安全处理的规则分页结果。</returns>
    /// <remarks>租户请求的全局作用域只返回已启用且允许租户使用的规则；调用方过滤条件不能覆盖内部租户条件。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">作用域无效或平台上下文请求了未指定租户的私有规则。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<PageResultDtoBase<NumberingRuleListItemDto>> GetNumberingRulePageAsync(NumberingRulePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询当前租户可调用的全局业务编号规则。
    /// </summary>
    /// <param name="input">分页、关键字、状态、过滤和排序参数。</param>
    /// <param name="cancellationToken">用于取消字段安全校验和平台库查询的取消令牌。</param>
    /// <returns>平台库中已启用且向租户开放的全局规则分页结果。</returns>
    /// <remarks>租户调用时会在平台上下文内完成查询，方法返回后自动恢复原租户上下文。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<PageResultDtoBase<NumberingRuleListItemDto>> GetAvailableGlobalNumberingRulePageAsync(NumberingRulePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询当前可见作用域中的业务编号规则详情。
    /// </summary>
    /// <param name="id">规则主键，必须为正数。</param>
    /// <param name="scope">规则作用域；租户使用 <see cref="NumberingScope.Global"/> 时会切换到平台库。</param>
    /// <param name="cancellationToken">用于取消规则查询的取消令牌。</param>
    /// <returns>规则详情；当前作用域中不存在对应规则时返回 <see langword="null"/>。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">主键或作用域无效，或者租户尝试读取未开放的全局规则。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleDetailDto?> GetNumberingRuleDetailAsync(long id, NumberingScope scope = NumberingScope.Auto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询指定规则的永久发号记录。
    /// </summary>
    /// <param name="input">规则主键、作用域以及分页过滤参数。</param>
    /// <param name="cancellationToken">用于取消字段安全校验、规则校验和记录查询的取消令牌。</param>
    /// <returns>发号记录分页；每项使用格式快照重建首尾编号。</returns>
    /// <remarks>租户查看共享全局规则时只能看到由本租户请求产生的记录，不能读取其他租户的幂等键和业务标识。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则主键、作用域、规则存在性或全局开放状态校验失败。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<PageResultDtoBase<NumberingAllocationListItemDto>> GetNumberingAllocationPageAsync(NumberingAllocationPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前后端运行环境允许新规则保存的可移植时区选项。
    /// </summary>
    /// <param name="cancellationToken">在读取缓存目录前检查的取消令牌。</param>
    /// <returns>以 IANA ID 为保存值、按 UTC 偏移稳定排序的只读时区列表。</returns>
    /// <remarks>目录排除无法在 Windows 与 Unix 之间安全映射的时区；历史 Windows ID 仍由格式器兼容解析。</remarks>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<IReadOnlyList<NumberingTimeZoneOptionDto>> GetNumberingTimeZoneOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用指定格式参数预览编号，但不读取或推进任何规则流水。
    /// </summary>
    /// <param name="input">待验证的格式参数和示例流水值。</param>
    /// <param name="cancellationToken">在执行纯计算前检查的取消令牌。</param>
    /// <returns>示例编号、规则时区本地时间和对应周期键。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">格式组合、时区、流水位数或示例流水值无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingPreviewResultDto> PreviewNumberingFormatAsync(NumberingPreviewDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从指定示例流水值开始连续预览多个编号，但不读取或推进任何规则流水。
    /// </summary>
    /// <param name="input">格式参数、示例起始流水值和 1 至 50 的连续预览数量。</param>
    /// <param name="cancellationToken">在执行纯计算前检查的取消令牌。</param>
    /// <returns>连续编号列表、流水区间、规则时区本地时间和对应周期键。</returns>
    /// <remarks>该接口与单个预览共享格式校验路径，且不会创建工作单元或永久分配记录。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">数量、格式组合、时区、流水位数、起始流水或区间容量无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingBatchPreviewResultDto> PreviewNumberingBatchAsync(
        NumberingBatchPreviewDto input,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 业务编号 Dynamic API 契约。
/// </summary>
public interface INumberingAppService : IApplicationService
{
    /// <summary>
    /// 通过受权 Dynamic API 生成一个业务编号。
    /// </summary>
    /// <param name="input">规则编码、解析作用域、幂等键和可选业务关联信息；禁止传入任意租户 ID。</param>
    /// <param name="cancellationToken">用于取消规则解析、并发等待和数据库事务的取消令牌。</param>
    /// <returns>实际规则、流水区间、完整编号以及是否为幂等重放的结果。</returns>
    /// <remarks>相同租户、实际规则和幂等键的相同请求返回首次分配结果，不会再次消耗流水。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求、作用域、规则状态、幂等性、容量或并发重试校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResultDto> GenerateNumberAsync(NumberGenerateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过受权 Dynamic API 在一个原子分配中生成连续的业务编号。
    /// </summary>
    /// <param name="input">批量请求，数量必须在 1 至 1000 之间且幂等键必填。</param>
    /// <param name="cancellationToken">用于取消规则解析、并发等待和数据库事务的取消令牌。</param>
    /// <returns>连续流水区间、完整编号列表以及是否为幂等重放的结果。</returns>
    /// <remarks>整批请求共享一个幂等记录，成功时一次推进完整区间，失败时事务不会推进流水。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求、批量上限、作用域、规则状态、幂等性、容量或并发重试校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResultDto> GenerateNumberBatchAsync(NumberBatchGenerateDto input, CancellationToken cancellationToken = default);
}
