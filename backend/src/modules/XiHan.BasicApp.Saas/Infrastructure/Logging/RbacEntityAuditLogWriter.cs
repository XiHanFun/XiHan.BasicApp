#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityAuditLogWriter
// Guid:676be31f-c4ed-4f8d-b4f8-8f79d76b9764
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:24:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// RBAC 实体审计日志写入器
/// </summary>
public class RbacEntityAuditLogWriter : IEntityAuditLogWriter
{
    private readonly ISqlSugarDbContext _dbContext;
    private readonly ISqlSugarSplitTableExecutor _splitTableExecutor;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private ISqlSugarClient DbClient => _dbContext.GetClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="splitTableExecutor"></param>
    /// <param name="clientInfoProvider"></param>
    /// <param name="httpContextAccessor"></param>
    public RbacEntityAuditLogWriter(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _splitTableExecutor = splitTableExecutor;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 写入实体审计日志
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WriteAsync(EntityAuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var clientInfo = _clientInfoProvider.GetCurrent();
        var operationType = RbacLogMappingHelper.ResolveOperationTypeByName(record.OperationType);
        var requestId = RbacLogMappingHelper.TrimOrNull(record.RequestId, 100);
        var entityTypeName = RbacLogMappingHelper.TrimOrDefault(record.EntityType, 100, "UnknownEntity");
        var entityId = RbacLogMappingHelper.TrimOrNull(record.EntityId, 100);
        var sessionId = _httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>()?.Session?.Id;

        var entity = new SysAuditLog
        {
            TenantId = record.TenantId,
            UserId = record.UserId,
            UserName = RbacLogMappingHelper.TrimOrNull(record.UserName, 50),
            AuditType = RbacLogMappingHelper.TrimOrDefault(record.AuditType, 50, "EntityChange"),
            OperationType = operationType,
            EntityType = entityTypeName,
            EntityId = entityId,
            EntityName = entityTypeName,
            PrimaryKey = "BasicId",
            PrimaryKeyValue = entityId,
            Module = "Data",
            Function = RbacLogMappingHelper.TrimOrNull(record.OperationType, 50),
            Description = RbacLogMappingHelper.TrimOrNull(BuildDescription(record, entityTypeName, entityId), 500),
            BeforeData = RbacLogMappingHelper.TrimOrNull(record.BeforeData, 32000),
            AfterData = RbacLogMappingHelper.TrimOrNull(record.AfterData, 32000),
            ChangedFields = RbacLogMappingHelper.TrimOrNull(record.ChangedFields, 32000),
            ChangeDescription = RbacLogMappingHelper.TrimOrNull(BuildChangeDescription(record), 1000),
            RequestPath = RbacLogMappingHelper.TrimOrNull(record.RequestPath, 500),
            RequestMethod = RbacLogMappingHelper.TrimOrNull(record.RequestMethod, 10),
            OperationIp = RbacLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.OperationIp, 50),
            OperationLocation = RbacLogMappingHelper.TrimOrNull(clientInfo.Location, 200),
            Browser = RbacLogMappingHelper.TrimOrNull(clientInfo.Browser, 100),
            Os = RbacLogMappingHelper.TrimOrNull(clientInfo.OperatingSystem, 100),
            DeviceType = RbacLogMappingHelper.ResolveDeviceType(clientInfo.DeviceName),
            DeviceInfo = RbacLogMappingHelper.TrimOrNull(clientInfo.DeviceName, 200),
            UserAgent = RbacLogMappingHelper.TrimOrNull(clientInfo.UserAgent, 500),
            SessionId = RbacLogMappingHelper.TrimOrNull(sessionId, 100),
            RequestId = requestId,
            TraceId = RbacLogMappingHelper.TrimOrNull(requestId, 64),
            BusinessId = entityId,
            BusinessType = entityTypeName,
            IsSuccess = true,
            RiskLevel = RbacLogMappingHelper.ResolveRiskLevel(operationType),
            AuditTime = DateTimeOffset.UtcNow
        };

        await _splitTableExecutor.InsertAsync(DbClient, [entity], cancellationToken);
    }

    private static string BuildDescription(EntityAuditLogRecord record, string entityTypeName, string? entityId)
    {
        return entityId is null
            ? $"{record.OperationType}:{entityTypeName}"
            : $"{record.OperationType}:{entityTypeName}#{entityId}";
    }

    private static string BuildChangeDescription(EntityAuditLogRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ChangedFields))
        {
            return $"实体 {record.EntityType} 发生 {record.OperationType} 变更";
        }

        return $"实体 {record.EntityType} 执行 {record.OperationType}";
    }
}
