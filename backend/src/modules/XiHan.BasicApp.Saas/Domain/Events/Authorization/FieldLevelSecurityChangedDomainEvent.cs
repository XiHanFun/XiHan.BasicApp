#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityChangedDomainEvent
// Guid:f2cf5c96-93ab-4bd3-a5e8-2fe4e4280537
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 字段级安全变更事件
/// </summary>
public sealed class FieldLevelSecurityChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FieldLevelSecurityChangedDomainEvent(
        long tenantId,
        long fieldSecurityId,
        FieldSecurityTargetType targetType,
        long targetId,
        long resourceId,
        string fieldName,
        bool isReadable,
        bool isEditable,
        FieldMaskStrategy maskStrategy,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        FieldSecurityId = fieldSecurityId;
        TargetType = targetType;
        TargetId = targetId;
        ResourceId = resourceId;
        FieldName = fieldName;
        IsReadable = isReadable;
        IsEditable = isEditable;
        MaskStrategy = maskStrategy;
    }

    /// <summary>
    /// 字段级安全ID
    /// </summary>
    public long FieldSecurityId { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public FieldSecurityTargetType TargetType { get; }

    /// <summary>
    /// 目标ID
    /// </summary>
    public long TargetId { get; }

    /// <summary>
    /// 资源ID
    /// </summary>
    public long ResourceId { get; }

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// 是否可读
    /// </summary>
    public bool IsReadable { get; }

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool IsEditable { get; }

    /// <summary>
    /// 脱敏策略
    /// </summary>
    public FieldMaskStrategy MaskStrategy { get; }
}
