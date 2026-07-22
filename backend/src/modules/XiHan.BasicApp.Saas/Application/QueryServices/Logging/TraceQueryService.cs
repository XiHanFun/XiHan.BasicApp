// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 链路追踪查询应用服务
/// 按维度（用户名 / 会话标识 / TraceId / IP / 用户主键）+ 时间范围跨多类日志聚合成一条倒序时间线
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "链路追踪")]
public sealed class TraceQueryService
    : SaasApplicationService, ITraceQueryService
{
    /// <summary>
    /// 时间窗口上限（天），防止分表扫描过多月表
    /// </summary>
    private const int MaxWindowDays = 31;

    /// <summary>
    /// 单类型默认返回条数
    /// </summary>
    private const int DefaultMaxPerType = 200;

    /// <summary>
    /// 单类型返回条数上限
    /// </summary>
    private const int MaxPerTypeCap = 500;

    /// <summary>
    /// 维度取值最大长度
    /// </summary>
    private const int MaxValueLength = 200;

    private readonly ISqlSugarClientResolver _clientResolver;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TraceQueryService(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 按维度跨多类日志聚合链路追踪时间线（时间倒序）
    /// </summary>
    /// <param name="input">追踪查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>链路追踪时间线结果</returns>
    [PermissionAuthorize(SaasPermissionCodes.LogTrace.Read)]
    [HttpPost]
    public async Task<TraceTimelineResultDto> GetTraceTimelineAsync(TraceTimelineQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var value = input.Value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("追踪值不能为空。", nameof(input));
        }

        if (value.Length > MaxValueLength)
        {
            throw new ArgumentOutOfRangeException(nameof(input), $"追踪值长度不能超过 {MaxValueLength}。");
        }

        if (!Enum.IsDefined(input.Dimension))
        {
            throw new ArgumentOutOfRangeException(nameof(input.Dimension), "追踪维度无效。");
        }

        if (!input.StartTime.HasValue || !input.EndTime.HasValue)
        {
            throw new ArgumentException("必须指定时间范围。", nameof(input));
        }

        if (input.StartTime.Value > input.EndTime.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.StartTime), "开始时间不能晚于结束时间。");
        }

        if ((input.EndTime.Value - input.StartTime.Value).TotalDays > MaxWindowDays)
        {
            throw new ArgumentOutOfRangeException(nameof(input.EndTime), $"时间范围不能超过 {MaxWindowDays} 天。");
        }

        // 用户主键维度：取值必须是合法 long，否则视为参数错误
        long? userId = null;
        if (input.Dimension == TraceDimension.UserId)
        {
            if (!long.TryParse(value, out var parsedUserId))
            {
                throw new ArgumentException("用户主键必须为数字。", nameof(input));
            }

            userId = parsedUserId;
        }

        var start = input.StartTime.Value;
        var end = input.EndTime.Value;
        var maxPerType = input.MaxPerType is > 0 and <= MaxPerTypeCap ? input.MaxPerType : DefaultMaxPerType;

        var types = input.LogTypes is { Count: > 0 }
            ? input.LogTypes.Distinct().Where(t => Enum.IsDefined(t)).ToList()
            : [.. Enum.GetValues<TraceLogType>()];

        var items = new List<TraceTimelineItemDto>();
        var typeCounts = new Dictionary<string, int>();
        var truncated = false;

        foreach (var type in types)
        {
            var list = await QueryByTypeAsync(type, input.Dimension, value, userId, start, end, maxPerType, cancellationToken);
            if (list.Count == 0)
            {
                continue;
            }

            items.AddRange(list);
            typeCounts[type.ToString()] = list.Count;
            if (list.Count >= maxPerType)
            {
                truncated = true;
            }
        }

        var ordered = items.OrderByDescending(x => x.Time).ToList();
        return new TraceTimelineResultDto
        {
            Items = ordered,
            TypeCounts = typeCounts,
            Truncated = truncated,
            TotalCount = ordered.Count
        };
    }

    /// <summary>
    /// 按日志类型分派查询
    /// </summary>
    private Task<List<TraceTimelineItemDto>> QueryByTypeAsync(
        TraceLogType type, TraceDimension dim, string value, long? userId,
        DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        return type switch
        {
            TraceLogType.Access => QueryAccessAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.Api => QueryApiAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.Operation => QueryOperationAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.Login => QueryLoginAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.Exception => QueryExceptionAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.Diff => QueryDiffAsync(dim, value, userId, start, end, max, ct),
            TraceLogType.PermissionChange => QueryPermissionChangeAsync(dim, value, userId, start, end, max, ct),
            _ => Task.FromResult(new List<TraceTimelineItemDto>())
        };
    }

    private async Task<List<TraceTimelineItemDto>> QueryAccessAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysAccessLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.UserSessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.AccessIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysAccessLog>()
            .Where(x => x.AccessTime >= start && x.AccessTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.AccessTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromAccess);
    }

    private async Task<List<TraceTimelineItemDto>> QueryApiAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysOpenApiLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.UserSessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.RequestIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysOpenApiLog>()
            .Where(x => x.RequestTime >= start && x.RequestTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.RequestTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromApi);
    }

    private async Task<List<TraceTimelineItemDto>> QueryOperationAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysOperationLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.UserSessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.OperationIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysOperationLog>()
            .Where(x => x.OperationTime >= start && x.OperationTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.OperationTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromOperation);
    }

    private async Task<List<TraceTimelineItemDto>> QueryLoginAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysLoginLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.SessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.LoginIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysLoginLog>()
            .Where(x => x.LoginTime >= start && x.LoginTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.LoginTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromLogin);
    }

    private async Task<List<TraceTimelineItemDto>> QueryExceptionAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysExceptionLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.SessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.OperationIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysExceptionLog>()
            .Where(x => x.ExceptionTime >= start && x.ExceptionTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.ExceptionTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromException);
    }

    private async Task<List<TraceTimelineItemDto>> QueryDiffAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        Expression<Func<SysDiffLog, bool>>? predicate = dim switch
        {
            TraceDimension.UserName => x => x.UserName == value,
            TraceDimension.SessionId => x => x.SessionId == value,
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.OperationIp == value,
            TraceDimension.UserId => x => x.UserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysDiffLog>()
            .Where(x => x.AuditTime >= start && x.AuditTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.AuditTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromDiff);
    }

    private async Task<List<TraceTimelineItemDto>> QueryPermissionChangeAsync(TraceDimension dim, string value, long? userId, DateTimeOffset start, DateTimeOffset end, int max, CancellationToken ct)
    {
        // 权限变更日志无用户名/会话维度：仅 TraceId / IP / 用户主键（操作人）适用
        Expression<Func<SysPermissionChangeLog, bool>>? predicate = dim switch
        {
            TraceDimension.TraceId => x => x.TraceId == value,
            TraceDimension.Ip => x => x.OperationIp == value,
            TraceDimension.UserId => x => x.OperatorUserId == userId,
            _ => null
        };
        if (predicate is null)
        {
            return [];
        }

        var list = await DbClient.Queryable<SysPermissionChangeLog>()
            .Where(x => x.ChangeTime >= start && x.ChangeTime <= end)
            .Where(predicate)
            .SplitTable()
            .OrderByDescending(x => x.ChangeTime)
            .Take(max)
            .ToListAsync(ct);
        return list.ConvertAll(TraceApplicationMapper.FromPermissionChange);
    }
}
