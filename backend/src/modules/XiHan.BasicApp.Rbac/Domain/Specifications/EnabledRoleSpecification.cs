#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnabledRoleSpecification
// Guid:75ad5da4-83bc-4cfe-b252-9406b019e26b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:38:04
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Rbac.Domain.Specifications;

/// <summary>
/// 启用角色规约
/// </summary>
public sealed class EnabledRoleSpecification : Specification<SysRole>
{
    /// <summary>
    /// 启用角色规约表达式
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<SysRole, bool>> ToExpression()
    {
        return role => role.Status == YesOrNo.Yes;
    }
}
