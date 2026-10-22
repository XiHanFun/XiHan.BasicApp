// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限条件的求值规则校验：保存、启用前确认条件生效后能正常求值
/// </summary>
public interface IPermissionConditionRuleChecker
{
    /// <summary>
    /// 校验一条条件，不通过时抛出 <see cref="InvalidOperationException"/> 说明原因
    /// </summary>
    Task ValidateAsync(
        long? rolePermissionId,
        long? userPermissionId,
        string attributeName,
        ConditionOperator conditionOperator,
        bool isNegated,
        ConfigDataType valueType,
        string conditionValue,
        CancellationToken cancellationToken = default);
}
