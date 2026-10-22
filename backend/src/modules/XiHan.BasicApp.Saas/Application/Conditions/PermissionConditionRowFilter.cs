// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Application.Contracts.Enums;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Web.Api.Constants;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 数据行属性的补算：模型绑定之后按登记取主键、读数据行，把鉴权时待定的权限条件算完
/// </summary>
/// <remarks>
/// 一次操作多条（批量主键）时逐行算，每一行都要满足；取不到主键或读不到行时行属性取不到，按不满足处理。
/// 排在全部动作过滤器之前，不满足时直接返回 403，不进入动作。
/// </remarks>
public sealed class PermissionConditionRowFilter : IAsyncActionFilter
{
    /// <summary>
    /// 过滤器顺序：先于字段安全与模型校验
    /// </summary>
    public const int FilterOrder = -20_000;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IPermissionResourceRegistry _resources;
    private readonly PermissionConditionDecider _decider;
    private readonly IPermissionResourceRowReader _rowReader;
    private readonly IPermissionConditionHitRecorder _hitRecorder;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionConditionRowFilter(
        IPermissionResourceRegistry resources,
        PermissionConditionDecider decider,
        IPermissionResourceRowReader rowReader,
        IPermissionConditionHitRecorder hitRecorder)
    {
        _resources = resources;
        _decider = decider;
        _rowReader = rowReader;
        _hitRecorder = hitRecorder;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        foreach (var pending in PendingPermissionConditions.Take(httpContext))
        {
            if (await IsSatisfiedAsync(pending, context.ActionArguments.AsReadOnly(), cancellationToken))
            {
                continue;
            }

            await _hitRecorder.RecordAsync(
                PermissionConditionEnforcer.CreateHit(pending.UserId, pending.TenantId, pending.PermissionCode, pending.Mode, httpContext),
                cancellationToken);
            if (pending.Mode == PermissionConditionMode.Enforce)
            {
                await WriteForbiddenAsync(httpContext);
                context.Result = new EmptyResult();
                return;
            }
        }

        await next();
    }

    private async Task<bool> IsSatisfiedAsync(PendingPermissionCondition pending, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        if (!_resources.TryGet(pending.PermissionCode, out var registration))
        {
            return false;
        }

        var keys = (registration.KeySelector(arguments) ?? []).Where(key => key > 0).Distinct().ToList();
        var rows = (await _rowReader.ReadAsync(registration, keys, cancellationToken))
            .ToDictionary(PermissionResourceRegistration.KeyOf);
        IEnumerable<object?> targets = keys.Count == 0 ? [null] : keys.Select(key => rows.GetValueOrDefault(key));
        foreach (var row in targets)
        {
            var outcome = await _decider.DecideAsync(
                pending.Conditions,
                pending.PermissionCode,
                PermissionRowAttributeSource.WithRow(pending.RequestSource, registration, row),
                cancellationToken);
            if (outcome != PermissionConditionOutcome.Granted)
            {
                return false;
            }
        }

        return true;
    }

    private static async Task WriteForbiddenAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        var payload = new ApiResponse
        {
            Code = ApiResponseCodes.Forbidden,
            Message = PermissionCheckResult.ConditionNotMet.FailureMessage ?? "当前条件下不允许此操作",
            TraceId = httpContext.Items[XiHanWebApiConstants.TraceIdItemKey]?.ToString() ?? httpContext.TraceIdentifier
        };
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions), httpContext.RequestAborted);
    }
}
