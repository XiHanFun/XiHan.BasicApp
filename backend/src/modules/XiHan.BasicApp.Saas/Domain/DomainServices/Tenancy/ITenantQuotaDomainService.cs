// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户配额领域服务
/// </summary>
/// <remarks>
/// 职责：把 SysTenant / SysTenantEdition 上的 UserLimit、StorageLimit 从静态定义变成运行时约束。
///
/// 校验时机：
/// - 席位在创建用户前校验（UserDomainService.CreateUserAsync，覆盖后台创建、注册、外部登录建号三个入口）
/// - 存储在落库前校验（FileDomainService.CreateUploadingFileAsync，此时文件尚未开始传输）
/// - 秒传复用同租户既有文件、不新增占用，不参与校验
///
/// 平台运维态（无租户上下文）跳过校验；平台管理员切入某租户后仍按该租户配额校验，
/// 否则代管期间可无限创建，配额形同虚设。
/// </remarks>
public interface ITenantQuotaDomainService
{
    /// <summary>
    /// 校验当前租户新增席位后是否仍在配额内，超限抛出异常
    /// </summary>
    /// <param name="increment">本次新增席位数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureSeatQuotaAsync(int increment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验当前租户新增存储后是否仍在配额内，超限抛出异常
    /// </summary>
    /// <param name="incrementBytes">本次新增占用(字节)</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureStorageQuotaAsync(long incrementBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取租户配额快照（生效上限 + 已用量）
    /// </summary>
    /// <remarks>
    /// 供列表/详情展示使用：一次查询拿回全部租户的用量，不逐个统计。
    /// </remarks>
    /// <param name="tenantIds">租户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户主键到配额快照的映射；租户不存在时不在结果中</returns>
    Task<IReadOnlyDictionary<long, TenantQuotaSnapshot>> GetQuotaSnapshotsAsync(IReadOnlyCollection<long> tenantIds, CancellationToken cancellationToken = default);
}
