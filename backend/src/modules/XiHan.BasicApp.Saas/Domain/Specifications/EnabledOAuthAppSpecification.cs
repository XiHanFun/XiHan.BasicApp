#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnabledOAuthAppSpecification.cs
// Guid:a5c9e3f6-0b4d-4a8c-f123-d7c0b9a4e6f8
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
/// OAuth 应用启用状态规约
/// </summary>
public class EnabledOAuthAppSpecification : Specification<SysOAuthApp>
{
    public override Expression<Func<SysOAuthApp, bool>> ToExpression()
    {
        return app => app.Status == YesOrNo.Yes;
    }
}
