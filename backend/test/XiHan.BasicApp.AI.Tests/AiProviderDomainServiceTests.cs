// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.AI.Abstractions.Configuration;
using XiHan.Framework.AI.Abstractions.Guardrails;
using XiHan.Framework.AI.Providers;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI Provider 领域不变量测试：覆盖配置编码唯一且不可变、采样温度闭区间、密钥"只写不回读"的
/// 加密与保留语义、租户内单默认互斥，以及连接测试的探测降级路径。
/// </summary>
/// <remarks>
/// 连接测试用例一律构造"本地即失败"的配置（缺 Model / 非法端点），使工厂在建客户端阶段就抛异常，
/// 探测走 catch 分支返回失败结果——整套用例不发起任何网络请求。
/// </remarks>
public sealed class AiProviderDomainServiceTests
{
    /// <summary>
    /// 合法命令必须逐字段规范化落到实体：可选空白字段归一为 null，非空字段两端空白被裁掉。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ValidCommandShouldNormalizeAllFields()
    {
        var fixture = CreateFixture();
        var command = AiTestHelper.CreateProviderCommand() with
        {
            ConfigCode = "  code-a  ",
            ConfigName = "  配置  ",
            Provider = "  OpenAI  ",
            Model = "  gpt-4o  ",
            EmbeddingModel = "   ",
            BaseUrl = "  https://api.example.com  ",
            ExtraJson = "  {\"a\":1}  ",
            Remark = "\t"
        };

        var result = await fixture.Service.CreateProviderAsync(command);

        Assert.Equal("code-a", result.Provider.ConfigCode, StringComparer.Ordinal);
        Assert.Equal("配置", result.Provider.ConfigName, StringComparer.Ordinal);
        Assert.Equal("OpenAI", result.Provider.Provider, StringComparer.Ordinal);
        Assert.Equal("gpt-4o", result.Provider.Model, StringComparer.Ordinal);
        Assert.Null(result.Provider.EmbeddingModel);
        Assert.Equal("https://api.example.com", result.Provider.BaseUrl, StringComparer.Ordinal);
        Assert.Equal("{\"a\":1}", result.Provider.ExtraJson, StringComparer.Ordinal);
        Assert.Null(result.Provider.Remark);
    }

    /// <summary>
    /// 密钥必须加密后才落到实体上，明文一次都不能出现在 ApiKey 列里。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ShouldPersistEncryptedApiKeyOnly()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand());

        Assert.Equal("cipher:sk-plain-key", result.Provider.ApiKey, StringComparer.Ordinal);
        fixture.Protector.Verify(protector => protector.Protect("sk-plain-key"), Times.Once);
    }

    /// <summary>
    /// 密钥两端空白必须先归一再加密，否则复制粘贴带的换行会被一起加密进去导致鉴权 401。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ShouldTrimApiKeyBeforeEncrypting()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { ApiKey = "  sk-plain-key\n" });

        fixture.Protector.Verify(protector => protector.Protect("sk-plain-key"), Times.Once);
    }

    /// <summary>
    /// 未填密钥时必须以 null 交给保护器，落库仍为 null（"未配置密钥"不能变成一段密文）。
    /// </summary>
    /// <param name="apiKey">空白密钥输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProviderAsync_BlankApiKeyShouldStayNull(string? apiKey)
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { ApiKey = apiKey });

        Assert.Null(result.Provider.ApiKey);
        fixture.Protector.Verify(protector => protector.Protect(null), Times.Once);
    }

    /// <summary>
    /// 明文密钥超过 1000 字符必须在领域层就被拒绝，不能带着注定装不下的密文去写库。
    /// </summary>
    /// <remarks>
    /// 回归锚点：ApiKey 曾只走 NormalizeNullable（仅 trim）而无任何长度校验（对比同文件里 BaseUrl 走 Optional）。
    /// 落库的是密文不是明文，Data Protection 密文约为明文的 4/3 再加固定头部，明文越过约 1432 字符后
    /// 就超出 Api_Key 列长 2000，写库会被截断（密文截断即永久解不开）或直接报错；
    /// 上限 1000 的推导见 AiProviderDomainService.ApiKeyMaxLength 的注释，
    /// 密文侧边界由 AiProviderSecretProtectorTests.Protect_MaxLengthPlaintextCipherShouldFitColumn 对实际列长锁定。
    /// </remarks>
    [Fact]
    public async Task CreateProviderAsync_ApiKeyOverMaxLengthShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateProviderAsync(
                AiTestHelper.CreateProviderCommand() with { ApiKey = new string('k', 1001) }));

        Assert.Contains("API 密钥不能超过 1000 个字符", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 明文密钥恰好 1000 字符必须通过并照常加密，边界不得少一格。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ApiKeyAtMaxLengthShouldPass()
    {
        var fixture = CreateFixture();
        var apiKey = new string('k', 1000);

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { ApiKey = apiKey });

        Assert.Equal("cipher:" + apiKey, result.Provider.ApiKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 密钥长度以裁剪后的明文计：两端空白不计入上限。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ApiKeyLengthShouldBeMeasuredAfterTrim()
    {
        var fixture = CreateFixture();
        var apiKey = new string('k', 1000);

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { ApiKey = "  " + apiKey + "\n" });

        Assert.Equal("cipher:" + apiKey, result.Provider.ApiKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空命令必须在任何仓储调用前被拒。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_NullCommandShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.CreateProviderAsync(null!));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 已取消的请求不得产生任何落库副作用。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_CancelledTokenShouldRejectBeforeRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand(), source.Token));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 非法枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand() with { Status = (EnableStatus)99 }));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 采样温度越界必须拒绝，超出模型接受区间会被上游直接 400。
    /// </summary>
    /// <remarks>
    /// 回归锚点：<see cref="float.NaN"/> 曾被放行落库——旧实现用反向判定 <c>is &lt; 0f or > 2f</c>，
    /// 而 IEEE754 下 NaN 的两个比较都为 false。校验必须改成"必须落进 [0,2]"的正向判定，
    /// 让 NaN 与 ±Infinity 走同一条拒绝路径。
    /// </remarks>
    /// <param name="temperature">越界的采样温度。</param>
    [Theory]
    [InlineData(-0.01f)]
    [InlineData(-1f)]
    [InlineData(2.01f)]
    [InlineData(100f)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NaN)]
    public async Task CreateProviderAsync_TemperatureOutOfRangeShouldReject(float temperature)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand() with { Temperature = temperature }));

        Assert.Contains("采样温度须在 0~2 之间", exception.Message, StringComparison.Ordinal);
        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 采样温度闭区间端点与"不设置"必须原样通过。
    /// </summary>
    /// <param name="temperature">合法的采样温度（null 表示交由上游默认）。</param>
    [Theory]
    [InlineData(0f)]
    [InlineData(2f)]
    [InlineData(1f)]
    [InlineData(null)]
    public async Task CreateProviderAsync_TemperatureAtBoundaryShouldPass(float? temperature)
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { Temperature = temperature });

        Assert.Equal(temperature, result.Provider.Temperature);
    }

    /// <summary>
    /// 配置编码为空必须拒绝（编码是租户内唯一键，也是解析器的缓存键）。
    /// </summary>
    /// <param name="configCode">待校验的配置编码。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProviderAsync_BlankConfigCodeShouldReject(string? configCode)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand() with { ConfigCode = configCode! }));

        fixture.Repository.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 配置编码内部含空白必须拒绝，否则按编码解析 provider 会永远落空。
    /// </summary>
    /// <param name="configCode">含内部空白字符的编码。</param>
    [Theory]
    [InlineData("co de")]
    [InlineData("co\tde")]
    [InlineData("co\nde")]
    public async Task CreateProviderAsync_ConfigCodeWithInnerWhitespaceShouldReject(string configCode)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand() with { ConfigCode = configCode }));

        Assert.Equal("配置编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 租户内配置编码重复必须给出友好错误且一次都不写库，否则会撞唯一索引 UX_TeId_CoCd 报 500。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_DuplicateConfigCodeShouldRejectBeforeWrite()
    {
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.ExistsCodeAsync("config-code", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand()));

        Assert.Equal("配置编码已存在。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 各字段的长度上限必须与实体列长度一一对应，越界即拒绝。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("ConfigCode", 100)]
    [InlineData("ConfigName", 200)]
    [InlineData("Provider", 50)]
    [InlineData("Model", 100)]
    [InlineData("EmbeddingModel", 100)]
    [InlineData("BaseUrl", 500)]
    [InlineData("Remark", 500)]
    public async Task CreateProviderAsync_FieldOverMaxLengthShouldReject(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var over = new string('x', maxLength + 1);
        var command = AiTestHelper.CreateProviderCommand();
        command = fieldName switch
        {
            "ConfigCode" => command with { ConfigCode = over },
            "ConfigName" => command with { ConfigName = over },
            "Provider" => command with { Provider = over },
            "Model" => command with { Model = over },
            "EmbeddingModel" => command with { EmbeddingModel = over },
            "BaseUrl" => command with { BaseUrl = over },
            _ => command with { Remark = over }
        };

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.CreateProviderAsync(command));
    }

    /// <summary>
    /// 各字段恰好取到长度上限必须通过，边界不得少一格。
    /// </summary>
    /// <param name="fieldName">被测字段名。</param>
    /// <param name="maxLength">该字段的列长度上限。</param>
    [Theory]
    [InlineData("ConfigCode", 100)]
    [InlineData("ConfigName", 200)]
    [InlineData("Provider", 50)]
    [InlineData("Model", 100)]
    [InlineData("EmbeddingModel", 100)]
    [InlineData("BaseUrl", 500)]
    [InlineData("Remark", 500)]
    public async Task CreateProviderAsync_FieldAtMaxLengthShouldPass(string fieldName, int maxLength)
    {
        var fixture = CreateFixture();
        var exact = new string('x', maxLength);
        var command = AiTestHelper.CreateProviderCommand();
        command = fieldName switch
        {
            "ConfigCode" => command with { ConfigCode = exact },
            "ConfigName" => command with { ConfigName = exact },
            "Provider" => command with { Provider = exact },
            "Model" => command with { Model = exact },
            "EmbeddingModel" => command with { EmbeddingModel = exact },
            "BaseUrl" => command with { BaseUrl = exact },
            _ => command with { Remark = exact }
        };

        var result = await fixture.Service.CreateProviderAsync(command);

        var actual = fieldName switch
        {
            "ConfigCode" => result.Provider.ConfigCode,
            "ConfigName" => result.Provider.ConfigName,
            "Provider" => result.Provider.Provider,
            "Model" => result.Provider.Model,
            "EmbeddingModel" => result.Provider.EmbeddingModel,
            "BaseUrl" => result.Provider.BaseUrl,
            _ => result.Provider.Remark
        };
        Assert.Equal(exact, actual, StringComparer.Ordinal);
    }

    /// <summary>
    /// 扩展 JSON 是 BigString 列，只做空白归一、不设长度上限，超长内容必须能落库。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_VeryLongExtraJsonShouldPass()
    {
        var fixture = CreateFixture();
        var extraJson = "{\"a\":\"" + new string('j', 20_000) + "\"}";

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { ExtraJson = extraJson });

        Assert.Equal(extraJson, result.Provider.ExtraJson, StringComparer.Ordinal);
    }

    /// <summary>
    /// 新建默认 provider 必须把租户内其它默认行逐条置为非默认，否则运行时取哪份配置不确定。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_DefaultProviderShouldClearOtherDefaults()
    {
        var other = AiTestHelper.CreateProvider(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture();
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        var result = await fixture.Service.CreateProviderAsync(
            AiTestHelper.CreateProviderCommand() with { IsDefault = true });

        Assert.True(result.Provider.IsDefault);
        Assert.False(other.IsDefault);
        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(result.Provider.BasicId, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Repository.Verify(repository => repository.UpdateAsync(other, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 非默认 provider 一次都不能触发默认互斥清理（避免无谓的全表扫与写）。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_NonDefaultShouldNotTouchOtherDefaults()
    {
        var fixture = CreateFixture();

        _ = await fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand() with { IsDefault = false });

        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建时传入的取消令牌必须原样透传给每一次仓储调用。
    /// </summary>
    [Fact]
    public async Task CreateProviderAsync_ShouldForwardCancellationTokenToRepository()
    {
        var fixture = CreateFixture();
        using var source = new CancellationTokenSource();

        _ = await fixture.Service.CreateProviderAsync(AiTestHelper.CreateProviderCommand(), source.Token);

        fixture.Repository.Verify(repository => repository.ExistsCodeAsync(It.IsAny<string>(), null, source.Token), Times.Once);
        fixture.Repository.Verify(repository => repository.AddAsync(It.IsAny<SysAiProvider>(), source.Token), Times.Once);
    }

    /// <summary>
    /// 更新时的主键必须为正，非法主键要在查库前被拒。
    /// </summary>
    /// <param name="basicId">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateProviderAsync_NonPositiveIdShouldReject(long basicId)
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(basicId)));

        Assert.Contains("provider 主键必须大于 0", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目标 provider 不存在必须给出明确错误而不是空引用。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_MissingProviderShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7)));

        Assert.Equal("provider 不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 配置编码不可变：更新命令里根本没有该字段，更新后实体编码必须与原编码逐字相同。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_ShouldKeepConfigCodeImmutable()
    {
        var existing = AiTestHelper.CreateProvider(7, "original-code");
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7));

        Assert.Equal("original-code", result.Provider.ConfigCode, StringComparer.Ordinal);
        Assert.DoesNotContain(
            "ConfigCode",
            AiTestHelper.GetRecordParameterNames(typeof(AiProviderUpdateCommand)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新必须把命令里的每一个可变字段整体回写到实体上。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_ShouldOverwriteAllMutableFields()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7));

        Assert.Equal("新配置名", result.Provider.ConfigName, StringComparer.Ordinal);
        Assert.Equal("DeepSeek", result.Provider.Provider, StringComparer.Ordinal);
        Assert.Equal("deepseek-chat", result.Provider.Model, StringComparer.Ordinal);
        Assert.Equal("bge-m3", result.Provider.EmbeddingModel, StringComparer.Ordinal);
        Assert.Equal("https://api.new.com", result.Provider.BaseUrl, StringComparer.Ordinal);
        Assert.Equal(2048, result.Provider.MaxOutputTokens);
        Assert.Equal(1.2f, result.Provider.Temperature);
        Assert.Equal(60, result.Provider.TimeoutSeconds);
        Assert.Equal("{\"b\":2}", result.Provider.ExtraJson, StringComparer.Ordinal);
        Assert.Equal(2, result.Provider.Sort);
        Assert.Equal("新备注", result.Provider.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时留空密钥必须保留原密文，且一次都不能再调用加密器——这是"密钥只写不回读"表单的前提。
    /// </summary>
    /// <param name="apiKey">留空的密钥输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProviderAsync_BlankApiKeyShouldKeepStoredCipher(string? apiKey)
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderAsync(
            AiTestHelper.UpdateProviderCommand(7) with { ApiKey = apiKey });

        Assert.Equal("dp:old-cipher", result.Provider.ApiKey, StringComparer.Ordinal);
        fixture.Protector.Verify(protector => protector.Protect(It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// 更新时给出新密钥必须重新加密覆盖旧密文。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_NewApiKeyShouldReplaceCipher()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderAsync(
            AiTestHelper.UpdateProviderCommand(7) with { ApiKey = "  sk-new-key  " });

        Assert.Equal("cipher:sk-new-key", result.Provider.ApiKey, StringComparer.Ordinal);
        fixture.Protector.Verify(protector => protector.Protect("sk-new-key"), Times.Once);
    }

    /// <summary>
    /// 更新路径同样必须卡密钥长度，不得只在创建时把关（回归锚点，原实现两条路径都无长度校验）。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_ApiKeyOverMaxLengthShouldReject()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.ApiKey = "dp:old-cipher";
        var fixture = CreateFixture(existing);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateProviderAsync(
                AiTestHelper.UpdateProviderCommand(7) with { ApiKey = new string('k', 1001) }));

        Assert.Contains("API 密钥不能超过 1000 个字符", exception.Message, StringComparison.Ordinal);
        Assert.Equal("dp:old-cipher", existing.ApiKey, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 更新路径的密钥恰好 1000 字符必须通过并重新加密。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_ApiKeyAtMaxLengthShouldPass()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.ApiKey = "dp:old-cipher";
        var fixture = CreateFixture(existing);
        var apiKey = new string('k', 1000);

        var result = await fixture.Service.UpdateProviderAsync(
            AiTestHelper.UpdateProviderCommand(7) with { ApiKey = apiKey });

        Assert.Equal("cipher:" + apiKey, result.Provider.ApiKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新路径同样必须校验采样温度，不得只在创建时把关。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_TemperatureOutOfRangeShouldRejectBeforeLoad()
    {
        var fixture = CreateFixture(AiTestHelper.CreateProvider(7));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7) with { Temperature = 2.5f }));

        Assert.Contains("采样温度须在 0~2 之间", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 更新时把 provider 设为默认同样要触发单默认互斥清理。
    /// </summary>
    [Fact]
    public async Task UpdateProviderAsync_DefaultProviderShouldClearOtherDefaults()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var other = AiTestHelper.CreateProvider(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        _ = await fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7) with { IsDefault = true });

        Assert.False(other.IsDefault);
        fixture.Repository.Verify(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 状态变更必须双校验主键与枚举。
    /// </summary>
    [Fact]
    public async Task UpdateProviderStatusAsync_UndefinedStatusShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreateProvider(7));

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.UpdateProviderStatusAsync(new AiProviderStatusChangeCommand(7, (EnableStatus)99, null)));

        Assert.Contains("枚举值无效", exception.Message, StringComparison.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 状态变更时空白备注必须保留原备注，否则一次停用就把历史备注抹成 null。
    /// </summary>
    /// <param name="remark">空白备注输入。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProviderStatusAsync_BlankRemarkShouldKeepOriginal(string? remark)
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderStatusAsync(
            new AiProviderStatusChangeCommand(7, EnableStatus.Disabled, remark));

        Assert.Equal(EnableStatus.Disabled, result.Provider.Status);
        Assert.Equal("原备注", result.Provider.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态变更时非空备注必须覆盖原值并裁掉两端空白。
    /// </summary>
    [Fact]
    public async Task UpdateProviderStatusAsync_NonBlankRemarkShouldOverwrite()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.UpdateProviderStatusAsync(
            new AiProviderStatusChangeCommand(7, EnableStatus.Disabled, "  停用原因  "));

        Assert.Equal("停用原因", result.Provider.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已禁用的 provider 不得设为默认，否则默认配置不可用会让全部推理开局即报错。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_DisabledProviderShouldReject()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.IsEnabled = false;
        var fixture = CreateFixture(existing);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SetDefaultAsync(7));

        Assert.Equal("已禁用的 provider 不能设为默认。", exception.Message, StringComparer.Ordinal);
        fixture.Repository.Verify(
            repository => repository.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 目标本非默认时必须置为默认并写库，同时清理其它默认行。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_NonDefaultProviderShouldPromoteAndClearOthers()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var other = AiTestHelper.CreateProvider(9, "other");
        other.IsDefault = true;
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);

        var result = await fixture.Service.SetDefaultAsync(7);

        Assert.True(result.Provider.IsDefault);
        Assert.False(other.IsDefault);
        fixture.Repository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 目标本已是默认时只清理其它行、不再对自身发起一次无意义的写。
    /// </summary>
    [Fact]
    public async Task SetDefaultAsync_AlreadyDefaultShouldNotWriteItself()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.IsDefault = true;
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.SetDefaultAsync(7);

        Assert.True(result.Provider.IsDefault);
        fixture.Repository.Verify(repository => repository.GetOtherDefaultsAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Repository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 设为默认时非法主键必须在查库前被拒。
    /// </summary>
    /// <param name="id">非法主键值。</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task SetDefaultAsync_NonPositiveIdShouldReject(long id)
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.SetDefaultAsync(id));

        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 删除不存在的 provider 必须拒绝。
    /// </summary>
    [Fact]
    public async Task DeleteProviderAsync_MissingProviderShouldReject()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteProviderAsync(7));

        Assert.Equal("provider 不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 必须抛出，否则删除失败被当成成功会让前端列表与库不一致。
    /// </summary>
    [Fact]
    public async Task DeleteProviderAsync_RepositoryFailureShouldReject()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);
        _ = fixture.Repository
            .Setup(repository => repository.DeleteAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DeleteProviderAsync(7));

        Assert.Equal("provider 删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除成功路径必须把取消令牌透传给仓储。
    /// </summary>
    [Fact]
    public async Task DeleteProviderAsync_ShouldForwardCancellationToken()
    {
        var existing = AiTestHelper.CreateProvider(7);
        var fixture = CreateFixture(existing);
        using var source = new CancellationTokenSource();

        await fixture.Service.DeleteProviderAsync(7, source.Token);

        fixture.Repository.Verify(repository => repository.GetByIdAsync(7, source.Token), Times.Once);
        fixture.Repository.Verify(repository => repository.DeleteAsync(existing, source.Token), Times.Once);
    }

    /// <summary>
    /// 连接测试必须先解密密钥再建客户端，否则拿密文当密钥去请求上游必然 401。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_ShouldDecryptApiKeyBeforeProbing()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.Model = string.Empty;
        existing.EmbeddingModel = null;
        var fixture = CreateFixture(existing);

        _ = await fixture.Service.TestConnectionAsync(7);

        fixture.Protector.Verify(protector => protector.Unprotect("dp:old-cipher"), Times.Once);
    }

    /// <summary>
    /// 建客户端阶段就失败时，会话探测必须降级为"失败结果"而不是把异常抛给调用方（测试连接本身不应 500）。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_ChatClientFailureShouldReturnFailedProbeInsteadOfThrowing()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.Model = string.Empty;
        existing.EmbeddingModel = null;
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.TestConnectionAsync(7);

        Assert.False(result.Chat.Success);
        Assert.NotNull(result.Chat.Message);
        Assert.Contains("未配置 Model", result.Chat.Message!, StringComparison.Ordinal);
        Assert.True(result.Chat.LatencyMs >= 0);
        Assert.False(result.Success);
    }

    /// <summary>
    /// 未配置嵌入模型时不得发起嵌入探测，结果里的 Embedding 必须为 null（前端据此隐藏该行）。
    /// </summary>
    /// <param name="embeddingModel">空白的嵌入模型名。</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TestConnectionAsync_WithoutEmbeddingModelShouldSkipEmbeddingProbe(string? embeddingModel)
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.Model = string.Empty;
        existing.EmbeddingModel = embeddingModel;
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.TestConnectionAsync(7);

        Assert.Null(result.Embedding);
    }

    /// <summary>
    /// 配置了嵌入模型就必须单独探测一次，失败同样降级为失败结果且不回报维度。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_EmbeddingFailureShouldReturnFailedProbeWithoutDimensions()
    {
        var existing = AiTestHelper.CreateProvider(7);
        existing.Model = string.Empty;
        existing.EmbeddingModel = "bge-m3";
        existing.BaseUrl = "::not a uri::";
        var fixture = CreateFixture(existing);

        var result = await fixture.Service.TestConnectionAsync(7);

        Assert.NotNull(result.Embedding);
        Assert.False(result.Embedding!.Success);
        Assert.Null(result.Embedding.Dimensions);
        Assert.Equal("bge-m3", result.Embedding.Model, StringComparer.Ordinal);
        Assert.False(result.Success);
    }

    /// <summary>
    /// 连接测试的非法主键与不存在同样要在建客户端之前被拒。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_InvalidIdAndMissingProviderShouldReject()
    {
        var fixture = CreateFixture();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.TestConnectionAsync(0));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.TestConnectionAsync(7));

        Assert.Equal("provider 不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已取消的连接测试必须在查库前直接抛出。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_CancelledTokenShouldRejectBeforeRepository()
    {
        var fixture = CreateFixture(AiTestHelper.CreateProvider(7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.TestConnectionAsync(7, source.Token));

        fixture.Repository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 总体可用性口径：会话必须通过；配置了嵌入模型时嵌入也必须通过，未配置则不参与判定。
    /// </summary>
    /// <param name="chatSuccess">会话探测是否成功。</param>
    /// <param name="hasEmbedding">是否配置了嵌入模型。</param>
    /// <param name="embeddingSuccess">嵌入探测是否成功。</param>
    /// <param name="expected">期望的总体可用性。</param>
    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)]
    public void AiProviderTestResult_SuccessShouldRequireChatAndConfiguredEmbedding(
        bool chatSuccess,
        bool hasEmbedding,
        bool embeddingSuccess,
        bool expected)
    {
        var result = new AiProviderTestResult(
            new AiProviderChatProbe(chatSuccess, null, 1, "gpt-4o-mini"),
            hasEmbedding ? new AiProviderEmbeddingProbe(embeddingSuccess, null, 1, "bge-m3", embeddingSuccess ? 1024 : null) : null);

        Assert.Equal(expected, result.Success);
    }

    /// <summary>
    /// 除创建外的写方法同样要拒绝空命令与已取消令牌。
    /// </summary>
    [Fact]
    public async Task AllWriteMethods_NullCommandAndCancelledTokenShouldReject()
    {
        var fixture = CreateFixture(AiTestHelper.CreateProvider(7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateProviderAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.UpdateProviderStatusAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdateProviderAsync(AiTestHelper.UpdateProviderCommand(7), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.UpdateProviderStatusAsync(new AiProviderStatusChangeCommand(7, EnableStatus.Enabled, null), source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.SetDefaultAsync(7, source.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.DeleteProviderAsync(7, source.Token));
    }

    /// <summary>
    /// 构造纯内存测试夹具：仓储回传入参，密钥保护器用可预测的前缀替身，两个客户端工厂为真实实例但绝不发起网络请求。
    /// </summary>
    /// <param name="existing">GetByIdAsync 命中的既有 provider（null 表示查不到）。</param>
    /// <returns>被测服务与其依赖替身。</returns>
    private static ProviderFixture CreateFixture(SysAiProvider? existing = null)
    {
        var repository = new Mock<IAiProviderRepository>(MockBehavior.Strict);
        _ = repository
            .Setup(item => item.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = repository
            .Setup(item => item.AddAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiProvider entity, CancellationToken _) => AiTestHelper.SetBasicId(entity, 100));
        _ = repository
            .Setup(item => item.UpdateAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysAiProvider entity, CancellationToken _) => entity);
        _ = repository
            .Setup(item => item.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _ = repository
            .Setup(item => item.GetOtherDefaultsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _ = repository
            .Setup(item => item.DeleteAsync(It.IsAny<SysAiProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var protector = new Mock<IAiProviderSecretProtector>(MockBehavior.Strict);
        _ = protector
            .Setup(item => item.Protect(It.IsAny<string?>()))
            .Returns((string? plaintext) => plaintext is null ? null : "cipher:" + plaintext);
        _ = protector
            .Setup(item => item.Unprotect(It.IsAny<string?>()))
            .Returns((string? cipher) => cipher);

        var service = new AiProviderDomainService(
            repository.Object,
            protector.Object,
            new OpenAiCompatibleChatClientFactory(
                Options.Create(new XiHanAiOptions()),
                Options.Create(new AiGuardrailOptions()),
                Array.Empty<IAiGuardrail>()),
            new OpenAiEmbeddingGeneratorFactory());

        return new ProviderFixture(service, repository, protector);
    }

    /// <summary>
    /// Provider 领域服务测试夹具。
    /// </summary>
    /// <param name="Service">被测领域服务。</param>
    /// <param name="Repository">仓储替身。</param>
    /// <param name="Protector">密钥保护器替身。</param>
    private sealed record ProviderFixture(
        AiProviderDomainService Service,
        Mock<IAiProviderRepository> Repository,
        Mock<IAiProviderSecretProtector> Protector);
}
