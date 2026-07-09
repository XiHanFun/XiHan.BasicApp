#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionChangeLogEventHandler
// Guid:5d2c7f18-0b3a-4e6c-9f21-7a4c8e0d1b53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 权限变更审计日志处理器
/// </summary>
/// <remarks>
/// 订阅 <see cref="AuthorizationChangedDomainEvent"/>，把"谁在什么时候给谁授予/撤销了什么权限"
/// 结构化落库到 <see cref="SysPermissionChangeLog"/>（按月分表，只追加）。
/// 写入时解析并快照 操作人/目标用户/目标角色/权限 的名称与操作 IP，便于审计直读、且不随后续改名或删除而失真。
/// 与 <c>AuthorizationChangedEventHandler</c>（负责缓存失效）职责分离；共享发布方的工作单元事务落库。
/// </remarks>
[UnitOfWork(true)]
public sealed class PermissionChangeLogEventHandler
    : ILocalEventHandler<AuthorizationChangedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly IClientInfoProvider _clientInfoProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionChangeLogEventHandler(
        ISqlSugarClientResolver clientResolver,
        IClientInfoProvider clientInfoProvider)
    {
        _clientResolver = clientResolver;
        _clientInfoProvider = clientInfoProvider;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <inheritdoc />
    public async Task HandleEventAsync(AuthorizationChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var now = DateTimeOffset.UtcNow;
        var entity = new SysPermissionChangeLog
        {
            TenantId = eventData.TenantId,
            OperatorUserId = eventData.OperatorUserId,
            OperatorUserName = await ResolveUserNameAsync(eventData.OperatorUserId),
            TargetUserId = eventData.TargetUserId,
            TargetUserName = await ResolveUserNameAsync(eventData.TargetUserId),
            TargetRoleId = eventData.TargetRoleId,
            TargetRoleName = await ResolveRoleNameAsync(eventData.TargetRoleId),
            PermissionId = eventData.PermissionId,
            PermissionName = await ResolvePermissionNameAsync(eventData.PermissionId),
            ChangeType = eventData.ChangeType,
            ChangeReason = NormalizeText(eventData.Reason, 500),
            OperationIp = NormalizeText(_clientInfoProvider.GetCurrent().IpAddress, 50),
            ChangeTime = now,
            CreatedTime = now
        };
        entity.Description = NormalizeText(BuildDescription(entity), 500);

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    /// <summary>
    /// 解析用户名称（账号名）。
    /// </summary>
    /// <remarks>
    /// 清租户行过滤：审计名称是写入时快照，需跨租户/平台态解析（如平台超管给某租户用户直授权限，
    /// 当前上下文不覆盖该租户，若带租户过滤会解析为空）。读取侧仍按日志 TenantId 隔离，不造成越权。
    /// </remarks>
    private async Task<string?> ResolveUserNameAsync(long? userId)
    {
        if (userId is not > 0)
        {
            return null;
        }

        var name = await DbClient.Queryable<SysUser>()
            .ClearFilter<IMultiTenantEntity>()
            .Where(user => user.BasicId == userId.Value)
            .Select(user => user.UserName)
            .FirstAsync();
        return NormalizeText(name, 64);
    }

    /// <summary>
    /// 解析角色名称（清租户过滤，理由同用户）。
    /// </summary>
    private async Task<string?> ResolveRoleNameAsync(long? roleId)
    {
        if (roleId is not > 0)
        {
            return null;
        }

        var name = await DbClient.Queryable<SysRole>()
            .ClearFilter<IMultiTenantEntity>()
            .Where(role => role.BasicId == roleId.Value)
            .Select(role => role.RoleName)
            .FirstAsync();
        return NormalizeText(name, 100);
    }

    /// <summary>
    /// 解析权限名称（清租户过滤，理由同用户）。
    /// </summary>
    private async Task<string?> ResolvePermissionNameAsync(long? permissionId)
    {
        if (permissionId is not > 0)
        {
            return null;
        }

        var name = await DbClient.Queryable<SysPermission>()
            .ClearFilter<IMultiTenantEntity>()
            .Where(permission => permission.BasicId == permissionId.Value)
            .Select(permission => permission.PermissionName)
            .FirstAsync();
        return NormalizeText(name, 200);
    }

    /// <summary>
    /// 构建人类可读摘要（优先名称，回退 ID）。
    /// </summary>
    private static string BuildDescription(SysPermissionChangeLog log)
    {
        var parts = new List<string>();
        if (log.TargetUserId is > 0)
        {
            parts.Add($"用户「{log.TargetUserName ?? log.TargetUserId.ToString()}」");
        }

        if (log.TargetRoleId is > 0)
        {
            parts.Add($"角色「{log.TargetRoleName ?? log.TargetRoleId.ToString()}」");
        }

        if (log.PermissionId is > 0)
        {
            parts.Add($"权限「{log.PermissionName ?? log.PermissionId.ToString()}」");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : log.ChangeType.ToString();
    }

    /// <summary>
    /// 规范化字符串长度。
    /// </summary>
    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
