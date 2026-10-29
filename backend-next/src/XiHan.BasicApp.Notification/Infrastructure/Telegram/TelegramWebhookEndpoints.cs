// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// Telegram 的 Webhook 端点。
/// </summary>
/// <remarks>
/// <c>POST /api/telegram-bot/webhook/{机器人名称}</c>，绕过内核管道（Telegram 匿名调用，凭请求头的密钥认）。
/// 没配密钥、请求头 <c>X-Telegram-Bot-Api-Secret-Token</c> 缺失或不等（定长比较、区分大小写）即 401；其余（JSON 坏、机器人没在运行、正常）一律 200 无体，
/// 正常的放到后台处理后立即返回（4.x 同）。不进接口文档。
/// </remarks>
public static class TelegramWebhookEndpoints
{
    /// <summary>
    /// 密钥请求头。
    /// </summary>
    public const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// 映射端点。
    /// </summary>
    /// <param name="endpoints">端点路由。</param>
    /// <returns>端点路由。</returns>
    public static IEndpointRouteBuilder MapTelegramWebhook(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapPost(TelegramSettings.RoutePrefix + "/{botName}", ReceiveAsync).ExcludeFromDescription());
        return endpoints;
    }

    private static async Task<IResult> ReceiveAsync(HttpContext http, string botName, TelegramRuntime runtime, TelegramBotManager manager)
    {
        var secret = runtime.Settings.SecretToken;
        if (string.IsNullOrEmpty(secret) || !Matches(http.Request.Headers[SecretHeader].ToString(), secret))
        {
            return Results.Unauthorized();
        }

        TelegramUpdate? update;
        try
        {
            using var json = await JsonDocument.ParseAsync(http.Request.Body, cancellationToken: http.RequestAborted).ConfigureAwait(false);
            update = TelegramUpdate.Parse(json.RootElement);
        }
        catch (JsonException)
        {
            return Results.Ok();
        }

        if (update is not null && runtime.Find(Uri.UnescapeDataString(botName)) is { } bot)
        {
            manager.QueueDispatch(bot, update);
        }

        return Results.Ok();
    }

    private static bool Matches(string provided, string expected)
    {
        var left = Encoding.UTF8.GetBytes(provided);
        var right = Encoding.UTF8.GetBytes(expected);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
