// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Auditing;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 实体差异日志写入器
/// </summary>
public class SaasEntityDiffLogWriter : IEntityDiffLogWriter
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly IClientInfoProvider _clientInfoProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasEntityDiffLogWriter(
        ISqlSugarClientResolver clientResolver,
        IClientInfoProvider clientInfoProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _clientResolver = clientResolver;
        _clientInfoProvider = clientInfoProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 写入实体差异日志
    /// </summary>
    public async Task WriteAsync(EntityDiffLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var clientInfo = _clientInfoProvider.GetCurrent();
        var operationType = SaasLogMappingHelper.ResolveOperationTypeByName(record.OperationType);
        var requestId = SaasLogMappingHelper.TrimOrNull(record.RequestId, 100);
        var entityTypeName = SaasLogMappingHelper.TrimOrDefault(record.EntityType, 100, "UnknownEntity");
        var entityId = SaasLogMappingHelper.TrimOrNull(record.EntityId, 100);
        var sessionId = _httpContextAccessor.HttpContext?.Features.Get<ISessionFeature>()?.Session?.Id;

        var entity = new SysDiffLog
        {
            TenantId = record.TenantId ?? 0,
            UserId = record.UserId,
            UserName = SaasLogMappingHelper.TrimOrNull(record.UserName, 50),
            AuditType = SaasLogMappingHelper.TrimOrDefault(record.AuditType, 50, "EntityChange"),
            OperationType = operationType,
            EntityType = entityTypeName,
            EntityId = entityId,
            EntityName = entityTypeName,
            PrimaryKey = nameof(IEntityBase<>.BasicId),
            PrimaryKeyValue = entityId,
            TableName = entityTypeName,
            Description = SaasLogMappingHelper.TrimOrNull(BuildDescription(record, entityTypeName, entityId), 500),
            BeforeData = SaasLogMappingHelper.TrimOrNull(record.BeforeData, 32000),
            AfterData = SaasLogMappingHelper.TrimOrNull(record.AfterData, 32000),
            ChangedFields = SaasLogMappingHelper.TrimOrNull(record.ChangedFields, 32000),
            ChangeDescription = SaasLogMappingHelper.TrimOrNull(BuildChangeDescription(record), 1000),
            OperationIp = SaasLogMappingHelper.TrimOrNull(clientInfo.IpAddress ?? record.OperationIp, 50),
            SessionId = SaasLogMappingHelper.TrimOrNull(sessionId, 100),
            RequestId = requestId,
            TraceId = SaasLogMappingHelper.TrimOrNull(requestId, 64),
            IsSuccess = true,
            RiskLevel = (AuditRiskLevel)SaasLogMappingHelper.ResolveRiskLevel(operationType),
            AuditTime = DateTimeOffset.UtcNow
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    private static string BuildDescription(EntityDiffLogRecord record, string entityTypeName, string? entityId)
    {
        return entityId is null
            ? $"{record.OperationType}:{entityTypeName}"
            : $"{record.OperationType}:{entityTypeName}#{entityId}";
    }

    private static string BuildChangeDescription(EntityDiffLogRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.ChangedFields))
        {
            return $"实体 {record.EntityType} 发生 {record.OperationType} 变更";
        }

        return $"实体 {record.EntityType} 执行 {record.OperationType}";
    }
}
