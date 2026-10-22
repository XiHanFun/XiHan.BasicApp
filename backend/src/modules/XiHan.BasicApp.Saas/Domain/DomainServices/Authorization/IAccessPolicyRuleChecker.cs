// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 访问策略条件集的求值规则校验：保存前确认每条条件生效后能正常求值
/// </summary>
public interface IAccessPolicyRuleChecker
{
    /// <summary>
    /// 校验条件集，不通过时抛出 <see cref="InvalidOperationException"/> 说明原因
    /// </summary>
    void Validate(IReadOnlyList<AccessPolicyConditionCommand> conditions);
}
