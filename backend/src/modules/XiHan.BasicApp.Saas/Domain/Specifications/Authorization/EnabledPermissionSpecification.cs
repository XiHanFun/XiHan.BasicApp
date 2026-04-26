#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnabledPermissionSpecification
// Guid:750a93d8-97a1-4c4f-9ecd-46630d799f7c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
