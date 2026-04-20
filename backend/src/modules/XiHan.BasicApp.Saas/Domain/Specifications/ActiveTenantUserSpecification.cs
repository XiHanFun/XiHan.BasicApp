#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ActiveTenantUserSpecification.cs
// Guid:d2f6b0c3-7e1a-4d5f-c890-a4f7e6d1b3c5
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
/// 租户用户有效性规约
/// </summary>
public class ActiveTenantUserSpecification : Specification<SysTenantUser>
{
    public override Expression<Func<SysTenantUser, bool>> ToExpression()
    {
        return tenantUser => tenantUser.Status == YesOrNo.Yes;
    }
}
