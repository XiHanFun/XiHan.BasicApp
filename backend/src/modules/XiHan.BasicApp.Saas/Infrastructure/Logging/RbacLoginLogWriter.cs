#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacLoginLogWriter
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/03 23:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Api.Logging.Writers;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// RBAC 登录日志写入器
/// </summary>
public class RbacLoginLogWriter : ILoginLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacLoginLogWriter(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 写入登录日志
    /// </summary>
    public async Task WriteAsync(LoginLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var clientInfo = _clientInfoProvider.GetCurrent();
        var tenantId = _currentTenant.Id ?? 0;

        var entity = new SysLoginLog
        {
            TenantId = tenantId,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            SessionId = RbacLogMappingHelper.TrimOrNull(record.SessionId, 100),
            TraceId = RbacLogMappingHelper.TrimOrNull(record.TraceId, 64),
            LoginIp = RbacLogMappingHelper.TrimOrNull(record.LoginIp ?? clientInfo.IpAddress, 50),
            LoginLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            UserAgent = RbacLogMappingHelper.TrimOrNull(record.UserAgent ?? clientInfo.UserAgent, 500),
            Device = RbacLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 50),
            DeviceId = RbacLogMappingHelper.TrimOrNull(record.DeviceId, 200),
            LoginResult = (LoginResult)record.LoginResult,
            Message = RbacLogMappingHelper.TrimOrNull(record.Message, 500),
            LoginTime = record.LoginTime,
            CreatedTime = record.LoginTime
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }
}
