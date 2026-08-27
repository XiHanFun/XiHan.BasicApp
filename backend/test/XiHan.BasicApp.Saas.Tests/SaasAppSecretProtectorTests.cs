// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using XiHan.BasicApp.Saas.Infrastructure.Security;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 可逆加密字段保护器测试。
/// </summary>
/// <remarks>
/// 八个保护器形状一致，各自持有一个独立 Purpose。它们守的两件事：
/// <list type="number">
/// <item><b>Purpose 隔离</b>——同一密钥环下，A 类密文绝不能被 B 类保护器解开；
/// 一旦有人把 Purpose 复制粘贴写重了，存储密钥与租户连接串就互通了，而且没有任何编译期信号。</item>
/// <item><b>加密幂等</b>——已带 <c>dp:</c> 前缀的值不再二次加密；漏了这条，一次"编辑保存不改密钥"
/// 就会把密文当明文再加密一层，下次解密直接读出乱码。</item>
/// </list>
/// 全部使用进程内临时密钥环（<see cref="EphemeralDataProtectionProvider"/>），不落盘、不联网。
/// </remarks>
public sealed class SaasAppSecretProtectorTests
{
    private readonly IDataProtectionProvider _provider = new EphemeralDataProtectionProvider();

    /// <summary>
    /// 全部八个保护器：加密后必须带前缀、必须与明文不同、且能原样解回。
    /// </summary>
    [Fact]
    public void AllProtectors_ShouldRoundTripAndCarryCipherPrefix()
    {
        const string plaintext = "S3cr3t-值-!@#";

        foreach (var (name, protect, unprotect) in BuildProtectors())
        {
            var cipher = protect(plaintext);

            Assert.NotNull(cipher);
            Assert.StartsWith(SaasSecretProtectionPurposes.CipherPrefix, cipher!, StringComparison.Ordinal);
            Assert.NotEqual(plaintext, cipher, StringComparer.Ordinal);
            Assert.Equal(plaintext, unprotect(cipher), StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 加密必须幂等：已是密文的值原样返回，不叠加第二层加密。
    /// </summary>
    [Fact]
    public void AllProtectors_Protect_ShouldBeIdempotentOnAlreadyEncryptedValue()
    {
        foreach (var (name, protect, unprotect) in BuildProtectors())
        {
            var once = protect("原始密钥");
            var twice = protect(once);

            Assert.Equal(once, twice, StringComparer.Ordinal);
            Assert.Equal("原始密钥", unprotect(twice), StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// null 与空串在加解密两个方向都原样透传，不会被加密成一段无意义密文。
    /// </summary>
    [Fact]
    public void AllProtectors_ShouldPassThroughNullAndEmpty()
    {
        foreach (var (name, protect, unprotect) in BuildProtectors())
        {
            Assert.Null(protect(null));
            Assert.Null(unprotect(null));
            Assert.Equal(string.Empty, protect(string.Empty), StringComparer.Ordinal);
            Assert.Equal(string.Empty, unprotect(string.Empty), StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 同一明文两次加密得到不同密文（Data Protection 自带随机化），但都能解回同一明文。
    /// </summary>
    [Fact]
    public void Protect_SamePlaintextTwice_ShouldProduceDifferentCipherButSamePlaintext()
    {
        var protector = new DataProtectionStorageSecretProtector(_provider);

        var first = protector.Protect("same");
        var second = protector.Protect("same");

        Assert.NotEqual(first, second, StringComparer.Ordinal);
        Assert.Equal("same", protector.Unprotect(first), StringComparer.Ordinal);
        Assert.Equal("same", protector.Unprotect(second), StringComparer.Ordinal);
    }

    /// <summary>
    /// Purpose 隔离：同一密钥环下，任一保护器都解不开其它保护器写的密文。
    /// </summary>
    /// <remarks>
    /// 这是整组保护器唯一真正的安全边界。逐对交叉验证，任一 Purpose 被写重都会红。
    /// </remarks>
    [Fact]
    public void Protectors_ShouldNotDecryptEachOthersCipherText()
    {
        var protectors = BuildProtectors();

        foreach (var producer in protectors)
        {
            var cipher = producer.Protect("cross-purpose")!;

            foreach (var consumer in protectors.Where(item => !string.Equals(item.Name, producer.Name, StringComparison.Ordinal)))
            {
                Assert.ThrowsAny<CryptographicException>(() => consumer.Unprotect(cipher));
            }
        }
    }

    /// <summary>
    /// 未带 <c>dp:</c> 前缀的历史明文解密时直接抛错（fail-closed，不做旧明文兼容）。
    /// </summary>
    [Fact]
    public void Unprotect_LegacyPlaintext_ShouldThrowInsteadOfReturningIt()
    {
        var protector = new DataProtectionStorageSecretProtector(_provider);

        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect("这是一段历史明文密钥"));
    }

    /// <summary>
    /// 回归锚点：非本保护器写入的值（含短于前缀的脏值）一律按"解密失败"抛出，八个保护器口径一致。
    /// </summary>
    /// <remarks>
    /// 修复前 <c>Unprotect</c> 无条件裁掉前 3 个字符，<c>"a"</c> / <c>"dp"</c> 会在字符串切片处抛
    /// <see cref="ArgumentOutOfRangeException"/>——那是"参数越界"，把排查方向指向调用方传参，
    /// 而真实原因是库里那行不是密文。本用例当时锁定的正是那份错误语义，现改为锁定修复后的行为：
    /// 抛 <see cref="CryptographicException"/> 且消息里点明缺少前缀。
    /// </remarks>
    /// <param name="value">不是本保护器写出的值。</param>
    [Theory]
    [InlineData("a")]
    [InlineData("dp")]
    [InlineData("d")]
    [InlineData("这是一段历史明文密钥")]
    [InlineData("xdp:something")]
    public void Unprotect_ValueWithoutCipherPrefix_ShouldThrowCryptographicException(string value)
    {
        foreach (var (_, _, unprotect) in BuildProtectors())
        {
            var exception = Assert.ThrowsAny<CryptographicException>(() => unprotect(value));

            // 消息里必须点名前缀，否则运维只能看到一句泛化的"解密失败"，分不清是值不对还是密钥环不对
            Assert.Contains(SaasSecretProtectionPurposes.CipherPrefix, exception.Message, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// 密文前缀常量必须保持为 <c>dp:</c>——它是"已加密 / 历史明文"的唯一判据，改了就读不回历史数据。
    /// </summary>
    [Fact]
    public void CipherPrefix_ShouldStayAsAgreed()
    {
        Assert.Equal("dp:", SaasSecretProtectionPurposes.CipherPrefix, StringComparer.Ordinal);
    }

    /// <summary>
    /// Purpose 常量两两不同：任意两条相同即等于两类密钥共用密钥环，安全隔离失效。
    /// </summary>
    [Fact]
    public void Purposes_ShouldBeUnique()
    {
        var purposes = PurposeConstants();

        var duplicated = purposes
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} ← {string.Join(", ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(duplicated.Count == 0, $"以下 Purpose 被重复使用，密钥隔离失效：{string.Join(" | ", duplicated)}");
    }

    /// <summary>
    /// Purpose 必须带模块前缀与 <c>.vN</c> 版本后缀——版本号是唯一的密钥轮换手段。
    /// </summary>
    [Fact]
    public void Purposes_ShouldBeNamespacedAndVersioned()
    {
        var offenders = PurposeConstants()
            .Where(item => !item.Value.StartsWith("XiHan.BasicApp.Saas.", StringComparison.Ordinal)
                           || !System.Text.RegularExpressions.Regex.IsMatch(item.Value, @"\.v\d+$"))
            .Select(item => $"{item.Name}={item.Value}")
            .ToList();

        Assert.True(offenders.Count == 0, $"Purpose 必须以 XiHan.BasicApp.Saas. 开头并以 .vN 结尾：{string.Join(", ", offenders)}");
    }

    /// <summary>
    /// 每个保护器都必须挑一条专属 Purpose，不允许两个保护器共用同一常量。
    /// </summary>
    [Fact]
    public void EachProtector_ShouldOwnADistinctPurpose()
    {
        var protectors = BuildProtectors();
        var probes = protectors
            .Select(item => (item.Name, Cipher: item.Protect("probe")!))
            .ToList();

        foreach (var probe in probes)
        {
            var decryptable = protectors
                .Where(item => TryUnprotect(item.Unprotect, probe.Cipher))
                .Select(item => item.Name)
                .ToList();

            Assert.True(
                decryptable.Count == 1 && string.Equals(decryptable[0], probe.Name, StringComparison.Ordinal),
                $"{probe.Name} 写出的密文被这些保护器解开了：{string.Join(", ", decryptable)}");
        }
    }

    /// <summary>
    /// 构造全部八个保护器的加解密委托。
    /// </summary>
    /// <returns>保护器名称与加解密委托。</returns>
    private List<(string Name, Func<string?, string?> Protect, Func<string?, string?> Unprotect)> BuildProtectors()
    {
        var storage = new DataProtectionStorageSecretProtector(_provider);
        var tenant = new DataProtectionTenantConnectionSecretProtector(_provider);
        var sms = new DataProtectionSmsConfigSecretProtector(_provider);
        var email = new DataProtectionEmailConfigSecretProtector(_provider);
        var bot = new DataProtectionBotConfigSecretProtector(_provider);
        var telegram = new DataProtectionTelegramBotTokenProtector(_provider);
        var config = new DataProtectionConfigValueSecretProtector(_provider);
        var credential = new DataProtectionUserApiCredentialSecretProtector(_provider);

        return
        [
            (nameof(DataProtectionStorageSecretProtector), storage.Protect, storage.Unprotect),
            (nameof(DataProtectionTenantConnectionSecretProtector), tenant.Protect, tenant.Unprotect),
            (nameof(DataProtectionSmsConfigSecretProtector), sms.Protect, sms.Unprotect),
            (nameof(DataProtectionEmailConfigSecretProtector), email.Protect, email.Unprotect),
            (nameof(DataProtectionBotConfigSecretProtector), bot.Protect, bot.Unprotect),
            (nameof(DataProtectionTelegramBotTokenProtector), telegram.Protect, telegram.Unprotect),
            (nameof(DataProtectionConfigValueSecretProtector), config.Protect, config.Unprotect),
            (nameof(DataProtectionUserApiCredentialSecretProtector), credential.Protect, credential.Unprotect)
        ];
    }

    /// <summary>
    /// 读取 Purpose 常量（排除密文前缀常量本身）。
    /// </summary>
    /// <returns>常量名与取值。</returns>
    private static List<(string Name, string Value)> PurposeConstants()
    {
        return typeof(SaasSecretProtectionPurposes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Where(field => !string.Equals(field.Name, nameof(SaasSecretProtectionPurposes.CipherPrefix), StringComparison.Ordinal))
            .Select(field => (field.Name, Value: (string)field.GetRawConstantValue()!))
            .ToList();
    }

    /// <summary>
    /// 尝试解密，成功返回 true。
    /// </summary>
    /// <param name="unprotect">解密委托。</param>
    /// <param name="cipher">密文。</param>
    /// <returns>是否解密成功。</returns>
    private static bool TryUnprotect(Func<string?, string?> unprotect, string cipher)
    {
        try
        {
            unprotect(cipher);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
