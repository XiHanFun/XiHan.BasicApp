#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsStatus
// Guid:18b0808c-e187-4c91-bf4b-967ab52b06dd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 短信状态枚举
/// </summary>
public enum SmsStatus
{
    /// <summary>
    /// 待发送
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发送中
    /// </summary>
    Sending = 1,

    /// <summary>
    /// 发送成功
    /// </summary>
    Success = 2,

    /// <summary>
    /// 发送失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}
