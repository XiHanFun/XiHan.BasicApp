#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 操作类型枚举
/// </summary>
public enum OperationType
{
    /// <summary>
    /// 登录
    /// </summary>
    Login = 0,

    /// <summary>
    /// 登出
    /// </summary>
    Logout = 1,

    /// <summary>
    /// 查询
    /// </summary>
    Query = 2,

    /// <summary>
    /// 新增
    /// </summary>
    Create = 3,

    /// <summary>
    /// 修改
    /// </summary>
    Update = 4,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 7,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}
