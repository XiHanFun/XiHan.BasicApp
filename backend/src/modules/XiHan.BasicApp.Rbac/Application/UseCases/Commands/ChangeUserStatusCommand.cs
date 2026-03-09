#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChangeUserStatusCommand
// Guid:5f7b2bbd-30cb-4f63-97b9-a6f1e8ca58e5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/09 16:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.UseCases.Commands;

/// <summary>
/// 修改用户状态命令
/// </summary>
public class ChangeUserStatusCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "用户 ID 无效")]
    public long UserId { get; set; }

    /// <summary>
    /// 用户状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}
