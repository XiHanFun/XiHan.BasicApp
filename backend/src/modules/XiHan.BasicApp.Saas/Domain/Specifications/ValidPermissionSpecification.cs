#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ValidPermissionSpecification.cs
// Guid:e3a7c1d4-8f2b-4e6a-d901-b5a8f7e2c4d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 权限有效性规约
/// </summary>
public class ValidPermissionSpecification : Specification<SysPermission>
{
    public override Expression<Func<SysPermission, bool>> ToExpression()
    {
        return permission => permission.Status == YesOrNo.Yes;
    }
}
