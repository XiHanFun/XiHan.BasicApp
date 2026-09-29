// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// 一个套餐的种子声明
/// </summary>
/// <param name="Code">套餐编码</param>
/// <param name="Name">套餐名称</param>
/// <param name="Description">说明</param>
/// <param name="UserLimit">席位上限（空表示不限）</param>
/// <param name="StorageLimit">存储上限 MB（空表示不限）</param>
/// <param name="Price">价格（空表示面议）</param>
/// <param name="BillingPeriodMonths">计费周期（月）</param>
/// <param name="IsFree">是否免费</param>
/// <param name="IsDefault">是否新租户的默认套餐</param>
/// <param name="Sort">排序</param>
/// <param name="PermissionCodes">功能白名单；空表示租户能生效的全部权限（含各模块）</param>
public sealed record EditionSeed(
    string Code,
    string Name,
    string Description,
    int? UserLimit,
    long? StorageLimit,
    decimal? Price,
    int? BillingPeriodMonths,
    bool IsFree,
    bool IsDefault,
    int Sort,
    IReadOnlyList<string>? PermissionCodes);

/// <summary>
/// SaaS 套餐：免费版、基础版、专业版、企业版及各自的功能白名单
/// </summary>
/// <remarks>
/// 套餐的价格、配额、白名单归平台运营：只在套餐首次创建时连同白名单一起写入，之后运营改过的不再覆盖；
/// 编码已存在的套餐（包括运营自建的同码套餐、运营删掉的套餐）整个跳过。
/// 本种子排在全部模块的权限目录之后，企业版首次创建即拿到各模块租户能生效的全部权限；
/// 之后新增的权限要进哪些套餐，由运营在套餐管理里授予，或由对应版本的升级脚本写入。
/// 白名单只能含租户能生效的权限（租户侧与两侧），声明里混进平台侧或不存在的权限码直接报错。
/// </remarks>
public sealed class SaasEditionSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasEditionSeeder> logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 免费版：成员与组织只读，日志与消息基础可用
    /// </summary>
    private static readonly string[] FreePermissions =
    [
        SaasPermissionCodes.TenantSubscription.Read,
        SaasPermissionCodes.TenantMember.Read,
        SaasPermissionCodes.TenantMember.Update,
        SaasPermissionCodes.TenantMember.Status,
        SaasPermissionCodes.TenantMember.InviteStatus,
        SaasPermissionCodes.TenantMember.Revoke,
        SaasPermissionCodes.Department.Read,
        SaasPermissionCodes.User.Read,
        SaasPermissionCodes.UserSession.Read,
        SaasPermissionCodes.UserStatistics.Read,
        SaasPermissionCodes.UserDepartment.Read,
        SaasPermissionCodes.Role.Read,
        SaasPermissionCodes.RolePermission.Read,
        SaasPermissionCodes.UserRole.Read,
        SaasPermissionCodes.Permission.Read,
        SaasPermissionCodes.Config.Read,
        SaasPermissionCodes.Dict.Read,
        SaasPermissionCodes.File.Read,
        SaasPermissionCodes.Message.Read,
        SaasPermissionCodes.Notification.Read,
        SaasPermissionCodes.AccessLog.Read,
        SaasPermissionCodes.LoginLog.Read,
        SaasPermissionCodes.OperationLog.Read
    ];

    /// <summary>
    /// 基础版：免费版 + 组织、用户、角色的日常管理与通知发布
    /// </summary>
    private static readonly string[] BasicPermissions =
    [
        .. FreePermissions,
        SaasPermissionCodes.Department.Create,
        SaasPermissionCodes.Department.Update,
        SaasPermissionCodes.Department.Status,
        SaasPermissionCodes.User.Create,
        SaasPermissionCodes.User.Update,
        SaasPermissionCodes.User.Status,
        SaasPermissionCodes.UserSecurity.Read,
        SaasPermissionCodes.UserSecurity.ResetPassword,
        SaasPermissionCodes.UserSession.Revoke,
        SaasPermissionCodes.Role.Create,
        SaasPermissionCodes.Role.Update,
        SaasPermissionCodes.Role.Status,
        SaasPermissionCodes.RolePermission.Grant,
        SaasPermissionCodes.RolePermission.Update,
        SaasPermissionCodes.RolePermission.Status,
        SaasPermissionCodes.RolePermission.Revoke,
        SaasPermissionCodes.UserRole.Grant,
        SaasPermissionCodes.UserRole.Update,
        SaasPermissionCodes.UserRole.Status,
        SaasPermissionCodes.UserRole.Revoke,
        SaasPermissionCodes.UserDepartment.Grant,
        SaasPermissionCodes.UserDepartment.Update,
        SaasPermissionCodes.UserDepartment.Status,
        SaasPermissionCodes.UserDepartment.Revoke,
        SaasPermissionCodes.Notification.Create,
        SaasPermissionCodes.Notification.Update,
        SaasPermissionCodes.Notification.Publish,
        SaasPermissionCodes.Notification.Delete,
        SaasPermissionCodes.MessageTemplate.Read
    ];

    /// <summary>
    /// 专业版：基础版 + 高级授权（继承、数据范围、直授、字段安全、委托、申请、条件、约束）、审计日志、存储与模板管理
    /// </summary>
    private static readonly string[] ProPermissions =
    [
        .. BasicPermissions,
        SaasPermissionCodes.UserSecurity.Lock,
        SaasPermissionCodes.UserSecurity.LoginPolicy,
        SaasPermissionCodes.RoleHierarchy.Read,
        SaasPermissionCodes.RoleHierarchy.Create,
        SaasPermissionCodes.RoleHierarchy.Delete,
        SaasPermissionCodes.RoleDataScope.Read,
        SaasPermissionCodes.RoleDataScope.Update,
        SaasPermissionCodes.UserPermission.Read,
        SaasPermissionCodes.UserPermission.Grant,
        SaasPermissionCodes.UserPermission.Update,
        SaasPermissionCodes.UserPermission.Status,
        SaasPermissionCodes.UserPermission.Revoke,
        SaasPermissionCodes.UserDataScope.Read,
        SaasPermissionCodes.UserDataScope.Update,
        SaasPermissionCodes.FieldLevelSecurity.Read,
        SaasPermissionCodes.FieldLevelSecurity.Create,
        SaasPermissionCodes.FieldLevelSecurity.Update,
        SaasPermissionCodes.FieldLevelSecurity.Status,
        SaasPermissionCodes.FieldLevelSecurity.Delete,
        SaasPermissionCodes.PermissionDelegation.Read,
        SaasPermissionCodes.PermissionDelegation.Create,
        SaasPermissionCodes.PermissionDelegation.Update,
        SaasPermissionCodes.PermissionDelegation.Status,
        SaasPermissionCodes.PermissionDelegation.Revoke,
        SaasPermissionCodes.PermissionRequest.Read,
        SaasPermissionCodes.PermissionRequest.Create,
        SaasPermissionCodes.PermissionRequest.Update,
        SaasPermissionCodes.PermissionRequest.Status,
        SaasPermissionCodes.PermissionRequest.Withdraw,
        SaasPermissionCodes.PermissionCondition.Read,
        SaasPermissionCodes.PermissionCondition.Create,
        SaasPermissionCodes.PermissionCondition.Update,
        SaasPermissionCodes.PermissionCondition.Status,
        SaasPermissionCodes.PermissionCondition.Delete,
        SaasPermissionCodes.ConstraintRule.Read,
        SaasPermissionCodes.ConstraintRule.Create,
        SaasPermissionCodes.ConstraintRule.Update,
        SaasPermissionCodes.ConstraintRule.Status,
        SaasPermissionCodes.ConstraintRule.Delete,
        SaasPermissionCodes.ApiLog.Read,
        SaasPermissionCodes.DiffLog.Read,
        SaasPermissionCodes.ExceptionLog.Read,
        SaasPermissionCodes.PermissionChangeLog.Read,
        SaasPermissionCodes.StorageConfig.Read,
        SaasPermissionCodes.StorageConfig.Create,
        SaasPermissionCodes.StorageConfig.Update,
        SaasPermissionCodes.StorageConfig.Status,
        SaasPermissionCodes.StorageConfig.Delete,
        SaasPermissionCodes.MessageTemplate.Create,
        SaasPermissionCodes.MessageTemplate.Update,
        SaasPermissionCodes.MessageTemplate.Status,
        SaasPermissionCodes.MessageTemplate.Delete
    ];

    /// <summary>
    /// 套餐编码
    /// </summary>
    public static class Codes
    {
        /// <summary>
        /// 免费版（新租户默认）
        /// </summary>
        public const string Free = "free";

        /// <summary>
        /// 基础版
        /// </summary>
        public const string Basic = "basic";

        /// <summary>
        /// 专业版
        /// </summary>
        public const string Pro = "pro";

        /// <summary>
        /// 企业版
        /// </summary>
        public const string Enterprise = "enterprise";
    }

    /// <summary>
    /// 套餐声明
    /// </summary>
    public static IReadOnlyList<EditionSeed> Editions { get; } =
    [
        new(Codes.Free, "免费版", "适用于个人试用和小团队基础协作", 5, 1024, 0, 1, IsFree: true, IsDefault: true, 10, FreePermissions),
        new(Codes.Basic, "基础版", "适用于小型团队的组织、用户和角色管理", 20, 10240, 99, 1, IsFree: false, IsDefault: false, 20, BasicPermissions),
        new(Codes.Pro, "专业版", "适用于中大型团队的高级权限、审计和安全能力", 100, 102400, 299, 1, IsFree: false, IsDefault: false, 30, ProPermissions),
        new(Codes.Enterprise, "企业版", "适用于企业客户的完整能力和不限配额", null, null, null, 12, IsFree: false, IsDefault: false, 40, PermissionCodes: null)
    ];

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.Editions;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]套餐";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var codes = Editions.Select(static edition => edition.Code).ToList();
        var existingCodes = (await DbClient.Queryable<SysTenantEdition>()
                .IncludingDeleted()
                .Where(edition => edition.TenantId == 0 && codes.Contains(edition.EditionCode))
                .Select(edition => edition.EditionCode)
                .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = Editions.Where(edition => !existingCodes.Contains(edition.Code)).ToList();
        if (missing.Count == 0)
        {
            Logger.LogInformation("{Seeder}：套餐已存在，不覆盖运营的修改", Name);
            return;
        }

        var tenantEffective = (await DbClient.Queryable<SysPermission>()
                .Where(permission => permission.TenantId == 0)
                .ToListAsync())
            .Where(permission => permission.Side.IsTenantEffective())
            .ToDictionary(permission => permission.PermissionCode, permission => permission.BasicId, StringComparer.Ordinal);

        foreach (var seed in missing)
        {
            var edition = await DbClient.Insertable(new SysTenantEdition
            {
                TenantId = 0,
                EditionCode = seed.Code,
                EditionName = seed.Name,
                Description = seed.Description,
                UserLimit = seed.UserLimit,
                StorageLimit = seed.StorageLimit,
                Price = seed.Price,
                BillingPeriodMonths = seed.BillingPeriodMonths,
                IsFree = seed.IsFree,
                IsDefault = seed.IsDefault,
                Status = EnableStatus.Enabled,
                Sort = seed.Sort,
                Remark = "系统初始化套餐"
            }).ExecuteReturnEntityAsync();

            var permissionIds = ResolveWhitelist(seed, tenantEffective);
            await BulkInsertAsync([.. permissionIds.Select(permissionId => new SysTenantEditionPermission
            {
                TenantId = 0,
                EditionId = edition.BasicId,
                PermissionId = permissionId,
                Status = ValidityStatus.Valid,
                Remark = $"系统初始化套餐白名单：{seed.Name}"
            })]);

            Logger.LogInformation("{Seeder}：新增套餐 {Edition}，白名单 {Count} 个权限", Name, seed.Name, permissionIds.Count);
        }
    }

    /// <summary>
    /// 解析套餐白名单：声明的权限码逐个对上租户能生效的权限；企业版取全部
    /// </summary>
    private static IReadOnlyList<long> ResolveWhitelist(EditionSeed seed, IReadOnlyDictionary<string, long> tenantEffective)
    {
        if (seed.PermissionCodes is null)
        {
            return [.. tenantEffective.Values];
        }

        var unknown = seed.PermissionCodes.Where(code => !tenantEffective.ContainsKey(code)).ToList();
        return unknown.Count == 0
            ? [.. seed.PermissionCodes.Distinct(StringComparer.Ordinal).Select(code => tenantEffective[code])]
            : throw new InvalidOperationException($"套餐 {seed.Code} 的白名单含不存在或平台侧的权限：{string.Join("、", unknown)}。");
    }
}
