#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceType
// Guid:4b55e79a-c90c-4472-a6b2-280d71f6e504
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 资源类型枚举
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// 菜单资源（目录、菜单项）
    /// </summary>
    Menu = 0,

    /// <summary>
    /// API接口资源
    /// </summary>
    Api = 1,

    /// <summary>
    /// 按钮资源（页面操作按钮）
    /// </summary>
    Button = 2,

    /// <summary>
    /// 文件资源
    /// </summary>
    File = 3,

    /// <summary>
    /// 数据表资源
    /// </summary>
    DataTable = 4,

    /// <summary>
    /// 页面元素资源（Tab、区块等）
    /// </summary>
    Element = 5,

    /// <summary>
    /// 业务对象资源
    /// </summary>
    BusinessObject = 6,

    /// <summary>
    /// 其他资源
    /// </summary>
    Other = 99
}
