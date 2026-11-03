// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 仿真 OpenAI 兼容端点
/// </summary>
/// <remarks>
/// 照框架 OpenAI 一致性套件的仿真端点：<c>/chat/completions</c>（可流式）与 <c>/embeddings</c>，记录全部请求；
/// 经框架 OpenAI 工厂的命名 HttpClient <c>xihan.ai.openai</c> 换主处理器接入宿主，不触网。真实的大模型不在缺省测试里
/// </remarks>
public sealed class FakeOpenAIHandler : HttpMessageHandler
{
    /// <summary>
    /// 框架 OpenAI 工厂的命名 HttpClient
    /// </summary>
    public const string HttpClientName = "xihan.ai.openai";

    /// <summary>
    /// 仿真端点的地址
    /// </summary>
    public const string Endpoint = "http://fake-openai.test/v1";

    /// <summary>
    /// 缺省的嵌入维度
    /// </summary>
    public const int DefaultDimensions = 8;

    private readonly Lock _gate = new();
    private readonly List<(string Path, JsonNode Body)> _requests = [];

    /// <summary>
    /// 最终应答
    /// </summary>
    public string Reply { get; set; } = "pong";

    /// <summary>
    /// 嵌入维度
    /// </summary>
    public int Dimensions { get; set; } = DefaultDimensions;

    /// <summary>
    /// 要模型调用的工具：流式请求里还没有工具结果时先回工具调用，收到结果之后才回 <see cref="Reply"/>
    /// </summary>
    public (string Name, string Arguments)? ToolCall { get; set; }

    /// <summary>
    /// 为真时一律以 500 应答
    /// </summary>
    public bool Failing { get; set; }

    /// <summary>
    /// 收到的请求：(路径, 正文)
    /// </summary>
    public IReadOnlyList<(string Path, JsonNode Body)> Requests
    {
        get
        {
            lock (_gate)
            {
                return [.. _requests];
            }
        }
    }

    /// <summary>
    /// 把这个仿真端点接进宿主
    /// </summary>
    /// <param name="builder">宿主构建器</param>
    public void Attach(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // 排在产品代码的登记之后才生效：AI 包也给这个客户端配了连接处理器
        builder.Services.PostConfigure<HttpClientFactoryOptions>(HttpClientName, options => options.HttpMessageHandlerBuilderActions.Add(handlers => handlers.PrimaryHandler = this));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false))!;
        lock (_gate)
        {
            _requests.Add((path, body));
        }

        if (Failing)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("""{"error":{"message":"fake failure","type":"server_error"}}""", Encoding.UTF8, "application/json") };
        }

        if (path.EndsWith("/embeddings", StringComparison.Ordinal))
        {
            return Json(Embeddings(body));
        }

        if (!path.EndsWith("/chat/completions", StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        if (body["stream"]?.GetValue<bool>() != true)
        {
            return Json(Completion(Reply));
        }

        var answered = body["messages"]?.AsArray().Any(static m => m?["role"]?.GetValue<string>() == "tool") ?? false;
        return ToolCall is { } call && !answered ? SseToolCall(call.Name, call.Arguments) : Sse(Reply);
    }

    /// <summary>
    /// 非流式应答
    /// </summary>
    private static string Completion(string content) => new JsonObject
    {
        ["id"] = "chatcmpl-1",
        ["object"] = "chat.completion",
        ["created"] = 0,
        ["model"] = "fake",
        ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content }, ["finish_reason"] = "stop" }),
        ["usage"] = new JsonObject { ["prompt_tokens"] = 1, ["completion_tokens"] = 1, ["total_tokens"] = 2 },
    }.ToJsonString();

    /// <summary>
    /// 流式应答：两个增量与结束标记
    /// </summary>
    private static HttpResponseMessage Sse(string content)
    {
        var builder = new StringBuilder();
        foreach (var piece in new[] { content[..(content.Length / 2)], content[(content.Length / 2)..] })
        {
            var chunk = new JsonObject
            {
                ["id"] = "chatcmpl-1",
                ["object"] = "chat.completion.chunk",
                ["created"] = 0,
                ["model"] = "fake",
                ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["delta"] = new JsonObject { ["role"] = "assistant", ["content"] = piece }, ["finish_reason"] = null }),
            };
            builder.Append("data: ").Append(chunk.ToJsonString()).Append("\n\n");
        }

        builder.Append("data: [DONE]\n\n");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream") };
    }

    /// <summary>
    /// 流式应答：一次工具调用
    /// </summary>
    private static HttpResponseMessage SseToolCall(string name, string arguments)
    {
        var builder = new StringBuilder();
        var call = new JsonObject
        {
            ["id"] = "chatcmpl-1",
            ["object"] = "chat.completion.chunk",
            ["created"] = 0,
            ["model"] = "fake",
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                ["delta"] = new JsonObject
                {
                    ["role"] = "assistant",
                    ["tool_calls"] = new JsonArray(new JsonObject { ["index"] = 0, ["id"] = "call_1", ["type"] = "function", ["function"] = new JsonObject { ["name"] = name, ["arguments"] = arguments } }),
                },
                ["finish_reason"] = null,
            }),
        };
        builder.Append("data: ").Append(call.ToJsonString()).Append("\n\n");
        var finish = new JsonObject
        {
            ["id"] = "chatcmpl-1",
            ["object"] = "chat.completion.chunk",
            ["created"] = 0,
            ["model"] = "fake",
            ["choices"] = new JsonArray(new JsonObject { ["index"] = 0, ["delta"] = new JsonObject(), ["finish_reason"] = "tool_calls" }),
        };
        builder.Append("data: ").Append(finish.ToJsonString()).Append("\n\n");
        builder.Append("data: [DONE]\n\n");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream") };
    }

    /// <summary>
    /// 嵌入应答：每个输入一个确定性的向量
    /// </summary>
    private string Embeddings(JsonNode body)
    {
        var inputs = body["input"] is JsonArray array ? array.Select(static item => item!.GetValue<string>()).ToList() : [body["input"]!.GetValue<string>()];
        var data = new JsonArray();
        for (var index = 0; index < inputs.Count; index++)
        {
            var vector = new JsonArray();
            for (var dimension = 0; dimension < Dimensions; dimension++)
            {
                vector.Add((JsonNode?)JsonValue.Create(Math.Round((inputs[index].Length + dimension) / 100d, 4)));
            }

            data.Add((JsonNode?)new JsonObject { ["object"] = "embedding", ["index"] = index, ["embedding"] = vector });
        }

        return new JsonObject
        {
            ["object"] = "list",
            ["data"] = data,
            ["model"] = body["model"]?.GetValue<string>() ?? "fake",
            ["usage"] = new JsonObject { ["prompt_tokens"] = inputs.Count, ["total_tokens"] = inputs.Count },
        }.ToJsonString();
    }

    /// <summary>
    /// JSON 应答
    /// </summary>
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
