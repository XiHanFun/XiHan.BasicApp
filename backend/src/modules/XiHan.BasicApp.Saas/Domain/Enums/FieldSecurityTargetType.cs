#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldSecurityTargetType
// Guid:a7b8c9d0-1234-5678-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 字段级安全目标类型枚举
/// 定义 FLS 策略绑定的主体类型
/// </summary>
public enum FieldSecurityTargetType
{
    /// <summary>
    /// 角色：策略应用于该角色下的所有用户
    /// </summary>
    Role = 0,

    /// <summary>
    /// 用户：策略直接应用于指定用户（优先级高于角色）
    /// </summary>
    User = 1,

    /// <summary>
    /// 权限：策略随权限一起生效（用户拥有该权限时受限）
    /// </summary>
    Permission = 2
}
