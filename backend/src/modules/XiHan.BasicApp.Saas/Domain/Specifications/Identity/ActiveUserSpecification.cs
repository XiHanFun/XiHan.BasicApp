#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ActiveUserSpecification
// Guid:9dbb26b8-36da-4a3d-8bc2-13cb47a2a5be
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
