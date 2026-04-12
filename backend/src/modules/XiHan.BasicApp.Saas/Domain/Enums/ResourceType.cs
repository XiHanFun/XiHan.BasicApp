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

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 资源类型枚举
/// 资源是"被控制对象"（API/数据/文件等），不包含 UI 结构（菜单/按钮在 SysMenu 中维护）
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// API接口资源
    /// </summary>
    Api = 0,

    /// <summary>
    /// 文件资源
    /// </summary>
    File = 1,

    /// <summary>
    /// 数据表资源
    /// </summary>
    DataTable = 2,

    /// <summary>
    /// 业务对象资源
    /// </summary>
    BusinessObject = 3,

    /// <summary>
    /// 其他资源
    /// </summary>
    Other = 99
}
