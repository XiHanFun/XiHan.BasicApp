#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ActiveUserSpecification
// Guid:010eef6a-142c-4904-8c16-38df58e5057f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:53
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Rbac.Domain.Specifications;

/// <summary>
/// 活跃用户规约
/// </summary>
public sealed class ActiveUserSpecification : Specification<SysUser>
{
    /// <summary>
    /// 活跃用户规约表达式
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<SysUser, bool>> ToExpression()
    {
        return user => user.Status == YesOrNo.Yes;
    }
}
