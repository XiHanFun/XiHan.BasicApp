#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnabledRoleSpecification
// Guid:b40f5ce5-f126-4217-a40b-346fe5a59a6b
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
/// 启用角色规约
/// </summary>
public sealed class EnabledRoleSpecification : Specification<SysRole>
{
    /// <inheritdoc />
    public override Expression<Func<SysRole, bool>> ToExpression()
    {
        return role => !role.IsDeleted && role.Status == EnableStatus.Enabled;
    }
}
