// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 业务编号规则领域服务，负责规则不变量、格式冻结和安全重置策略。
/// </summary>
/// <remarks>
/// 规则归属完全取自当前租户上下文：平台上下文操作 <c>TenantId = 0</c> 的全局规则，租户上下文操作本租户私有规则。
/// 权限和平台专属操作校验由上层应用服务负责，本接口专注于领域一致性并依赖调用方提供写事务。
/// </remarks>
public interface INumberingRuleDomainService
{
    /// <summary>
    /// 在当前数据库与租户上下文中创建规则。
    /// </summary>
    /// <param name="command">创建命令；规则编码、名称、格式配置和时区必须满足领域约束。</param>
    /// <param name="cancellationToken">用于取消唯一性查询和持久化操作的取消令牌。</param>
    /// <returns>包含已持久化规则实体的命令结果。</returns>
    /// <remarks>租户私有规则的“允许租户使用”标志会被强制归一为 <see langword="false"/>，避免产生跨租户开放的错误语义。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则参数或唯一性校验失败。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleCommandResult> CreateAsync(NumberingRuleCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前作用域中的规则。
    /// </summary>
    /// <param name="command">更新命令；主键必须属于当前租户作用域。</param>
    /// <param name="cancellationToken">用于取消规则查询和持久化操作的取消令牌。</param>
    /// <returns>包含更新后规则实体的命令结果。</returns>
    /// <remarks>首次发号后会冻结所有影响编号唯一性的格式字段；未发号但已预留流水的规则也不能缩小到无法容纳当前值的位数。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不存在、越权或违反格式冻结策略。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleCommandResult> UpdateAsync(NumberingRuleUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前作用域中的规则状态。
    /// </summary>
    /// <param name="command">状态命令；可携带用于补充规则备注的说明。</param>
    /// <param name="cancellationToken">用于取消规则查询和持久化操作的取消令牌。</param>
    /// <returns>包含状态变更后规则实体的命令结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不存在或状态无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleCommandResult> UpdateStatusAsync(NumberingRuleStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 安全设置下一流水值。
    /// </summary>
    /// <param name="command">重置命令；原因必填，全局规则必须提供精确匹配的规则编码。</param>
    /// <param name="cancellationToken">用于取消规则查询、最大已分配值查询和持久化操作的取消令牌。</param>
    /// <returns>包含重置后规则实体的命令结果。</returns>
    /// <remarks>
    /// <c>NextValue</c> 表示下一次应发出的流水值，实体保存的是 <c>NextValue - 1</c>。
    /// 方法允许前移形成空洞，但会使用永久分配记录阻止回退到当前周期可能重复的范围。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">可能导致重复编号或确认信息无效。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberingRuleCommandResult> ResetAsync(NumberingRuleResetCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除从未发号的规则。
    /// </summary>
    /// <param name="id">规则主键，必须为正数且属于当前租户作用域。</param>
    /// <param name="cancellationToken">用于取消规则查询和删除操作的取消令牌。</param>
    /// <remarks>已经发号的规则必须保留以维持永久审计链，只能停用，不能删除。</remarks>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不存在或已经发号。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
