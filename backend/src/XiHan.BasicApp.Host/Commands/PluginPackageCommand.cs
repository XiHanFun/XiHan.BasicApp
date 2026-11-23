// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using XiHan.BasicApp.Plugins.Domain.Packages;

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// <c>plugins keygen</c> 与 <c>plugins sign</c> 命令：插件发布者生成签名密钥、给插件包签名
/// </summary>
/// <remarks>
/// 只读写本机文件，不构建宿主、不连数据库：
/// <c>plugins keygen &lt;私钥文件&gt;</c> 生成 P-256 私钥（PKCS#8 PEM，文件已存在即拒绝），在 stdout 打出公钥指纹与要写进 <c>BasicApp:Plugins:TrustedPublishers</c> 的公钥
/// <c>plugins sign &lt;插件包&gt; --key &lt;私钥文件&gt;</c> 原地写入（或换掉）签名文件，在 stdout 打出公钥指纹；签什么见 <see cref="PluginSignature"/>
/// 退出码：0 成功，1 文件读写或包、密钥不合要求（原因写 stderr），2 用法错误
/// </remarks>
public static class PluginPackageCommand
{
    /// <summary>
    /// 命令名
    /// </summary>
    public const string CommandName = "plugins";

    private const string Usage = "Usage: plugins keygen <private-key-file> | plugins sign <package> --key <private-key-file>";

    /// <summary>
    /// 是不是这条命令
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static bool Matches(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Count > 0 && string.Equals(args[0], CommandName, StringComparison.Ordinal);
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="output">标准输出</param>
    /// <param name="error">标准错误</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>进程退出码</returns>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            switch (args.Count > 1 ? args[1] : null)
            {
                case "keygen" when args.Count == 3:
                    await KeygenAsync(args[2], output, cancellationToken).ConfigureAwait(false);
                    return 0;
                case "sign" when args.Count == 5 && string.Equals(args[3], "--key", StringComparison.Ordinal):
                    await SignAsync(args[2], args[4], output, cancellationToken).ConfigureAwait(false);
                    return 0;
                default:
                    await error.WriteLineAsync(Usage).ConfigureAwait(false);
                    return 2;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or CryptographicException or ArgumentException)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 1;
        }
    }

    /// <summary>
    /// 生成私钥并打出公钥
    /// </summary>
    private static async Task KeygenAsync(string path, TextWriter output, CancellationToken cancellationToken)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using (file.ConfigureAwait(false))
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            var writer = new StreamWriter(file);
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteAsync(key.ExportPkcs8PrivateKeyPem().AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }

        var publicKey = key.ExportSubjectPublicKeyInfo();
        await output.WriteLineAsync($"key: {PluginSignature.KeyIdOf(publicKey)}").ConfigureAwait(false);
        await output.WriteLineAsync($"publicKey: {Convert.ToBase64String(publicKey)}").ConfigureAwait(false);
    }

    /// <summary>
    /// 用私钥给包签名
    /// </summary>
    private static async Task SignAsync(string packagePath, string keyPath, TextWriter output, CancellationToken cancellationToken)
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(await File.ReadAllTextAsync(keyPath, cancellationToken).ConfigureAwait(false));
        using (var publicKey = PluginSignature.ImportPublicKey(key.ExportSubjectPublicKeyInfo()))
        {
            if (publicKey is null)
            {
                throw new InvalidDataException($"'{keyPath}' is not a P-256 private key; create one with 'plugins keygen'.");
            }
        }

        var package = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        await using (package.ConfigureAwait(false))
        {
            await output.WriteLineAsync($"key: {await PluginSignature.SignAsync(package, key, cancellationToken).ConfigureAwait(false)}").ConfigureAwait(false);
        }
    }
}
