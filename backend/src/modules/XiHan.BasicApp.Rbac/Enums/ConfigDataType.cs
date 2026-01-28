#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigDataType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 配置数据类型枚举
/// </summary>
public enum ConfigDataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String = 0,

    /// <summary>
    /// 数字
    /// </summary>
    Number = 1,

    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// JSON对象
    /// </summary>
    Json = 3,

    /// <summary>
    /// 数组
    /// </summary>
    Array = 4
}
