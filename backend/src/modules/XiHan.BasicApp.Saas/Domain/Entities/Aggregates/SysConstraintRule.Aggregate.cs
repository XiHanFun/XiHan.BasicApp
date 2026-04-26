#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRule.Aggregate
// Guid:a1b2c3d4-5e6f-7890-abcd-ef12345678d2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
