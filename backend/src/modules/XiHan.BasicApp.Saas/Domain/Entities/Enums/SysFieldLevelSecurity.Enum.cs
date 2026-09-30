// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 字段读取方式（脱敏策略）
/// </summary>
/// <remarks>
/// 严格程度按泄露的信息量从多到少：部分脱敏（泄露片段）&lt; 哈希（泄露是否相等）&lt; 全部星号（泄露长度）
/// &lt; 固定文本（只泄露有没有值）&lt; 隐藏（什么都不泄露）。多条规则命中同一字段时取最严的一种。
/// 除明文与隐藏外，其余方式只对文本字段有效。
/// </remarks>
public enum FieldMaskStrategy
{
    /// <summary>
    /// 明文：原值返回（配合只读使用）
    /// </summary>
    [Description("不脱敏")]
    None = 0,

    /// <summary>
    /// 隐藏：返回空值
    /// </summary>
    [Description("完全隐藏")]
    Hidden = 1,

    /// <summary>
    /// 全部星号：按原长度替换为 *
    /// </summary>
    [Description("全部星号")]
    FullMask = 2,

    /// <summary>
    /// 部分脱敏：保留前几位、后几位，中间替换为 *
    /// </summary>
    [Description("部分脱敏")]
    PartialMask = 3,

    /// <summary>
    /// 哈希：返回 SHA-256 前 16 位十六进制，可比较是否相同但看不到原值
    /// </summary>
    [Description("哈希")]
    Hash = 4,

    /// <summary>
    /// 固定文本：有值时一律显示规则里的固定文本
    /// </summary>
    [Description("固定替换")]
    Redact = 5
}

/// <summary>
/// 字段级安全规则的目标类型
/// </summary>
public enum FieldSecurityTargetType
{
    /// <summary>
    /// 角色：当前用户生效的角色
    /// </summary>
    [Description("角色")]
    Role = 0,

    /// <summary>
    /// 用户：指定用户本人
    /// </summary>
    [Description("用户")]
    User = 1,

    /// <summary>
    /// 部门：部门及其下级部门的有效成员
    /// </summary>
    [Description("部门")]
    Department = 3
}
