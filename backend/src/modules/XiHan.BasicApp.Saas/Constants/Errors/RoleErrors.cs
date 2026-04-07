#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleErrors
// Guid:fa3ed5eb-e2ad-4eb6-f7c8-d34e5f6a7b8c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 角色模块错误消息
/// </summary>
public static class RoleErrors
{
    /// <summary>
    /// 角色不存在
    /// </summary>
    public const string NotFound = "角色不存在";

    /// <summary>
    /// 角色编码已存在
    /// </summary>
    public const string CodeExists = "角色编码已存在";

    /// <summary>
    /// 角色已分配给用户，无法删除
    /// </summary>
    public const string HasUsers = "角色已分配给用户，无法删除";
}
