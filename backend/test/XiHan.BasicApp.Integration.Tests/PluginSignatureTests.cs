// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Host.Commands;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Plugins;
using XiHan.BasicApp.Plugins.Domain.Packages;
using XiHan.Framework.Kernel.Plugins;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证插件包的发布者签名与可信发布者
/// </summary>
/// <remarks>
/// 不放行时：没有签名 <c>PLUGINS_DENIED_102</c>、公钥不受信任 <c>PLUGINS_DENIED_103</c>；受信任的公钥签过、包没改过才通过；签名与内容对不上或签名文件读不出一律 <c>PLUGINS_VALIDATION_109</c>
/// 放行时没有签名与不受信任的包照装，结论记在版本上；每次加载前按装时的结论与当时的配置再判一次；宿主的 <c>plugins keygen</c> / <c>plugins sign</c> 签出的包受信任
/// </remarks>
public sealed class PluginSignatureTests : IDisposable
{
    private const string Cache = "binding:cache";
    private const string Config = "config:XiHan:Weather";

    private readonly TestDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-plugin-signature-" + Guid.NewGuid().ToString("N"));
    private readonly ECDsa _trusted = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsa _stranger = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    public void Dispose()
    {
        _database.Dispose();
        _trusted.Dispose();
        _stranger.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string TrustedKeyId => PluginSignature.KeyIdOf(_trusted.ExportSubjectPublicKeyInfo());

    /// <summary>
    /// 起一个信任 <see cref="_trusted"/>（发布者 Contoso）的宿主，插件宿主是不起进程的替身
    /// </summary>
    private async Task<(BasicAppTestServer Server, string Platform, StandInPluginHost Host)> StartAsync(bool allowUnsigned, params string[] trustedPublicKeys)
    {
        KeyValuePair<string, string?>[] trust = [.. trustedPublicKeys.SelectMany(static (key, i) => new KeyValuePair<string, string?>[] { new($"BasicApp:Plugins:TrustedPublishers:{i}:Name", "Contoso"), new($"BasicApp:Plugins:TrustedPublishers:{i}:PublicKey", key) })];
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Files:LocalRootPath", Path.Combine(_root, "files")), new("BasicApp:Plugins:CacheDirectory", Path.Combine(_root, "cache")), new("BasicApp:Plugins:AllowUnsigned", allowUnsigned ? "true" : "false"), .. trust],
            configure: static builder => builder.Services.AddKeyedSingleton<IPluginHost>(PluginIsolation.OutOfProcess, static (services, _) => new StandInPluginHost(services.GetRequiredService<XiHan.Framework.Kernel.Operations.OperationRegistry>(), services.GetRequiredService<MessageCatalogRegistry>())));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        return (server, platform, (StandInPluginHost)server.Services.GetRequiredKeyedService<IPluginHost>(PluginIsolation.OutOfProcess));
    }

    private static string PublicKeyOf(ECDsa key) => Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());

    private static byte[] Sign(byte[] package, ECDsa key)
    {
        using var buffer = new MemoryStream();
        buffer.Write(package);
        PluginSignature.SignAsync(buffer, key, CancellationToken.None).GetAwaiter().GetResult();
        return buffer.ToArray();
    }

    /// <summary>
    /// 改包里的一个文件（或加上它）
    /// </summary>
    private static byte[] Rewrite(byte[] package, string entry, string content)
    {
        using var buffer = new MemoryStream();
        buffer.Write(package);
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Update, leaveOpen: true))
        {
            archive.GetEntry(entry)?.Delete();
            using var stream = archive.CreateEntry(entry).Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        return buffer.ToArray();
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> InspectAsync(BasicAppTestServer server, string token, byte[] package)
    {
        var fileId = await PluginsTests.UploadAsync(server, token, package);
        return await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins:inspect?fileId={fileId}", token: token));
    }

    private static async Task AssertRejectedAsync(BasicAppTestServer server, string token, byte[] package, HttpStatusCode status, string code, string because)
    {
        var (response, body) = await InspectAsync(server, token, package);
        Assert.True(response.StatusCode == status && body.GetProperty("code").GetString() == code, $"{because}: {(int)response.StatusCode} {body.GetRawText()}");
    }

    private static async Task<JsonElement> AssertSignatureAsync(BasicAppTestServer server, string token, byte[] package, string status)
    {
        var (response, body) = await InspectAsync(server, token, package);
        Assert.True(response.StatusCode == HttpStatusCode.OK, body.GetRawText());
        var signature = body.GetProperty("signature");
        Assert.Equal(status, signature.GetProperty("status").GetString());
        return signature;
    }

    [Fact]
    public async Task WithoutUnsignedAllowed_OnlyPackagesSignedByATrustedKeyPass()
    {
        var (server, platform, _) = await StartAsync(allowUnsigned: false, PublicKeyOf(_trusted));
        await using var _ = server;
        var package = PluginsTests.Package();

        await AssertRejectedAsync(server, platform, package, HttpStatusCode.Forbidden, PluginsErrors.PackageUnsigned, "unsigned");
        await AssertRejectedAsync(server, platform, Sign(package, _stranger), HttpStatusCode.Forbidden, PluginsErrors.PublisherUntrusted, "signed by an untrusted key");
        // 发布者名也算：同一把公钥，旁挂清单换了发布者就找不到
        await AssertRejectedAsync(server, platform, Sign(PluginsTests.Package(descriptor: static d => d["publisher"] = "Fabrikam"), _trusted), HttpStatusCode.Forbidden, PluginsErrors.PublisherUntrusted, "another publisher");
        // 签过之后改了一个文件、加了一个文件
        var signed = Sign(package, _trusted);
        await AssertRejectedAsync(server, platform, Rewrite(signed, "locales/en-US.json", """{"weather":{"pages":{"forecast":"Changed"},"alerts":{"title":"Title"}}}"""), HttpStatusCode.BadRequest, PluginsErrors.SignatureInvalid, "changed after signing");
        await AssertRejectedAsync(server, platform, Rewrite(signed, "extra.txt", "x"), HttpStatusCode.BadRequest, PluginsErrors.SignatureInvalid, "an entry added after signing");
        await AssertRejectedAsync(server, platform, Rewrite(package, PluginSignature.Entry, """{"algorithm":"rsa-sha256","key":"00","signature":"AA=="}"""), HttpStatusCode.BadRequest, PluginsErrors.SignatureInvalid, "an unreadable signature file");

        var trusted = await AssertSignatureAsync(server, platform, signed, "Trusted");
        Assert.Equal(TrustedKeyId, trusted.GetProperty("keyId").GetString());

        // 装好之后结论记在版本上
        var fileId = await PluginsTests.UploadAsync(server, platform, signed);
        var (installed, body) = await PluginsTests.InstallAsync(server, platform, fileId, Cache, Config);
        Assert.True(installed.StatusCode == HttpStatusCode.OK, body.GetRawText());
        var version = Assert.Single(body.GetProperty("versions").EnumerateArray()).GetProperty("signature");
        Assert.Equal(("Trusted", TrustedKeyId), (version.GetProperty("status").GetString(), version.GetProperty("keyId").GetString()));
    }

    [Fact]
    public async Task WithUnsignedAllowed_UnsignedAndUntrustedPass_ButAMismatchIsStillRejected()
    {
        var (server, platform, _) = await StartAsync(allowUnsigned: true, PublicKeyOf(_trusted));
        await using var _ = server;
        var package = PluginsTests.Package();

        Assert.Equal(JsonValueKind.Null, (await AssertSignatureAsync(server, platform, package, "None")).GetProperty("keyId").ValueKind);
        var untrusted = await AssertSignatureAsync(server, platform, Sign(package, _stranger), "Untrusted");
        Assert.Equal(PluginSignature.KeyIdOf(_stranger.ExportSubjectPublicKeyInfo()), untrusted.GetProperty("keyId").GetString());
        await AssertRejectedAsync(server, platform, Rewrite(Sign(package, _trusted), "extra.txt", "x"), HttpStatusCode.BadRequest, PluginsErrors.SignatureInvalid, "changed after signing");
    }

    [Fact]
    public async Task Load_RechecksTrust_AgainstTheConfigurationOfTheTime()
    {
        string id;
        var (first, platform, firstHost) = await StartAsync(allowUnsigned: false, PublicKeyOf(_trusted));
        await using (first)
        {
            var fileId = await PluginsTests.UploadAsync(first, platform, Sign(PluginsTests.Package(), _trusted));
            var (installed, body) = await PluginsTests.InstallAsync(first, platform, fileId, Cache, Config);
            Assert.True(installed.StatusCode == HttpStatusCode.OK, body.GetRawText());
            id = body.GetProperty("plugin").GetProperty("id").GetString()!;
            var (enabled, enabledBody) = await first.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/plugins:enable", JsonSerializer.Serialize(new { ids = new[] { id } }), platform));
            Assert.True(enabled.StatusCode == HttpStatusCode.OK, enabledBody.GetRawText());
            Assert.Single(firstHost.Loads);
        }

        // 公钥撤掉了：启动时不加载，错误码记在插件上
        var (second, token, secondHost) = await StartAsync(allowUnsigned: false);
        await using (second)
        {
            Assert.Empty(secondHost.Loads);
            var (got, status) = await second.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}:status", token: token));
            Assert.True(got.StatusCode == HttpStatusCode.OK, status.GetRawText());
            Assert.Equal((false, PluginsErrors.PublisherUntrusted), (status.GetProperty("isLoaded").GetBoolean(), status.GetProperty("errorCode").GetString()));
        }

        // 放行不受信任的包：照常加载
        var (third, thirdToken, thirdHost) = await StartAsync(allowUnsigned: true);
        await using var _ = third;
        Assert.Single(thirdHost.Loads);
        var (_, loaded) = await third.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/plugins/{id}:status", token: thirdToken));
        Assert.True(loaded.GetProperty("isLoaded").GetBoolean(), loaded.GetRawText());
    }

    [Fact]
    public async Task HostCommands_GenerateAKey_AndSignAPackageThatIsTrusted()
    {
        Directory.CreateDirectory(_root);
        var keyFile = Path.Combine(_root, "contoso.key.pem");
        var packageFile = Path.Combine(_root, "weather.xhplugin");
        await File.WriteAllBytesAsync(packageFile, PluginsTests.Package());

        var (generated, keyOutput, _) = await RunAsync("plugins", "keygen", keyFile);
        Assert.Equal(0, generated);
        var printed = keyOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(static l => l.Split(": ", 2)).ToDictionary(static p => p[0], static p => p[1]);
        // 私钥文件已存在即拒绝，不覆盖
        Assert.Equal(1, (await RunAsync("plugins", "keygen", keyFile)).Code);
        Assert.Equal(2, (await RunAsync("plugins", "sign", packageFile)).Code);

        var (signedCode, signOutput, _) = await RunAsync("plugins", "sign", packageFile, "--key", keyFile);
        Assert.Equal((0, $"key: {printed["key"]}"), (signedCode, signOutput.Trim()));

        var (server, platform, _) = await StartAsync(allowUnsigned: false, printed["publicKey"]);
        await using var _ = server;
        var signature = await AssertSignatureAsync(server, platform, await File.ReadAllBytesAsync(packageFile), "Trusted");
        Assert.Equal(printed["key"], signature.GetProperty("keyId").GetString());
    }

    private static async Task<(int Code, string Output, string Error)> RunAsync(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await PluginPackageCommand.RunAsync(args, output, error, CancellationToken.None);
        return (code, output.ToString(), error.ToString());
    }
}
