#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthApp.Aggregate.cs
// Guid:b4d8f2a5-9c3e-4b7f-a012-e6d9c8b3f5a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// OAuth 应用聚合领域行为
/// </summary>
public partial class SysOAuthApp
{
    /// <summary>
    /// 启用应用
    /// </summary>
    public void Enable()
    {
        Status = EnableStatus.Enabled;
        AddLocalEvent(new OAuthChangedDomainEvent(BasicId));
    }

    /// <summary>
    /// 禁用应用
    /// </summary>
    public void Disable()
    {
        Status = EnableStatus.Disabled;
        AddLocalEvent(new OAuthChangedDomainEvent(BasicId));
    }

    /// <summary>
    /// 重置客户端密钥
    /// </summary>
    public void RegenerateSecret(string newSecret)
    {
        ClientSecret = newSecret;
        AddLocalEvent(new OAuthChangedDomainEvent(BasicId));
    }
}
