#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldSecurityService
// Guid:f5b6789c-3547-4384-b012-a1c562193bf1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 合并后的有效字段规则（按当前用户 + 角色，deny-overrides）
/// </summary>
public sealed class EffectiveFieldRule
{
    /// <summary>
    /// 字段名（对应实体/DTO 属性名）
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// 是否可读（任一命中规则不可读即不可读）
    /// </summary>
    public bool IsReadable { get; init; } = true;

    /// <summary>
    /// 是否可编辑（任一命中规则不可编辑即不可编辑）
    /// </summary>
    public bool IsEditable { get; init; } = true;

    /// <summary>
    /// 脱敏策略（取最严）
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; init; }

    /// <summary>
    /// 脱敏规则描述
    /// </summary>
    public string? MaskPattern { get; init; }
}

/// <summary>
/// 字段级安全（FLS）服务端落地：读脱敏 + 写校验。
/// 解析当前用户在某资源上的有效规则（deny-overrides），对返回 DTO 反射脱敏，并校验更新字段可编辑性。
/// </summary>
public interface IFieldSecurityService
{
    /// <summary>
    /// 解析当前用户在指定资源上的有效字段规则（字段名 → 规则）。无登录/无资源/无规则返回空。
    /// </summary>
    Task<IReadOnlyDictionary<string, EffectiveFieldRule>> ResolveAsync(string resourceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对单个返回对象按有效规则就地脱敏。
    /// </summary>
    Task ApplyAsync<T>(string resourceCode, T? item, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 对返回对象集合按有效规则就地脱敏。
    /// </summary>
    Task ApplyAsync<T>(string resourceCode, IEnumerable<T> items, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 排序字段 FLS 门控：就地剔除当前用户在该资源上「不可读或已脱敏」的排序字段。
    /// 防止按受保护字段排序时——排序在 SQL 层按真实值进行、展示却被脱敏——通过结果顺序反推被保护的值。
    /// 字段名按大小写不敏感匹配（前端列键多为 camelCase，FLS 规则名为实体属性名 PascalCase）；无显式规则的字段默认放行。
    /// </summary>
    Task GuardSortsAsync(QueryConditions conditions, string resourceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验本次更新涉及的字段均可编辑；命中不可编辑字段则抛出异常。
    /// </summary>
    Task EnsureEditableAsync(string resourceCode, IEnumerable<string> changingFields, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写校验：对比更新入参与当前实体（按同名属性），命中不可编辑字段被实际修改则抛出异常。
    /// </summary>
    Task EnsureUpdatableAsync<TInput, TCurrent>(string resourceCode, TInput input, TCurrent current, CancellationToken cancellationToken = default)
        where TInput : class
        where TCurrent : class;
}
