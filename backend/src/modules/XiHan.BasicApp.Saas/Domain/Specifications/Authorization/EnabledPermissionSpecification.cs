// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 启用权限规约
/// </summary>
public sealed class EnabledPermissionSpecification : Specification<SysPermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysPermission, bool>> ToExpression()
    {
        return permission => !permission.IsDeleted && permission.Status == EnableStatus.Enabled;
    }
}
