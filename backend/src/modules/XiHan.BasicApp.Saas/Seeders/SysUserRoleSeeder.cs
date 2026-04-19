#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRoleSeeder
// Guid:9f0a1b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Saas.Seeders;

/// <summary>
/// 系统用户角色关系种子数据
/// </summary>
public class SysUserRoleSeeder : DataSeederBase
{
    private const string SuperAdminUserName = "superadmin";
    private const string SuperAdminRoleCode = "super_admin";

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserRoleSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysUserRoleSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => 16;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Saas]系统用户角色关系种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var users = await DbClient.Queryable<SysUser>().ToListAsync();
        var roles = await DbClient.Queryable<SysRole>().ToListAsync();

        if (users.Count == 0 || roles.Count == 0)
        {
            Logger.LogWarning("找不到用户或角色数据，跳过用户角色关系种子数据");
            return;
        }

        var userMap = users
            .Where(user => !string.IsNullOrWhiteSpace(user.UserName))
            .GroupBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(user => user.TenantId != 0 ? 1 : 0)
                    .ThenBy(user => user.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);

        var roleMap = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.RoleCode))
            .GroupBy(role => role.RoleCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(role => role.TenantId != 0 ? 1 : 0)
                    .ThenBy(role => role.BasicId)
                    .First()
                    .BasicId,
                StringComparer.OrdinalIgnoreCase);

        var requiredPairs = new List<(long UserId, long RoleId)>();
        AddUserRole(requiredPairs, userMap, roleMap, SuperAdminUserName, SuperAdminRoleCode);
        AddUserRole(requiredPairs, userMap, roleMap, "admin", "admin");
        AddUserRole(requiredPairs, userMap, roleMap, "test", "employee");
        AddUserRole(requiredPairs, userMap, roleMap, "demo", "guest");

        var removedSuperAdminMappingCount = 0;
        if (roleMap.TryGetValue(SuperAdminRoleCode, out var superAdminRoleId)
            && userMap.TryGetValue(SuperAdminUserName, out var superAdminUserId))
        {
            removedSuperAdminMappingCount = await DbClient
                .Deleteable<SysUserRole>()
                .Where(mapping =>
                    mapping.RoleId == superAdminRoleId
                    && mapping.UserId != superAdminUserId)
                .ExecuteCommandAsync();
        }

        if (requiredPairs.Count == 0)
        {
            Logger.LogWarning("未解析到任何用户角色关系，跳过用户角色关系种子数据");
            return;
        }

        var targetUserIds = requiredPairs.Select(pair => pair.UserId).Distinct().ToArray();
        var targetRoleIds = requiredPairs.Select(pair => pair.RoleId).Distinct().ToArray();
        var existingPairs = await DbClient
            .Queryable<SysUserRole>()
            .Where(mapping => targetUserIds.Contains(mapping.UserId) && targetRoleIds.Contains(mapping.RoleId))
            .Select(mapping => new { mapping.BasicId, mapping.UserId, mapping.RoleId, mapping.Status })
            .ToListAsync();

        var existingSet = existingPairs
            .Select(pair => $"{pair.UserId}_{pair.RoleId}")
            .ToHashSet(StringComparer.Ordinal);

        var userRoles = requiredPairs
            .Where(pair => !existingSet.Contains($"{pair.UserId}_{pair.RoleId}"))
            .Select(pair => new SysUserRole
            {
                UserId = pair.UserId,
                RoleId = pair.RoleId,
                Status = YesOrNo.Yes
            })
            .ToList();

        var disabledMappingIds = existingPairs
            .Where(pair =>
                pair.Status != YesOrNo.Yes
                && requiredPairs.Contains((pair.UserId, pair.RoleId)))
            .Select(pair => pair.BasicId)
            .Distinct()
            .ToArray();
        if (disabledMappingIds.Length > 0)
        {
            await DbClient
                .Updateable<SysUserRole>()
                .SetColumns(mapping => mapping.Status == YesOrNo.Yes)
                .Where(mapping => disabledMappingIds.Contains(mapping.BasicId))
                .ExecuteCommandAsync();
        }

        if (userRoles.Count == 0 && disabledMappingIds.Length == 0 && removedSuperAdminMappingCount == 0)
        {
            Logger.LogInformation("用户角色关系数据已存在，跳过新增");
            return;
        }

        if (userRoles.Count > 0)
        {
            await BulkInsertAsync(userRoles);
        }

        Logger.LogInformation(
            "用户角色关系种子完成：新增 {InsertCount} 条，启用 {EnableCount} 条，清理多余超管绑定 {RemovedSuperAdminCount} 条",
            userRoles.Count,
            disabledMappingIds.Length,
            removedSuperAdminMappingCount);
    }

    /// <summary>
    /// 添加用户角色关系
    /// </summary>
    private static void AddUserRole(
        ICollection<(long UserId, long RoleId)> target,
        IReadOnlyDictionary<string, long> userMap,
        IReadOnlyDictionary<string, long> roleMap,
        string userName,
        string roleCode)
    {
        if (userMap.TryGetValue(userName, out var userId) && roleMap.TryGetValue(roleCode, out var roleId))
        {
            target.Add((userId, roleId));
        }
    }
}
