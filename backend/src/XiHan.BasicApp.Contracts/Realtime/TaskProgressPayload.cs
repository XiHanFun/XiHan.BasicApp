// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Realtime;

/// <summary>
/// 后台任务进度的推送载荷
/// </summary>
/// <remarks>
/// 客户端方法 <see cref="RealtimeMethods.TaskProgress"/>，只推给发起人。前端灵动岛按 <paramref name="TaskId"/> 复用同一条，进行中常驻，成功或失败后自动收起
/// 字段与状态值即前端 <c>ServerTaskProgressPayload</c> 认的那几个；文案由服务端按发起人的区域翻好
/// </remarks>
/// <param name="TaskId">任务标识</param>
/// <param name="Label">给人看的文案</param>
/// <param name="Detail">副文本</param>
/// <param name="State">状态：<c>loading</c>、<c>success</c> 或 <c>error</c></param>
/// <param name="Progress">进度 0 到 100，为空是不确定态</param>
/// <param name="Link">点击跳转的路由</param>
public sealed record TaskProgressPayload(string TaskId, string Label, string? Detail, string State, int? Progress, string? Link)
{
    /// <summary>
    /// 进行中
    /// </summary>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">文案</param>
    /// <param name="detail">副文本</param>
    /// <param name="progress">进度，为空是不确定态</param>
    /// <returns>载荷</returns>
    public static TaskProgressPayload Running(string taskId, string label, string? detail = null, int? progress = null)
        => new(taskId, label, detail, "loading", progress, null);

    /// <summary>
    /// 成功
    /// </summary>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">文案</param>
    /// <param name="detail">副文本</param>
    /// <param name="link">点击跳转的路由</param>
    /// <returns>载荷</returns>
    public static TaskProgressPayload Succeeded(string taskId, string label, string? detail, string? link)
        => new(taskId, label, detail, "success", 100, link);

    /// <summary>
    /// 失败
    /// </summary>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">文案</param>
    /// <param name="detail">失败原因</param>
    /// <returns>载荷</returns>
    public static TaskProgressPayload Failed(string taskId, string label, string? detail)
        => new(taskId, label, detail, "error", null, null);
}
