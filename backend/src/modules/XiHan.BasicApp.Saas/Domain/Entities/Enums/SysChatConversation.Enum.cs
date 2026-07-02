#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysChatConversation.Enum
// Guid:9a3b2c5d-7c4f-4b6a-8d9e-2f3a4b5c6d7e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 聊天会话类型
/// </summary>
public enum ChatConversationType
{
    /// <summary>
    /// 单聊（两人私聊，PairKey 唯一定位）
    /// </summary>
    [Description("单聊")]
    Single = 1,

    /// <summary>
    /// 群聊（自建群，含群主与成员管理）
    /// </summary>
    [Description("群聊")]
    Group = 2,

    /// <summary>
    /// 部门群（按部门自动建群，成员随部门归属同步）
    /// </summary>
    [Description("部门群")]
    Department = 3
}

/// <summary>
/// 聊天会话成员角色
/// </summary>
public enum ChatMemberRole
{
    /// <summary>
    /// 群主（解散群、移交群主、全量成员管理）
    /// </summary>
    [Description("群主")]
    Owner = 1,

    /// <summary>
    /// 管理员（成员管理）
    /// </summary>
    [Description("管理员")]
    Admin = 2,

    /// <summary>
    /// 普通成员
    /// </summary>
    [Description("成员")]
    Member = 3
}
