#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentErrors
// Guid:cd6ba8be-b5da-4be9-cafb-a67b8c9d0e1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 部门模块错误消息
/// </summary>
public static class DepartmentErrors
{
    /// <summary>
    /// 部门不存在
    /// </summary>
    public const string NotFound = "部门不存在";

    /// <summary>
    /// 部门编码已存在
    /// </summary>
    public const string CodeExists = "部门编码已存在";

    /// <summary>
    /// 部门有子部门，无法删除
    /// </summary>
    public const string HasChildren = "部门有子部门，无法删除";

    /// <summary>
    /// 部门有用户，无法删除
    /// </summary>
    public const string HasUsers = "部门有用户，无法删除";
}
