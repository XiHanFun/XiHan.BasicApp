// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 引擎标识与契约标识的换算
/// </summary>
/// <remarks>
/// 引擎的定义、实例、节点实例与书签 ID 都是 UUID v7 的 <c>N</c> 格式（32 位十六进制），契约里按 <see cref="Guid"/> 传
/// 用户在引擎里（发起人、受理人、办理人）按 <see cref="Guid"/> 的缺省格式写，与令牌的主体 ID 一致
/// </remarks>
public static class WorkflowIds
{
    /// <summary>
    /// 引擎 ID 转契约 ID
    /// </summary>
    /// <param name="id">引擎 ID</param>
    /// <returns>契约 ID</returns>
    public static Guid ToContract(string id) => Guid.Parse(id);

    /// <summary>
    /// 可空的引擎 ID 转契约 ID
    /// </summary>
    /// <param name="id">引擎 ID</param>
    /// <returns>契约 ID</returns>
    public static Guid? ToContractOrNull(string? id) => id is null ? null : Guid.Parse(id);

    /// <summary>
    /// 契约 ID 转引擎 ID
    /// </summary>
    /// <param name="id">契约 ID</param>
    /// <returns>引擎 ID</returns>
    public static string ToEngine(Guid id) => id.ToString("N");

    /// <summary>
    /// 用户 ID 在引擎里的写法
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns>引擎里的用户标识</returns>
    public static string ToEngineUser(Guid userId) => userId.ToString();

    /// <summary>
    /// 引擎里的用户标识转用户 ID
    /// </summary>
    /// <param name="value">引擎里的用户标识</param>
    /// <returns>用户 ID，不是本系统的用户时为空</returns>
    public static Guid? UserOf(string? value) => Guid.TryParse(value, out var id) ? id : null;
}

/// <summary>
/// 调用方的一侧与身份
/// </summary>
/// <param name="Side">一侧：平台为 <c>platform</c>，否则是租户 ID</param>
/// <param name="UserId">调用方的用户 ID（主体不是本系统的用户时为空）</param>
public sealed record WorkflowCaller(string Side, Guid? UserId)
{
    /// <summary>
    /// 引擎里的调用方标识（发起人、办理人）
    /// </summary>
    public string? EngineUser => UserId is { } id ? WorkflowIds.ToEngineUser(id) : null;

    /// <summary>
    /// 从调用上下文取
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <returns>调用方</returns>
    public static WorkflowCaller Of(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new WorkflowCaller(SideRows.SideOf(context), WorkflowIds.UserOf(context.GetSecurity().Principal.Id));
    }
}

/// <summary>
/// 请求里 JSON 文本的读取
/// </summary>
public static class WorkflowJsonText
{
    /// <summary>
    /// 读一个 JSON 对象
    /// </summary>
    /// <remarks>
    /// 空白即没有；不是 JSON 对象即 <see cref="WorkflowErrors.JsonObjectRequired"/>
    /// </remarks>
    /// <param name="text">JSON 文本</param>
    /// <param name="field">字段名（错误消息用）</param>
    /// <returns>对象，没有时为空</returns>
    public static Result<Maybe<JsonObject>> ReadObject(string? text, string field)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Maybe.None<JsonObject>();
        }

        try
        {
            return JsonNode.Parse(text) is JsonObject value
                ? Maybe.Some(value)
                : Error.Validation(WorkflowErrors.JsonObjectRequired, WorkflowMessages.JsonObjectRequired, MessageArguments.Of(("field", field)));
        }
        catch (JsonException)
        {
            return Error.Validation(WorkflowErrors.JsonObjectRequired, WorkflowMessages.JsonObjectRequired, MessageArguments.Of(("field", field)));
        }
    }
}
