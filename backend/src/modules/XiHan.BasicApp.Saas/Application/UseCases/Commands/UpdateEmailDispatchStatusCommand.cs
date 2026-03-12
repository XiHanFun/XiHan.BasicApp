#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UpdateEmailDispatchStatusCommand
// Guid:c0f2d33d-bac8-4f89-bfd4-f7457bb6974b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 更新邮件发送状态命令
/// </summary>
public class UpdateEmailDispatchStatusCommand
{
    /// <summary>
    /// 邮件ID
    /// </summary>
    public long EmailId { get; set; }

    /// <summary>
    /// 是否发送成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（失败时）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 是否允许重试（失败时）
    /// </summary>
    public bool AllowRetry { get; set; } = true;
}
