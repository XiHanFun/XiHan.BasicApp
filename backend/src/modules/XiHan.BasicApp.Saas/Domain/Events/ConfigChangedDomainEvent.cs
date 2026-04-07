#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigChangedDomainEvent
// Guid:b3c4d5e6-f7a8-9012-cdef-012345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 配置变更领域事件
/// </summary>
public sealed class ConfigChangedDomainEvent : EntityChangedDomainEvent<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigChangedDomainEvent(long entityId, string? configKey = null, long? tenantId = null, string? configGroup = null)
        : base(entityId, tenantId)
    {
        ConfigKey = configKey;
        ConfigGroup = configGroup;
    }

    /// <summary>
    /// 配置键
    /// </summary>
    public string? ConfigKey { get; }

    /// <summary>
    /// 配置分组（用于分组列表缓存失效）
    /// </summary>
    public string? ConfigGroup { get; }
}
