// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Operations.FieldSecurity;

/// <summary>
/// <c>field-security.entities</c> 的请求（无参数）。
/// </summary>
public sealed record FieldSecurityEntitiesRequest;

/// <summary>
/// 字段安全里一个可配置的字段。
/// </summary>
/// <param name="Name">字段名（与接口字段同名）。</param>
/// <param name="TitleKey">显示名的界面文案键。</param>
/// <param name="IsText">是否文本；非文本只能明文只读或隐藏。</param>
/// <param name="IsNullable">响应里是否可空；不可空的不能隐藏。</param>
public sealed record FieldSecurityFieldResponse(string Name, string TitleKey, bool IsText, bool IsNullable);

/// <summary>
/// 字段安全里一个可配置的实体。
/// </summary>
/// <param name="Code">实体码。</param>
/// <param name="TitleKey">显示名的界面文案键。</param>
/// <param name="Fields">可配置的字段。</param>
public sealed record FieldSecurityEntityResponse(string Code, string TitleKey, IReadOnlyList<FieldSecurityFieldResponse> Fields);

/// <summary>
/// <c>field-security.entities</c> 的响应：各包登记的实体与字段（按实体码排序）。
/// </summary>
/// <param name="Items">实体。</param>
public sealed record FieldSecurityEntitiesResponse(IReadOnlyList<FieldSecurityEntityResponse> Items);

/// <summary>
/// <c>field-security.get</c> 的请求。
/// </summary>
/// <param name="Id">规则 ID。</param>
public sealed record GetFieldSecurityRuleRequest(Guid Id);

/// <summary>
/// <c>field-security.delete</c> 的请求。
/// </summary>
/// <param name="Ids">规则 ID，1 到 100 个。</param>
public sealed record FieldSecurityRuleIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>field-security.create</c> 的请求：在当前一侧新建规则。
/// </summary>
/// <param name="TargetKind">目标类型（部门只在租户侧）。</param>
/// <param name="TargetId">目标 ID：这一侧的角色、用户或部门，须启用。</param>
/// <param name="Entity">实体码（见 <c>field-security.entities</c>）。</param>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位（0 到 32，只有部分脱敏时用）。</param>
/// <param name="KeepTail">部分脱敏保留后几位（0 到 32，只有部分脱敏时用）。</param>
/// <param name="Replacement">固定文本（不超过 100 字，只有固定文本时用）。</param>
/// <param name="IsEditable">能否改写：明文规则只能只读；脱敏且可改即只写。</param>
/// <param name="Status">状态。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateFieldSecurityRuleRequest(
    FieldSecurityTargetKind TargetKind,
    [Required] Guid TargetId,
    [Required, StringLength(64, MinimumLength = 1)] string Entity,
    [Required, StringLength(64, MinimumLength = 1)] string Field,
    FieldMaskStrategy Strategy,
    [Range(0, SysFieldSecurityRule.MaxKeep)] int? KeepHead = null,
    [Range(0, SysFieldSecurityRule.MaxKeep)] int? KeepTail = null,
    [StringLength(SysFieldSecurityRule.MaxReplacementLength)] string? Replacement = null,
    bool IsEditable = false,
    FieldRuleStatus Status = FieldRuleStatus.Active,
    [StringLength(512)] string? Remark = null)
{
    /// <summary>
    /// 转成规则定义。
    /// </summary>
    public FieldRuleDefinition ToDefinition() => new(TargetKind, TargetId, Entity.Trim(), Field.Trim(), Strategy, KeepHead, KeepTail, Replacement, IsEditable, Status, Remark);
}

/// <summary>
/// <c>field-security.update</c> 的请求：整体修改规则（含状态）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="TargetKind">目标类型。</param>
/// <param name="TargetId">目标 ID。</param>
/// <param name="Entity">实体码。</param>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位。</param>
/// <param name="KeepTail">部分脱敏保留后几位。</param>
/// <param name="Replacement">固定文本。</param>
/// <param name="IsEditable">能否改写。</param>
/// <param name="Status">状态。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateFieldSecurityRuleRequest(
    Guid Id,
    FieldSecurityTargetKind TargetKind,
    [Required] Guid TargetId,
    [Required, StringLength(64, MinimumLength = 1)] string Entity,
    [Required, StringLength(64, MinimumLength = 1)] string Field,
    FieldMaskStrategy Strategy,
    [Range(0, SysFieldSecurityRule.MaxKeep)] int? KeepHead = null,
    [Range(0, SysFieldSecurityRule.MaxKeep)] int? KeepTail = null,
    [StringLength(SysFieldSecurityRule.MaxReplacementLength)] string? Replacement = null,
    bool IsEditable = false,
    FieldRuleStatus Status = FieldRuleStatus.Active,
    [StringLength(512)] string? Remark = null)
{
    /// <summary>
    /// 转成规则定义。
    /// </summary>
    public FieldRuleDefinition ToDefinition() => new(TargetKind, TargetId, Entity.Trim(), Field.Trim(), Strategy, KeepHead, KeepTail, Replacement, IsEditable, Status, Remark);
}

/// <summary>
/// 一条字段安全规则。
/// </summary>
/// <param name="Id">规则 ID。</param>
/// <param name="TargetKind">目标类型。</param>
/// <param name="TargetId">目标 ID。</param>
/// <param name="TargetName">目标显示名；目标已不存在时为空。</param>
/// <param name="Entity">实体码。</param>
/// <param name="Field">字段名。</param>
/// <param name="Strategy">读取方式。</param>
/// <param name="KeepHead">部分脱敏保留前几位。</param>
/// <param name="KeepTail">部分脱敏保留后几位。</param>
/// <param name="Replacement">固定文本。</param>
/// <param name="IsEditable">能否改写。</param>
/// <param name="Status">状态。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record FieldSecurityRuleResponse(
    Guid Id,
    FieldSecurityTargetKind TargetKind,
    Guid TargetId,
    string? TargetName,
    string Entity,
    string Field,
    FieldMaskStrategy Strategy,
    int? KeepHead,
    int? KeepTail,
    string? Replacement,
    bool IsEditable,
    FieldRuleStatus Status,
    string? Remark,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static FieldSecurityRuleResponse From(SysFieldSecurityRule rule, string? targetName)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new FieldSecurityRuleResponse(rule.Id, rule.TargetKind, rule.TargetId, targetName, rule.Entity, rule.Field, rule.Strategy, rule.KeepHead, rule.KeepTail, rule.Replacement, rule.IsEditable, rule.Status, rule.Remark, rule.CreatedAt);
    }
}
