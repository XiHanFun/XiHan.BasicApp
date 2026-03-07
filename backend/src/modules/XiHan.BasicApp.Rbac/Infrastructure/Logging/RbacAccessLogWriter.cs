#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacAccessLogWriter
// Guid:1a2b8f30-c377-4ff9-9488-7d6ea5f41f53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:08:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Rbac.Infrastructure.Logging;

/// <summary>
/// RBAC 访问日志写入器
/// </summary>
public class RbacAccessLogWriter : IAccessLogWriter
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ISqlSugarSplitTableExecutor _splitTableExecutor;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClientInfoProvider _clientInfoProvider;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="currentTenant"></param>
    /// <param name="clientInfoProvider"></param>
    public RbacAccessLogWriter(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        ICurrentTenant currentTenant,
        IClientInfoProvider clientInfoProvider)
    {
        _dbContext = dbContext;
        _splitTableExecutor = splitTableExecutor;
        _currentTenant = currentTenant;
        _clientInfoProvider = clientInfoProvider;
    }

    /// <summary>
    /// 写入访问日志
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WriteAsync(AccessLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var now = DateTimeOffset.UtcNow;
        var elapsedMilliseconds = RbacLogMappingHelper.NormalizeElapsed(record.ElapsedMilliseconds);
        var clientInfo = _clientInfoProvider.GetCurrent();
        var accessTime = now.AddMilliseconds(-elapsedMilliseconds);

        var entity = new SysAccessLog
        {
            TenantId = _currentTenant.Id,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            SessionId = RbacLogMappingHelper.TrimOrNull(record.SessionId, 100),
            ResourcePath = RbacLogMappingHelper.TrimOrDefault(record.Path, 500, "/"),
            ResourceType = "HttpApi",
            Method = RbacLogMappingHelper.TrimOrNull(record.Method, 10),
            AccessResult = RbacLogMappingHelper.ResolveAccessResult(record.StatusCode),
            StatusCode = record.StatusCode,
            AccessIp = RbacLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.RemoteIp, 50),
            AccessLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            UserAgent = RbacLogMappingHelper.TrimOrNull(clientInfo.UserAgent ?? record.UserAgent, 500),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            Device = RbacLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 50),
            Referer = RbacLogMappingHelper.TrimOrNull(record.Referer, 500),
            ResponseTime = elapsedMilliseconds,
            AccessTime = accessTime,
            LeaveTime = now,
            StayTime = (long)Math.Ceiling(elapsedMilliseconds / 1000D),
            ErrorMessage = RbacLogMappingHelper.TrimOrNull(record.ErrorMessage, 1000),
            Remark = RbacLogMappingHelper.TrimOrNull(
                string.IsNullOrWhiteSpace(record.TraceId) ? null : $"TraceId:{record.TraceId}",
                500)
        };

        await _splitTableExecutor.InsertAsync(DbClient, [entity], cancellationToken);
    }
}
