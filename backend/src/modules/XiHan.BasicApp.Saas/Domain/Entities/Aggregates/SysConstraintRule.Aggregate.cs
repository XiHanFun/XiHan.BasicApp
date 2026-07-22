// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 约束规则领域行为
/// </summary>
public partial class SysConstraintRule
{
    /// <summary>
    /// 启用规则
    /// </summary>
    public void Enable()
    {
        Status = EnableStatus.Enabled;
    }

    /// <summary>
    /// 停用规则
    /// </summary>
    public void Disable()
    {
        Status = EnableStatus.Disabled;
    }
}
