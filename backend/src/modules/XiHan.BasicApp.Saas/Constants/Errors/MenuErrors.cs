#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuErrors
// Guid:bc5af7ad-a4cf-4ad8-b9ea-f56a7b8c9d0e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 菜单模块错误消息
/// </summary>
public static class MenuErrors
{
    /// <summary>
    /// 菜单不存在
    /// </summary>
    public const string NotFound = "菜单不存在";

    /// <summary>
    /// 菜单编码已存在
    /// </summary>
    public const string CodeExists = "菜单编码已存在";

    /// <summary>
    /// 菜单有子菜单，无法删除
    /// </summary>
    public const string HasChildren = "菜单有子菜单，无法删除";
}
