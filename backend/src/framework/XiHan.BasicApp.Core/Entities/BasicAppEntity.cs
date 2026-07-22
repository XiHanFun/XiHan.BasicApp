// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// - 全局/平台记录的判定统一以 TenantId == 0 为准；如需 IsGlobal 语义，请在实体 Expand 中以派生只读属性 IsGlobal => TenantId == 0 暴露，不再落库（避免与 TenantId 漂移）
/// - 查询合并全局 + 私有：WHERE TenantId IN (0, {currentTenantId})
/// - 服务层应有 ITenantContext 自动注入 TenantId，禁止业务代码直接操纵
///
/// 【审计三件套索引规范】
/// - 所有具体实体必须包含：IX_{table}_TeId_CrTi、IX_{table}_CrId
/// - FullAudited/AggregateRoot 实体额外包含：IX_{table}_TeId_IsDe
/// - 日志类分表实体将 {table} 替换为 {split_table}
///
/// 【软删与唯一索引约定】
/// - FullAudited/AggregateRoot 等支持软删（IsDeleted）的实体，其唯一索引（UX_*）末列统一附加 IsDeleted，
///   使唯一性仅约束未删除行（IsDeleted=false），从而软删后可再次创建同编码记录
/// - 限制：同一编码至多保留一条软删行（IsDeleted=true）；如需第二次软删同编码记录，服务层须先物理清理旧软删行
/// - 纯创建型（Creation）关联/日志实体为硬删，无 IsDeleted，唯一索引保持原样
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
