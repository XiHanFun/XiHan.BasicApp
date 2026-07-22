// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 启用用户规约
/// </summary>
public sealed class ActiveUserSpecification : Specification<SysUser>
{
    /// <inheritdoc />
    public override Expression<Func<SysUser, bool>> ToExpression()
    {
        return user => !user.IsDeleted && user.Status == EnableStatus.Enabled;
    }
}
