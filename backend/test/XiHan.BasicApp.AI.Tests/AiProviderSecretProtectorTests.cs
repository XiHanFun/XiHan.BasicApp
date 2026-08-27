// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Infrastructure.Security;
using XiHan.BasicApp.Saas.Infrastructure.Security;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI Provider 密钥保护器测试：锁定「密钥必须可逆加密落库、密文永不等于明文、加密幂等、
/// 解密失败一律抛出（fail-closed，不回退明文）」这条安全底线。
/// </summary>
/// <remarks>
/// 用进程内临时密钥环（<see cref="EphemeralDataProtectionProvider"/>）跑真实加解密，不落盘、不联网；
/// 每个用例自建 provider，互不共享可变状态，可任意顺序并行执行。
/// </remarks>
public sealed class AiProviderSecretProtectorTests
{
    /// <summary>
    /// 明文密钥加密后必须能原样还原，否则线上配置的模型密钥全部作废。
    /// </summary>
    /// <param name="plaintext">待往返的明文密钥。</param>
    [Theory]
    [InlineData("sk-1234567890")]
    [InlineData("a")]
    [InlineData("含中文的密钥-🔑")]
    [InlineData("  两端有空白  ")]
    [InlineData("dp")]
    public void ProtectThenUnprotect_ShouldRoundTripPlaintext(string plaintext)
    {
        var protector = CreateProtector();

        var cipher = protector.Protect(plaintext);

        Assert.Equal(plaintext, protector.Unprotect(cipher), StringComparer.Ordinal);
    }

    /// <summary>
    /// 超长密钥同样必须完整往返，不得被截断（列长 500，但加密体量远大于明文，本用例只保证算法侧无损）。
    /// </summary>
    [Fact]
    public void ProtectThenUnprotect_LongSecretShouldRoundTripWithoutTruncation()
    {
        var protector = CreateProtector();
        var plaintext = new string('k', 4096);

        var restored = protector.Unprotect(protector.Protect(plaintext));

        Assert.Equal(plaintext, restored, StringComparer.Ordinal);
    }

    /// <summary>
    /// 密文必须与明文不同且不含明文子串，否则密钥等同于明文落库。
    /// </summary>
    [Fact]
    public void Protect_CipherShouldNeitherEqualNorContainPlaintext()
    {
        var protector = CreateProtector();
        const string Plaintext = "sk-secret-value";

        var cipher = protector.Protect(Plaintext)!;

        Assert.NotEqual(Plaintext, cipher, StringComparer.Ordinal);
        Assert.DoesNotContain(Plaintext, cipher, StringComparison.Ordinal);
    }

    /// <summary>
    /// 密文必须带 <c>dp:</c> 前缀，这是幂等判定与"是否已加密"的唯一标记。
    /// </summary>
    [Fact]
    public void Protect_CipherShouldCarryCipherPrefix()
    {
        var protector = CreateProtector();

        var cipher = protector.Protect("sk-secret-value")!;

        Assert.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, cipher, StringComparison.Ordinal);
        Assert.Equal("dp:", SaasSecretProtectionPurposes.CipherPrefix, StringComparer.Ordinal);
    }

    /// <summary>
    /// 对已是密文的值再次加密必须原样返回（幂等），否则更新流程会把密文二次加密、解出来是密文。
    /// </summary>
    [Fact]
    public void Protect_AlreadyProtectedValueShouldReturnSameInstanceValue()
    {
        var protector = CreateProtector();
        var cipher = protector.Protect("sk-secret-value")!;

        var twice = protector.Protect(cipher);

        Assert.Equal(cipher, twice, StringComparer.Ordinal);
        Assert.Equal("sk-secret-value", protector.Unprotect(twice), StringComparer.Ordinal);
    }

    /// <summary>
    /// 同一明文两次加密必须得到不同密文（Data Protection 带随机化），否则密文可被比对反推。
    /// </summary>
    [Fact]
    public void Protect_SamePlaintextTwiceShouldProduceDifferentCiphers()
    {
        var protector = CreateProtector();

        var first = protector.Protect("sk-secret-value")!;
        var second = protector.Protect("sk-secret-value")!;

        Assert.NotEqual(first, second, StringComparer.Ordinal);
        Assert.Equal(protector.Unprotect(first), protector.Unprotect(second), StringComparer.Ordinal);
    }

    /// <summary>
    /// null 与空串必须原样穿透（"未配置密钥"不是错误，不能被加密成一段密文）。
    /// </summary>
    /// <param name="value">空值输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ProtectAndUnprotect_NullOrEmptyShouldPassThroughUnchanged(string? value)
    {
        var protector = CreateProtector();

        Assert.Equal(value, protector.Protect(value), StringComparer.Ordinal);
        Assert.Equal(value, protector.Unprotect(value), StringComparer.Ordinal);
    }

    /// <summary>
    /// 纯空白不等于空：仍走加密路径，且能原样还原（领域层已先把空白归一为 null，此处只锁保护器自身口径）。
    /// </summary>
    /// <param name="value">纯空白输入。</param>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Protect_WhitespaceOnlyShouldStillBeEncrypted(string value)
    {
        var protector = CreateProtector();

        var cipher = protector.Protect(value)!;

        Assert.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, cipher, StringComparison.Ordinal);
        Assert.Equal(value, protector.Unprotect(cipher), StringComparer.Ordinal);
    }

    /// <summary>
    /// 密文被篡改必须解密失败并抛出，绝不允许静默返回垃圾串或回退明文。
    /// </summary>
    [Fact]
    public void Unprotect_TamperedCipherShouldThrow()
    {
        var protector = CreateProtector();
        var cipher = protector.Protect("sk-secret-value")!;
        var tampered = cipher[..^1] + (cipher[^1] == 'A' ? 'B' : 'A');

        _ = Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(tampered));
    }

    /// <summary>
    /// 截断的密文同样必须抛出，不得当作"部分可用"。
    /// </summary>
    [Fact]
    public void Unprotect_TruncatedCipherShouldThrow()
    {
        var protector = CreateProtector();
        var cipher = protector.Protect("sk-secret-value")!;

        _ = Assert.ThrowsAny<Exception>(() => protector.Unprotect(cipher[..(cipher.Length / 2)]));
    }

    /// <summary>
    /// 无 <c>dp:</c> 前缀的历史明文必须解密失败（fail-closed，源码注释明确不做旧明文兼容），
    /// 且必须给出"不是本保护器写的值"这一明确语义。
    /// </summary>
    /// <remarks>
    /// 回归锚点：旧实现不判前缀直接切片——短于 3 字符的脏值在切片处抛 ArgumentOutOfRangeException，
    /// 够长但无前缀的历史明文被砍掉 3 个有效字符后抛 CryptographicException，
    /// 两者都会被误读成"密钥环不匹配/密文损坏"，把排查方向带偏。
    /// </remarks>
    /// <param name="value">未加密的历史值。</param>
    [Theory]
    [InlineData("sk-legacy-plaintext")]
    [InlineData("xy")]
    [InlineData("d")]
    [InlineData("dp")]
    [InlineData("DP:cipher")]
    public void Unprotect_LegacyPlaintextWithoutPrefixShouldThrowInvalidOperation(string value)
    {
        var protector = CreateProtector();

        var exception = Assert.Throws<InvalidOperationException>(() => protector.Unprotect(value));

        Assert.Contains("不是有效密文", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 取到领域层上限（1000 字符）的明文加密后必须仍能落进 Api_Key 列，
    /// 这是 AiProviderDomainService.ApiKeyMaxLength 的推导依据。
    /// </summary>
    /// <remarks>
    /// 回归锚点：落库的是密文不是明文。Data Protection 密文约为明文的 4/3 再加固定头部与 <c>dp:</c> 前缀，
    /// 明文越过列长对应的临界点后密文会被截断（截断的密文永久解不开）或写库直接报错，
    /// 而领域层原先对 ApiKey 没有任何长度校验。
    /// 列长直接从实体读，上限与列长任何一侧被改窄都会在此先红。
    /// </remarks>
    [Fact]
    public void Protect_MaxLengthPlaintextCipherShouldFitColumn()
    {
        // 必须与 AiProviderDomainService.ApiKeyMaxLength 保持一致（该常量为私有，故此处按值复刻）。
        const int ApiKeyMaxLength = 1000;
        var columnLength = typeof(SysAiProvider)
            .GetProperty(nameof(SysAiProvider.ApiKey))!
            .GetCustomAttribute<SugarColumn>()!
            .Length;
        var protector = CreateProtector();

        var cipher = protector.Protect(new string('k', ApiKeyMaxLength))!;

        Assert.True(columnLength > 0, "Api_Key 改成不定长列后，本用例的列长推导前提失效，需同步修订领域层上限注释。");
        Assert.True(
            cipher.Length <= columnLength,
            $"明文 {ApiKeyMaxLength} 字符加密后为 {cipher.Length} 字符，已超出 Api_Key 列长 {columnLength}。");
    }

    /// <summary>
    /// 换一个 Purpose 的保护器解不开本保护器的密文，密钥用途隔离必须真实生效。
    /// </summary>
    /// <remarks>
    /// 若 Purpose 被写错或与存储密钥/短信密钥共用，一处密钥轮换会连带打穿另一处；
    /// 本用例用同一密钥环、不同 Purpose 交叉解密，必须失败。
    /// </remarks>
    [Fact]
    public void Unprotect_CipherFromAnotherPurposeShouldThrow()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = new DataProtectionAiProviderSecretProtector(provider);
        var foreign = provider.CreateProtector(SaasSecretProtectionPurposes.StorageSecretAccessKey);
        var foreignCipher = SaasSecretProtectionPurposes.CipherPrefix + foreign.Protect("sk-secret-value");

        _ = Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(foreignCipher));
    }

    /// <summary>
    /// 另一套密钥环产出的密文也必须解不开（多实例未共享密钥环时 fail-closed，而不是返回脏数据）。
    /// </summary>
    [Fact]
    public void Unprotect_CipherFromAnotherKeyRingShouldThrow()
    {
        var first = CreateProtector();
        var second = CreateProtector();
        var cipher = first.Protect("sk-secret-value");

        _ = Assert.ThrowsAny<CryptographicException>(() => second.Unprotect(cipher));
    }

    /// <summary>
    /// 保护器必须且只能使用 AI Provider 专属 Purpose，Purpose 变更会让全部历史密钥立即失效。
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateProtectorWithAiProviderApiKeyPurpose()
    {
        var provider = new PurposeRecordingDataProtectionProvider();

        _ = new DataProtectionAiProviderSecretProtector(provider);

        Assert.Equal(
            ["XiHan.BasicApp.Saas.AiProvider.ApiKey.v1"],
            provider.RequestedPurposes,
            StringComparer.Ordinal);
        Assert.Equal(
            SaasSecretProtectionPurposes.AiProviderApiKey,
            provider.RequestedPurposes[0],
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 构造保护器：进程内临时密钥环，纯内存、不落盘、不联网。
    /// </summary>
    /// <returns>被测保护器。</returns>
    private static DataProtectionAiProviderSecretProtector CreateProtector()
    {
        return new DataProtectionAiProviderSecretProtector(new EphemeralDataProtectionProvider());
    }

    /// <summary>
    /// 只记录 Purpose 的假 provider，用于断言保护器申领的密钥用途。
    /// </summary>
    private sealed class PurposeRecordingDataProtectionProvider : IDataProtectionProvider
    {
        /// <summary>
        /// 被申领过的 Purpose（按调用顺序）。
        /// </summary>
        public List<string> RequestedPurposes { get; } = [];

        /// <summary>
        /// 记录 Purpose 并返回一个不参与本用例断言的真实保护器。
        /// </summary>
        /// <param name="purpose">保护用途。</param>
        /// <returns>保护器。</returns>
        public IDataProtector CreateProtector(string purpose)
        {
            RequestedPurposes.Add(purpose);
            return new EphemeralDataProtectionProvider().CreateProtector(purpose);
        }
    }
}
