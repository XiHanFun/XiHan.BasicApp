// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using Grpc.Core;
using XiHan.Framework.Plugins.Protocol.V1;
using Status = XiHan.Framework.Plugins.Protocol.V1.Status;

namespace CrmPlugin;

/// <summary>
/// 插件服务：握手、生命周期、健康与三个客户操作
/// </summary>
/// <remarks>
/// 操作的租户取宿主随调用交来的 Baggage <c>xihan.tenant</c>；失败只给错误码、消息键与参数，文案由宿主按握手的消息目录翻译成调用方的语言
/// </remarks>
/// <param name="state">生命周期状态</param>
/// <param name="customers">客户</param>
/// <param name="lifetime">进程生命周期</param>
internal sealed class CrmPluginService(PluginState state, CustomerStore customers, IHostApplicationLifetime lifetime) : Plugin.PluginBase
{
    /// <summary>
    /// 列出客户
    /// </summary>
    public const string ListOperation = "crm-customers.list";

    /// <summary>
    /// 新建客户
    /// </summary>
    public const string CreateOperation = "crm-customers.create";

    /// <summary>
    /// 修改客户
    /// </summary>
    public const string UpdateOperation = "crm-customers.update";

    /// <summary>
    /// 删除客户
    /// </summary>
    public const string DeleteOperation = "crm-customers.delete";

    /// <summary>
    /// 租户的 Baggage 键
    /// </summary>
    private const string TenantKey = "xihan.tenant";

    /// <summary>
    /// 一个客户的 JSON Schema
    /// </summary>
    private const string CustomerSchema = """{"type":"object","properties":{"id":{"type":"string","format":"uuid"},"name":{"type":"string"},"email":{"type":["string","null"]}},"required":["id","name"]}""";

    /// <inheritdoc />
    public override Task<HandshakeResponse> Handshake(HandshakeRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.ProtocolVersions.Contains(1u))
        {
            throw new RpcException(new Grpc.Core.Status(StatusCode.FailedPrecondition, "No common protocol version."));
        }

        if (!state.TryAdvance(LifecycleState.Declared, LifecycleState.Resolved))
        {
            throw OutOfOrder("Handshake");
        }

        var response = new HandshakeResponse
        {
            ProtocolVersion = 1,
            Primitives = 1,
            Manifest = state.ManifestJson,
            Sdk = new SdkInfo { Name = "CrmPlugin", Version = "1.0.0", Language = "csharp" },
        };
        response.Operations.Add(new OperationDeclaration
        {
            Id = ListOperation,
            Stability = "experimental",
            Expose = { "http" },
            Permission = "crm.crm-customers.read",
            RequestSchema = """{"type":"object","properties":{}}""",
            ResponseSchema = $$$"""{"type":"object","properties":{"items":{"type":"array","items":{{{CustomerSchema}}}}},"required":["items"]}""",
        });
        response.Operations.Add(new OperationDeclaration
        {
            Id = CreateOperation,
            Stability = "experimental",
            Expose = { "http" },
            Permission = "crm.crm-customers.create",
            RequestSchema = """{"type":"object","properties":{"name":{"type":"string","maxLength":64},"email":{"type":["string","null"],"maxLength":128}},"required":["name"]}""",
            ResponseSchema = CustomerSchema,
        });
        response.Operations.Add(new OperationDeclaration
        {
            Id = UpdateOperation,
            Stability = "experimental",
            Expose = { "http" },
            Permission = "crm.crm-customers.update",
            RequestSchema = """{"type":"object","properties":{"id":{"type":"string","format":"uuid"},"name":{"type":"string","maxLength":64},"email":{"type":["string","null"],"maxLength":128}},"required":["id","name"]}""",
            ResponseSchema = CustomerSchema,
        });
        response.Operations.Add(new OperationDeclaration
        {
            Id = DeleteOperation,
            Stability = "experimental",
            Expose = { "http" },
            Permission = "crm.crm-customers.delete",
            RequestSchema = """{"type":"object","properties":{"id":{"type":"string","format":"uuid"}},"required":["id"]}""",
            ResponseSchema = """{"type":"object","properties":{}}""",
        });
        response.Catalogs.Add(CrmCatalog.Create());
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public override Task<ConfigureResponse> Configure(ConfigureRequest request, ServerCallContext context)
        => state.TryAdvance(LifecycleState.Resolved, LifecycleState.Configured)
            ? Task.FromResult(new ConfigureResponse { Status = Ok() })
            : throw OutOfOrder("Configure");

    /// <inheritdoc />
    public override Task<StartResponse> Start(StartRequest request, ServerCallContext context)
        => state.TryAdvance(LifecycleState.Configured, LifecycleState.Ready)
            ? Task.FromResult(new StartResponse { Status = Ok() })
            : throw OutOfOrder("Start");

    /// <inheritdoc />
    public override Task<StopResponse> Stop(StopRequest request, ServerCallContext context)
    {
        if (!state.TryStop())
        {
            throw OutOfOrder("Stop");
        }

        // 先应答，再退出进程
        _ = Task.Run(async () =>
        {
            await Task.Delay(100).ConfigureAwait(false);
            lifetime.StopApplication();
        });
        return Task.FromResult(new StopResponse { Status = Ok() });
    }

    /// <inheritdoc />
    public override Task<HealthResponse> Health(HealthRequest request, ServerCallContext context) => Task.FromResult(CurrentHealth());

    /// <inheritdoc />
    public override async Task WatchHealth(HealthRequest request, IServerStreamWriter<HealthResponse> responseStream, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(responseStream);
        ArgumentNullException.ThrowIfNull(context);
        // 健康只在启停时变化：先发一次当前状态，之后保持连接直到宿主取消或进程停止
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        await responseStream.WriteAsync(CurrentHealth(), stopping.Token).ConfigureAwait(false);
        try
        {
            await Task.Delay(Timeout.Infinite, stopping.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 宿主取消或进程停止：正常结束流
        }
    }

    /// <inheritdoc />
    public override Task<InvokeResponse> Invoke(InvokeRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Operation is not (ListOperation or CreateOperation or UpdateOperation or DeleteOperation))
        {
            throw new RpcException(new Grpc.Core.Status(StatusCode.Unimplemented, $"Operation '{request.Operation}' is not served by this plugin."));
        }

        if (state.State != LifecycleState.Ready)
        {
            return Task.FromResult(new InvokeResponse { Status = Failure("PLUGINS_UNAVAILABLE_001", "plugins.process.unavailable", new JsonObject { ["id"] = "xihan.samples.crm", ["state"] = state.State.ToString() }) });
        }

        var tenant = request.Metadata?.Baggage.TryGetValue(TenantKey, out var value) == true ? value : string.Empty;
        var payload = request.Payload.IsEmpty ? [] : JsonNode.Parse(request.Payload.ToStringUtf8())?.AsObject() ?? [];
        return Task.FromResult(request.Operation switch
        {
            ListOperation => Success(new JsonObject { ["items"] = new JsonArray([.. customers.List(tenant).Select(ToJson)]) }),
            CreateOperation => Create(tenant, payload),
            UpdateOperation => Update(tenant, payload),
            _ => Delete(tenant, payload),
        });
    }

    /// <summary>
    /// 新建客户：名称去掉首尾空白后不能为空
    /// </summary>
    private InvokeResponse Create(string tenant, JsonObject payload)
    {
        var name = (payload["name"]?.GetValue<string>() ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return new InvokeResponse { Status = Failure("CRM_VALIDATION_001", "crm.customer.name_required", null) };
        }

        var email = payload["email"]?.GetValue<string>();
        return Success(ToJson(customers.Create(tenant, name, string.IsNullOrWhiteSpace(email) ? null : email.Trim())));
    }

    /// <summary>
    /// 修改客户：只改得到本租户的，名称同样不能为空
    /// </summary>
    private InvokeResponse Update(string tenant, JsonObject payload)
    {
        var text = payload["id"]?.GetValue<string>() ?? string.Empty;
        var name = (payload["name"]?.GetValue<string>() ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return new InvokeResponse { Status = Failure("CRM_VALIDATION_001", "crm.customer.name_required", null) };
        }

        var email = payload["email"]?.GetValue<string>();
        return Guid.TryParse(text, out var id) && customers.Update(tenant, id, name, string.IsNullOrWhiteSpace(email) ? null : email.Trim()) is { } customer
            ? Success(ToJson(customer))
            : new InvokeResponse { Status = Failure("CRM_NOTFOUND_001", "crm.customer.not_found", new JsonObject { ["id"] = text }) };
    }

    /// <summary>
    /// 删除客户：只删得掉本租户的
    /// </summary>
    private InvokeResponse Delete(string tenant, JsonObject payload)
    {
        var text = payload["id"]?.GetValue<string>() ?? string.Empty;
        return Guid.TryParse(text, out var id) && customers.Delete(tenant, id)
            ? Success(new JsonObject())
            : new InvokeResponse { Status = Failure("CRM_NOTFOUND_001", "crm.customer.not_found", new JsonObject { ["id"] = text }) };
    }

    /// <summary>
    /// 当前健康：状态即插件的生命周期状态，健康项 <c>crm</c> 可服务时健康
    /// </summary>
    private HealthResponse CurrentHealth()
    {
        var current = state.State;
        var response = new HealthResponse { State = current };
        response.Items.Add(new HealthItem { Name = "crm", Status = current == LifecycleState.Ready ? HealthStatus.Healthy : HealthStatus.Unhealthy });
        return response;
    }

    /// <summary>
    /// 客户的 JSON
    /// </summary>
    private static JsonNode ToJson(Customer customer)
        => new JsonObject { ["id"] = customer.Id.ToString("D"), ["name"] = customer.Name, ["email"] = customer.Email };

    /// <summary>
    /// 次序不对的生命周期调用，以 <c>FailedPrecondition</c> 拒绝、状态不变
    /// </summary>
    private RpcException OutOfOrder(string rpc)
        => new(new Grpc.Core.Status(StatusCode.FailedPrecondition, $"'{rpc}' is not accepted in state '{state.State}'."));

    /// <summary>
    /// 成功状态
    /// </summary>
    private static Status Ok() => new() { Ok = true };

    /// <summary>
    /// 成功应答，载荷是 JSON 对象
    /// </summary>
    private static InvokeResponse Success(JsonNode payload)
        => new() { Status = Ok(), Payload = ByteString.CopyFrom(Encoding.UTF8.GetBytes(payload.ToJsonString())) };

    /// <summary>
    /// 业务失败：错误码、消息键与参数，文案留空由宿主翻译
    /// </summary>
    private static Status Failure(string code, string messageKey, JsonObject? details)
        => new() { Ok = false, Error = new Error { Code = code, MessageKey = messageKey, Details = details?.ToJsonString(JsonSerializerOptions.Default) ?? string.Empty } };
}
