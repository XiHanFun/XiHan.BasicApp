// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Runtime.Security;

/// <summary>
/// 数据保护的装配与约定。
/// </summary>
/// <remarks>
/// 底层是 ASP.NET Core Data Protection，应用名固定为 <see cref="AppInfo.Name"/>（同一套密钥环的实例之间才能互解）。
/// 多实例部署要共享密钥环：配置 <see cref="KeysDirectoryKey"/> 指向各实例都能读写的目录；不配即 ASP.NET Core 的缺省位置（单机开发够用）。
/// </remarks>
public static class SecretProtection
{
    /// <summary>
    /// 密文前缀：区分本保护器写入的值与明文。
    /// </summary>
    public const string CipherPrefix = "dp:";

    /// <summary>
    /// 密钥环目录的配置键。
    /// </summary>
    public const string KeysDirectoryKey = "BasicApp:DataProtection:KeysDirectory";

    /// <summary>
    /// 登记 Data Protection 与 <see cref="ISecretProtector"/>。
    /// </summary>
    public static IServiceCollection AddSecretProtection(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var builder = services.AddDataProtection().SetApplicationName(AppInfo.Name);
        var directory = configuration[KeysDirectoryKey];
        if (!string.IsNullOrWhiteSpace(directory))
        {
            builder.PersistKeysToFileSystem(new DirectoryInfo(directory));
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        return services;
    }

    /// <summary>
    /// 基于 <see cref="IDataProtectionProvider"/> 的实现。
    /// </summary>
    /// <remarks>
    /// 每个用途一个保护器。
    /// </remarks>
    /// <param name="provider">数据保护提供器。</param>
    private sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
    {
        /// <inheritdoc />
        public string Protect(string purpose, string plaintext)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
            ArgumentNullException.ThrowIfNull(plaintext);
            return plaintext.StartsWith(CipherPrefix, StringComparison.Ordinal)
                ? plaintext
                : CipherPrefix + provider.CreateProtector(purpose).Protect(plaintext);
        }

        /// <inheritdoc />
        public bool TryUnprotect(string purpose, string ciphertext, [NotNullWhen(true)] out string? plaintext)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
            ArgumentNullException.ThrowIfNull(ciphertext);
            plaintext = null;
            if (!ciphertext.StartsWith(CipherPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                plaintext = provider.CreateProtector(purpose).Unprotect(ciphertext[CipherPrefix.Length..]);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}
