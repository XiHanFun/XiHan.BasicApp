// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.Identity;
using XiHan.BasicApp.Saas.Domain.Messaging;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 配置键格式校验与各类单一事实源常量的契约测试。
/// 配置键是 module.domain.name 的点分小写格式，落库前必须先规范化；
/// 通道名、模板码、业务类型、内建客户端标识则是「禁止散落魔法字符串」的登记表，重复即语义冲突。
/// </summary>
public sealed class SaasDomainConfigKeyAndConstantTests
{
    /// <summary>
    /// 规范化会去空白并转小写，保证同一配置键的不同书写落到同一行。
    /// </summary>
    /// <param name="input">调用方传入的原始配置键。</param>
    /// <param name="expected">期望规范化后的配置键。</param>
    [Theory]
    [InlineData("saas.auth.login.methods", "saas.auth.login.methods")]
    [InlineData("  saas.auth.login.methods  ", "saas.auth.login.methods")]
    [InlineData("SAAS.AUTH.LOGIN.METHODS", "saas.auth.login.methods")]
    [InlineData("Saas.Bot.Telegram.Webhook-Base-Url", "saas.bot.telegram.webhook-base-url")]
    public void Normalize_ShouldTrimAndLowercase(string input, string expected)
    {
        Assert.Equal(expected, SaasConfigKeys.Normalize(input), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空、空白配置键在规范化与校验入口都必须直接拒绝（null 抛派生的空引用异常）。
    /// </summary>
    /// <param name="input">非法配置键。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeAndValidate_BlankKey_ShouldThrowArgumentException(string? input)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => SaasConfigKeys.Normalize(input!));
        _ = Assert.ThrowsAny<ArgumentException>(() => SaasConfigKeys.Validate(input!));
    }

    /// <summary>
    /// 配置键长度上界为 100：100 字符通过，101 字符拒绝。
    /// </summary>
    [Fact]
    public void Validate_LengthBoundary_ShouldAccept100AndReject101()
    {
        var atLimit = "saas." + new string('a', 95);
        var overLimit = "saas." + new string('a', 96);

        Assert.Equal(100, atLimit.Length);
        SaasConfigKeys.Validate(atLimit);
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => SaasConfigKeys.Validate(overLimit));
        Assert.Contains("不能超过 100 个字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 校验只接受小写英文：含任何大写字母都要求调用方先规范化。
    /// </summary>
    [Fact]
    public void Validate_UppercaseKey_ShouldThrowInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => SaasConfigKeys.Validate("Saas.auth.login"));

        Assert.Equal("配置键必须使用小写英文。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 配置键至少两段且不得有空段，单段键与含空段的键都要拒绝。
    /// </summary>
    /// <param name="configKey">非法配置键。</param>
    [Theory]
    [InlineData("saas")]
    [InlineData("saas.")]
    [InlineData(".saas")]
    [InlineData("saas..login")]
    [InlineData("saas. .login")]
    public void Validate_MalformedSegments_ShouldRequireDottedLayers(string configKey)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => SaasConfigKeys.Validate(configKey));

        Assert.Equal("配置键必须使用 module.domain.name 的点分层格式。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 段内只允许小写字母、数字和连字符，且连字符不能出现在段首或段尾。
    /// </summary>
    /// <param name="configKey">非法配置键。</param>
    [Theory]
    [InlineData("saas.auth_login")]
    [InlineData("saas.auth login")]
    [InlineData("saas.auth:login")]
    [InlineData("saas.-auth")]
    [InlineData("saas.auth-")]
    [InlineData("saas.认证")]
    public void Validate_InvalidSegmentCharacters_ShouldThrowInvalidOperationException(string configKey)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => SaasConfigKeys.Validate(configKey));

        Assert.Equal("配置键只能包含小写英文、数字、点和段内连字符。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 合法配置键形态：两段起、数字与段内连字符均可。
    /// </summary>
    /// <param name="configKey">合法配置键。</param>
    [Theory]
    [InlineData("saas.auth")]
    [InlineData("saas.auth.login.methods")]
    [InlineData("saas.bot.telegram.webhook-base-url")]
    [InlineData("saas.a1.b2c3")]
    public void Validate_WellFormedKey_ShouldPass(string configKey)
    {
        SaasConfigKeys.Validate(configKey);
    }

    /// <summary>
    /// 所有以模块前缀开头的配置键常量都必须能通过自身的格式校验，
    /// 否则种子写入或强类型读取时会在运行期才炸。
    /// </summary>
    [Fact]
    public void DeclaredConfigKeys_ShouldAllSatisfyValidate()
    {
        var violations = new List<string>();
        foreach (var (name, value) in EnumerateRecursiveStringConstants(typeof(SaasConfigKeys)))
        {
            if (!value.StartsWith(SaasConfigKeys.Prefix + ".", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                SaasConfigKeys.Validate(value);
            }
            catch (Exception exception)
            {
                violations.Add($"{name} = {value} -> {exception.Message}");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"以下配置键常量不满足配置键格式：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 配置键常量值必须唯一，重复会让两个语义共用一行配置而互相覆盖。
    /// </summary>
    [Fact]
    public void DeclaredConfigKeys_ShouldBeUnique()
    {
        var duplicates = EnumerateRecursiveStringConstants(typeof(SaasConfigKeys))
            .Where(item => item.Value.StartsWith(SaasConfigKeys.Prefix + ".", StringComparison.Ordinal))
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} <- {string.Join(" / ", group.Select(item => item.Name))}")
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"以下配置键被多个常量重复定义：{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
    }

    /// <summary>
    /// 配置键的第二段必须是已登记的配置分组码，保证读写两侧按分组能取全。
    /// </summary>
    [Fact]
    public void DeclaredConfigKeys_SecondSegment_ShouldBeRegisteredGroup()
    {
        var groups = EnumerateStringConstants(typeof(SaasConfigKeys.Groups))
            .Select(item => item.Value)
            .ToHashSet(StringComparer.Ordinal);

        var violations = EnumerateRecursiveStringConstants(typeof(SaasConfigKeys))
            .Where(item => item.Value.StartsWith(SaasConfigKeys.Prefix + ".", StringComparison.Ordinal))
            .Where(item => !groups.Contains(item.Value.Split('.')[1]))
            .Select(item => $"{item.Name} = {item.Value}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"以下配置键的分组段未登记进 SaasConfigKeys.Groups：{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// 消息通道名是发件箱路由与发送器匹配的键，必须小写且互不重复。
    /// </summary>
    [Fact]
    public void MessageChannelNames_ShouldBeLowercaseAndUnique()
    {
        var channels = EnumerateStringConstants(typeof(SaasMessageChannelNames)).ToList();

        Assert.NotEmpty(channels);
        Assert.All(channels, item => Assert.Equal(item.Value.ToLowerInvariant(), item.Value, StringComparer.Ordinal));
        Assert.Equal(channels.Count, channels.Select(item => item.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("email", SaasMessageChannelNames.Email, StringComparer.Ordinal);
        Assert.Equal("sms", SaasMessageChannelNames.Sms, StringComparer.Ordinal);
        Assert.Equal("bot", SaasMessageChannelNames.Bot, StringComparer.Ordinal);
    }

    /// <summary>
    /// 消息模板码是种子与租户覆盖模板的匹配键，必须是小写短横线串且全局唯一。
    /// </summary>
    [Fact]
    public void MessageTemplateCodes_ShouldBeLowerKebabAndUnique()
    {
        var codes = EnumerateNestedStringConstants(typeof(SaasMessageTemplateCodes)).ToList();

        Assert.NotEmpty(codes);
        var malformed = codes
            .Where(item => !IsLowerKebab(item.Value))
            .Select(item => $"{item.Name} = {item.Value}")
            .ToList();
        Assert.True(
            malformed.Count == 0,
            $"以下消息模板码不是小写短横线格式：{Environment.NewLine}{string.Join(Environment.NewLine, malformed)}");

        var duplicates = codes
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        Assert.True(
            duplicates.Count == 0,
            $"以下消息模板码重复：{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
    }

    /// <summary>
    /// 认证流程模板码必须带 auth- 前缀、通知模板码必须带 notification- 前缀，
    /// 便于按前缀批量识别与种子分组。
    /// </summary>
    [Fact]
    public void MessageTemplateCodes_ShouldCarryScopePrefix()
    {
        Assert.All(
            EnumerateStringConstants(typeof(SaasMessageTemplateCodes.Auth)),
            item => Assert.StartsWith("auth-", item.Value, StringComparison.Ordinal));
        Assert.All(
            EnumerateStringConstants(typeof(SaasMessageTemplateCodes.Notification)),
            item => Assert.StartsWith("notification-", item.Value, StringComparison.Ordinal));
    }

    /// <summary>
    /// 消息业务类型用于关联业务实体，采用 message.{业务} 的点分命名。
    /// </summary>
    [Fact]
    public void MessageBusinessTypes_ShouldUseDottedMessagePrefix()
    {
        var types = EnumerateStringConstants(typeof(SaasMessageBusinessTypes)).ToList();

        Assert.NotEmpty(types);
        Assert.All(types, item => Assert.StartsWith("message.", item.Value, StringComparison.Ordinal));
        Assert.Equal("message.notification", SaasMessageBusinessTypes.Notification, StringComparer.Ordinal);
    }

    /// <summary>
    /// 内建 OAuth 客户端标识是签发令牌与种子共用的字面量，必须稳定不变。
    /// </summary>
    [Fact]
    public void OAuthClientIds_ShouldStayStable()
    {
        Assert.Equal("basicapp-web", SaasOAuthClientIds.Web, StringComparer.Ordinal);
        Assert.Equal("basicapp", SaasOAuthClientIds.DefaultScope, StringComparer.Ordinal);
        Assert.All(
            EnumerateStringConstants(typeof(SaasOAuthClientIds)),
            item => Assert.True(IsLowerKebab(item.Value), $"{item.Name} = {item.Value} 不是小写短横线格式"));
    }

    private static IEnumerable<(string Name, string Value)> EnumerateStringConstants(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => ($"{type.Name}.{field.Name}", (string)field.GetRawConstantValue()!));
    }

    private static IEnumerable<(string Name, string Value)> EnumerateRecursiveStringConstants(Type type)
    {
        return EnumerateStringConstants(type)
            .Concat(type.GetNestedTypes(BindingFlags.Public).SelectMany(EnumerateRecursiveStringConstants));
    }

    private static IEnumerable<(string Name, string Value)> EnumerateNestedStringConstants(Type type)
    {
        return type
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(EnumerateStringConstants)
            .Concat(EnumerateStringConstants(type));
    }

    private static bool IsLowerKebab(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[^1] == '-')
        {
            return false;
        }

        return value.All(static character =>
            character is >= 'a' and <= 'z'
            || character is >= '0' and <= '9'
            || character == '-');
    }
}
