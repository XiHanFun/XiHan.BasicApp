#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UpdateSmsDispatchStatusCommand
// Guid:0cbf9eac-f639-4f0d-986b-764139f6f95f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:31:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 更新短信发送状态命令
/// </summary>
public class UpdateSmsDispatchStatusCommand
{
    /// <summary>
    /// 短信ID
    /// </summary>
    public long SmsId { get; set; }

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
