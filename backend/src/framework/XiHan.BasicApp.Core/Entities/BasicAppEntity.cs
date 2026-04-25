#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppEntity
// Guid:892d867f-e909-4e3c-80c9-81c24b8e07dc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/10 06:33:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Data.SqlSugar.Aggregates;
using XiHan.Framework.Data.SqlSugar.Entities;

namespace XiHan.BasicApp.Core.Entities;

/// <summary>
/// BasicApp 实体基类
/// </summary>
/// <remarks>
/// 本项目统一约定：
/// - 平台级/全局记录统一使用 TenantId = 0（"平台租户"占位），不得使用 NULL
/// - 业务租户 Id 从 1 开始分配；0 号租户由平台保留
/// - IsGlobal = true 的实体写入时必须同时设置 TenantId = 0
/// - 查询合并全局 + 私有：WHERE TenantId IN (0, {currentTenantId})
/// - 服务层应有 ITenantContext 自动注入 TenantId，禁止业务代码直接操纵
///
/// 【审计三件套索引规范】
/// - 所有具体实体必须包含：IX_{table}_TeId_CrTi、IX_{table}_CrId
/// - FullAudited/AggregateRoot 实体额外包含：IX_{table}_TeId_IsDe
/// - 日志类分表实体将 {table} 替换为 {split_table}
/// </remarks>
public abstract class BasicAppEntity : SugarMultiTenantEntity<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected BasicAppEntity()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basicId"></param>
    protected BasicAppEntity(long basicId)
        : base(basicId)
    {
    }
}

/// <summary>
/// BasicApp 实体基类（自增主键）
/// </summary>
public abstract class BasicAppEntityWithIdentity : SugarMultiTenantEntityWithIdentity<long>
{
}

/// <summary>
/// BasicApp 创建审计实体基类
/// </summary>
public abstract class BasicAppCreationEntity : SugarMultiTenantCreationEntity<long>
{
}

/// <summary>
/// BasicApp 修改审计实体基类
/// </summary>
public abstract class BasicAppModificationEntity : SugarMultiTenantModificationEntity<long>
{
}

/// <summary>
/// BasicApp 删除审计实体基类
/// </summary>
public abstract class BasicAppDeletionEntity : SugarMultiTenantDeletionEntity<long>
{
}

/// <summary>
/// BasicApp 完整审计实体基类
/// </summary>
public abstract class BasicAppFullAuditedEntity : SugarMultiTenantFullAuditedEntity<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected BasicAppFullAuditedEntity()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basicId"></param>
    protected BasicAppFullAuditedEntity(long basicId)
        : base(basicId)
    {
    }
}

/// <summary>
/// BasicApp 聚合根基类
/// </summary>
public abstract class BasicAppAggregateRoot : SugarMultiTenantAggregateRoot<long>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    protected BasicAppAggregateRoot()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="basicId"></param>
    protected BasicAppAggregateRoot(long basicId)
        : base(basicId)
    {
    }
}
