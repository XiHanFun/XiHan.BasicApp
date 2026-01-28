#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessResult
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 访问结果枚举
/// </summary>
public enum AccessResult
{
    /// <summary>
    /// 成功
    /// </summary>
    Success = 0,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 1,

    /// <summary>
    /// 权限不足
    /// </summary>
    Forbidden = 2,

    /// <summary>
    /// 未授权
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// 资源不存在
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError = 5
}
