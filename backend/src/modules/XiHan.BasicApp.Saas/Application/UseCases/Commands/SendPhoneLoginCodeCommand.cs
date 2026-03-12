#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SendPhoneLoginCodeCommand
// Guid:743d487e-89a8-433a-bb6d-5409f180aaf7
// Author:zhaifanhua
// CreateTime:2026/03/13 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 发送手机登录验证码命令
/// </summary>
public class SendPhoneLoginCodeCommand
{
    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "手机号格式不正确")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; } = 1;
}
