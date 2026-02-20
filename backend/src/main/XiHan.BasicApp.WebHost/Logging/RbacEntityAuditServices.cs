#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacEntityAuditServices
// Guid:ac2a9b0d-a245-466f-94ce-ac7a08bc9cf1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:38:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.Auditing;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.WebHost.Logging;

/// <summary>
/// RBAC 实体审计上下文提供器
/// </summary>
public class RbacEntityAuditContextProvider(
    IHttpContextAccessor httpContextAccessor,
    ICurrentUser currentUser) : IEntityAuditContextProvider
{
    /// <inheritdoc />
    public EntityAuditLogRecord CreateBaseRecord()
    {
        var httpContext = httpContextAccessor.HttpContext;
        return new EntityAuditLogRecord
        {
            AuditType = "EntityChange",
            RequestPath = httpContext?.Request.Path.ToString(),
            RequestMethod = httpContext?.Request.Method,
            OperationIp = httpContext?.Connection.RemoteIpAddress?.ToString(),
            RequestId = httpContext?.TraceIdentifier,
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            TenantId = currentUser.TenantId
        };
    }

    /// <inheritdoc />
    public bool ShouldAudit(Type entityType)
    {
        var ns = entityType.Namespace ?? string.Empty;
        if (!ns.StartsWith("XiHan.BasicApp.Rbac.Entities", StringComparison.Ordinal))
        {
            return false;
        }

        // 避免日志表自审计造成噪声
        return !entityType.Name.EndsWith("Log", StringComparison.Ordinal);
    }
}

/// <summary>
/// RBAC 实体审计日志写入器
/// </summary>
public class RbacEntityAuditLogWriter(
    ISysAuditLogRepository repository) : IEntityAuditLogWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(EntityAuditLogRecord record, CancellationToken cancellationToken = default)
    {
        var entity = new SysAuditLog
        {
            UserId = record.UserId,
            UserName = record.UserName,
            AuditType = record.AuditType,
            OperationType = MapOperationType(record.OperationType),
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            BeforeData = record.BeforeData,
            AfterData = record.AfterData,
            ChangedFields = record.ChangedFields,
            RequestPath = record.RequestPath,
            RequestMethod = record.RequestMethod,
            OperationIp = record.OperationIp,
            RequestId = record.RequestId,
            AuditTime = DateTimeOffset.Now,
            IsSuccess = true,
            TenantId = record.TenantId
        };

        await repository.SaveAsync(entity, cancellationToken);
    }

    private static OperationType MapOperationType(string operationType)
    {
        return operationType switch
        {
            "Create" => OperationType.Create,
            "Update" => OperationType.Update,
            "Delete" => OperationType.Delete,
            _ => OperationType.Other
        };
    }
}
