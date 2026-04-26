#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSession.Aggregate
// Guid:d6f0b4c7-1e5a-4d9b-c234-a8f1e0d5b7c9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/20 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 用户会话聚合领域行为
/// </summary>
public partial class SysUserSession
{
    /// <summary>
    /// 撤销会话
    /// </summary>
    public void Revoke(string reason)
    {
        IsOnline = false;
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        LogoutTime = DateTimeOffset.UtcNow;
        RevokedReason = reason;
    }

    /// <summary>
    /// 标记下线
    /// </summary>
    public void MarkOffline()
    {
        IsOnline = false;
        LogoutTime = DateTimeOffset.UtcNow;
    }
}
