#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysFieldLevelSecurity.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

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
    Permission = 2,

    /// <summary>
    /// 部门：策略应用于该部门下的所有用户（适用于"某部门可见/不可见某字段"场景）
    /// </summary>
    Department = 3
}

/// <summary>
/// 字段脱敏策略枚举
/// 当 IsReadable=false 时生效，控制字段返回形式
/// </summary>
public enum FieldMaskStrategy
{
    /// <summary>
    /// 不脱敏（IsReadable=true 时使用，原值返回）
    /// </summary>
    None = 0,

    /// <summary>
    /// 完全隐藏：字段返回 null 或从响应中移除
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// 全部星号：如 123456 → ******
    /// </summary>
    FullMask = 2,

    /// <summary>
    /// 部分脱敏：保留首尾字符，中间星号（如手机号 138****1234）
    /// 具体规则由 MaskPattern 字段描述
    /// </summary>
    PartialMask = 3,

    /// <summary>
    /// 哈希：返回字段值的 Hash（如 SHA256），不可逆
    /// </summary>
    Hash = 4,

    /// <summary>
    /// 固定替换：返回固定占位符（如 [已脱敏]）
    /// 占位符由 MaskPattern 字段指定
    /// </summary>
    Redact = 5,

    /// <summary>
    /// 自定义：由 MaskPattern 字段描述规则，应用层按自定义逻辑处理
    /// </summary>
    Custom = 99
}

