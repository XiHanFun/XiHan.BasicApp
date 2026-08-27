// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 存储配置领域服务测试：默认存储在租户内唯一且必须处于启用状态，
/// 默认配置既不能停用也不能删除，被文件记录引用的配置禁止删除；
/// 访问密钥必须经保护器加密落库，更新时留空表示保留原密钥（前端脱敏不回显）。
/// </summary>
public sealed class SaasDomainStorageConfigTests
{
    /// <summary>
    /// 创建时配置编码与名称去空白，可选字段纯空白折叠为 null，密钥经保护器加密后落库。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_ShouldTrimTextAndProtectSecret()
    {
        var context = new StorageConfigTestContext();

        var result = await context.Service.CreateStorageConfigAsync(BuildCreateCommand(
            configCode: "  s3-main  ",
            configName: "  主存储  ",
            endpoint: "   ",
            secretAccessKey: "  raw-secret  "));

        Assert.Equal("s3-main", result.Config.ConfigCode, StringComparer.Ordinal);
        Assert.Equal("主存储", result.Config.ConfigName, StringComparer.Ordinal);
        Assert.Null(result.Config.Endpoint);
        Assert.Equal("protected:raw-secret", result.Config.SecretAccessKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 配置编码重复时拒绝创建。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_DuplicateCode_ShouldThrowInvalidOperationException()
    {
        var context = new StorageConfigTestContext();
        context.SetupConfigCodeExists();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand()));

        Assert.Equal("存储配置编码已存在。", exception.Message, StringComparer.Ordinal);
        context.StorageConfigRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysStorageConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建为默认配置时必须同时启用，停用状态下设为默认要被拒绝。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_DefaultButDisabled_ShouldReject()
    {
        var context = new StorageConfigTestContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(isDefault: true, isEnabled: false)));

        Assert.Equal("默认存储配置必须处于启用状态。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 创建为默认配置时必须先把其它默认配置清掉，保证默认存储全局唯一。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_AsDefault_ShouldClearOtherDefaults()
    {
        var context = new StorageConfigTestContext();

        _ = await context.Service.CreateStorageConfigAsync(BuildCreateCommand(isDefault: true, isEnabled: true));

        context.StorageConfigRepository.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Expression<Func<SysStorageConfig, SysStorageConfig>>>(),
                It.IsAny<Expression<Func<SysStorageConfig, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 非默认配置创建时不得触碰其它记录的默认标记。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_NotDefault_ShouldNotTouchOtherDefaults()
    {
        var context = new StorageConfigTestContext();

        _ = await context.Service.CreateStorageConfigAsync(BuildCreateCommand(isDefault: false));

        context.StorageConfigRepository.Verify(
            repo => repo.UpdateAsync(
                It.IsAny<Expression<Func<SysStorageConfig, SysStorageConfig>>>(),
                It.IsAny<Expression<Func<SysStorageConfig, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 对象存储（非本地）必须补齐存储桶、访问密钥ID 与访问密钥三项，缺一即拒。
    /// </summary>
    /// <param name="bucketName">存储桶名称。</param>
    /// <param name="accessKeyId">访问密钥ID。</param>
    /// <param name="secretAccessKey">访问密钥。</param>
    /// <param name="expectedMessage">期望的拒绝提示。</param>
    [Theory]
    [InlineData(null, "ak", "sk", "对象存储配置必须填写存储桶名称。")]
    [InlineData("   ", "ak", "sk", "对象存储配置必须填写存储桶名称。")]
    [InlineData("bucket", null, "sk", "对象存储配置必须填写访问密钥ID。")]
    [InlineData("bucket", "ak", "   ", "对象存储配置必须填写访问密钥。")]
    public async Task CreateStorageConfig_ObjectStorageMissingCredentials_ShouldReject(
        string? bucketName,
        string? accessKeyId,
        string? secretAccessKey,
        string expectedMessage)
    {
        var context = new StorageConfigTestContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(
                storageType: StorageConfigType.S3,
                bucketName: bucketName,
                accessKeyId: accessKeyId,
                secretAccessKey: secretAccessKey)));

        Assert.Equal(expectedMessage, exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 本地存储不受对象存储的凭据必填约束，可以完全不填桶与密钥。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_LocalStorage_ShouldNotRequireObjectStorageCredentials()
    {
        var context = new StorageConfigTestContext();

        var result = await context.Service.CreateStorageConfigAsync(BuildCreateCommand(
            storageType: StorageConfigType.Local,
            bucketName: null,
            accessKeyId: null,
            secretAccessKey: null));

        Assert.Equal(StorageConfigType.Local, result.Config.StorageType);
        Assert.Null(result.Config.SecretAccessKey);
    }

    /// <summary>
    /// 配置编码与名称必填，排序不得为负数，存储类型枚举须已定义。
    /// </summary>
    [Fact]
    public async Task CreateStorageConfig_BasicInputConstraints_ShouldReject()
    {
        var context = new StorageConfigTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(configCode: "   ")));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(configName: null!)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(sort: -1)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateStorageConfigAsync(BuildCreateCommand(storageType: (StorageConfigType)99)));
    }

    /// <summary>
    /// 更新时密钥留空表示保留原密钥，不得把已加密的密钥覆盖成空。
    /// </summary>
    [Fact]
    public async Task UpdateStorageConfig_BlankSecret_ShouldKeepExistingSecret()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.SecretAccessKey = "protected:old";

        _ = await context.Service.UpdateStorageConfigAsync(BuildUpdateCommand(secretAccessKey: "   "));

        Assert.Equal("protected:old", config.SecretAccessKey, StringComparer.Ordinal);
        context.SecretProtector.Verify(protector => protector.Protect(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// 更新时提供了新密钥则必须重新加密落库。
    /// </summary>
    [Fact]
    public async Task UpdateStorageConfig_WithNewSecret_ShouldProtectAndOverwrite()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.SecretAccessKey = "protected:old";

        _ = await context.Service.UpdateStorageConfigAsync(BuildUpdateCommand(secretAccessKey: "  new-secret  "));

        Assert.Equal("protected:new-secret", config.SecretAccessKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新不改动配置编码与默认标记：编码是引用键，默认标记走独立命令。
    /// </summary>
    [Fact]
    public async Task UpdateStorageConfig_ShouldNotChangeCodeOrDefaultFlag()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.ConfigCode = "s3-main";
        config.IsDefault = true;

        _ = await context.Service.UpdateStorageConfigAsync(BuildUpdateCommand(configName: "改名"));

        Assert.Equal("s3-main", config.ConfigCode, StringComparer.Ordinal);
        Assert.True(config.IsDefault);
        Assert.Equal("改名", config.ConfigName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 默认存储配置不能停用，必须先把其它启用配置设为默认。
    /// </summary>
    [Fact]
    public async Task UpdateStorageConfigStatus_DisableDefault_ShouldReject()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.IsDefault = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.UpdateStorageConfigStatusAsync(new StorageConfigStatusChangeCommand(5, false)));

        Assert.Equal("默认存储配置不能停用，请先将其他启用配置设为默认。", exception.Message, StringComparer.Ordinal);
        Assert.True(config.IsEnabled);
    }

    /// <summary>
    /// 非默认配置可以正常停用与启用。
    /// </summary>
    [Fact]
    public async Task UpdateStorageConfigStatus_NonDefault_ShouldToggleEnabled()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.IsDefault = false;

        _ = await context.Service.UpdateStorageConfigStatusAsync(new StorageConfigStatusChangeCommand(5, false));

        Assert.False(config.IsEnabled);
    }

    /// <summary>
    /// 设为默认前必须已启用，停用的配置不得被提升为默认。
    /// </summary>
    [Fact]
    public async Task SetDefaultStorageConfig_DisabledConfig_ShouldReject()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.IsEnabled = false;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.SetDefaultStorageConfigAsync(new StorageConfigDefaultChangeCommand(5)));

        Assert.Equal("默认存储配置必须处于启用状态。", exception.Message, StringComparer.Ordinal);
        Assert.False(config.IsDefault);
    }

    /// <summary>
    /// 设为默认时会先清掉其它记录的默认标记，再把自身置为默认，保证默认唯一。
    /// </summary>
    [Fact]
    public async Task SetDefaultStorageConfig_ShouldClearOthersThenMarkItself()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.IsEnabled = true;

        var result = await context.Service.SetDefaultStorageConfigAsync(new StorageConfigDefaultChangeCommand(5));

        Assert.True(result.Config.IsDefault);
        Assert.NotNull(context.CapturedClearDefaultPredicate);
        var compiled = context.CapturedClearDefaultPredicate!.Compile();
        var other = new SysStorageConfig { IsDefault = true };
        SaasTestHelper.SetBasicId(other, 6);
        // 自身必须被排除在清理范围之外，否则刚置位又被清掉
        Assert.True(compiled(other));
        Assert.False(compiled(config));
    }

    /// <summary>
    /// 默认存储配置不能删除，必须先改默认。
    /// </summary>
    [Fact]
    public async Task DeleteStorageConfig_DefaultConfig_ShouldReject()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();
        config.IsDefault = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeleteStorageConfigAsync(5));

        Assert.Equal("默认存储配置不能删除，请先将其他配置设为默认。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已被文件存储记录引用的配置禁止删除，避免历史文件失去可解析的存储配置。
    /// </summary>
    [Fact]
    public async Task DeleteStorageConfig_ReferencedByFileStorage_ShouldReject()
    {
        var context = new StorageConfigTestContext();
        _ = context.SetupExistingConfig();
        _ = context.FileStorageRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<Expression<Func<SysFileStorage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeleteStorageConfigAsync(5));

        Assert.Equal("存储配置已被文件存储记录引用，禁止删除。", exception.Message, StringComparer.Ordinal);
        context.StorageConfigRepository.Verify(
            repo => repo.DeleteAsync(It.IsAny<SysStorageConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 无引用的非默认配置可正常删除；仓储返回失败必须显式抛出。
    /// </summary>
    [Fact]
    public async Task DeleteStorageConfig_ShouldSucceedWhenUnreferencedAndSurfaceFailure()
    {
        var context = new StorageConfigTestContext();
        var config = context.SetupExistingConfig();

        var result = await context.Service.DeleteStorageConfigAsync(5);
        Assert.Same(config, result.Config);

        var failureContext = new StorageConfigTestContext();
        var failing = failureContext.SetupExistingConfig();
        _ = failureContext.StorageConfigRepository
            .Setup(repo => repo.DeleteAsync(failing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => failureContext.Service.DeleteStorageConfigAsync(5));
        Assert.Equal("存储配置删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 存储配置主键必须为正数，目标不存在时给出明确拒绝。
    /// </summary>
    [Fact]
    public async Task StorageConfigCommands_InvalidIdOrMissingTarget_ShouldReject()
    {
        var context = new StorageConfigTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdateStorageConfigAsync(BuildUpdateCommand(basicId: 0)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdateStorageConfigStatusAsync(new StorageConfigStatusChangeCommand(0, true)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.SetDefaultStorageConfigAsync(new StorageConfigDefaultChangeCommand(0)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.DeleteStorageConfigAsync(0));

        _ = context.StorageConfigRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysStorageConfig?)null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeleteStorageConfigAsync(5));
        Assert.Equal("存储配置不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 命令对象为空必须抛空引用异常，取消令牌必须在访问仓储之前生效。
    /// </summary>
    [Fact]
    public async Task StorageConfigCommands_NullCommandAndCancelledToken_ShouldThrow()
    {
        var context = new StorageConfigTestContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.CreateStorageConfigAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdateStorageConfigAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdateStorageConfigStatusAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.SetDefaultStorageConfigAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.DeleteStorageConfigAsync(5, cancellation.Token));
        context.StorageConfigRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static StorageConfigCreateCommand BuildCreateCommand(
        string configCode = "s3-main",
        string configName = "主存储",
        StorageConfigType storageType = StorageConfigType.S3,
        string? endpoint = "https://s3.example.com",
        string? region = "cn-north-1",
        string? bucketName = "bucket",
        string? accessKeyId = "ak",
        string? secretAccessKey = "sk",
        bool isDefault = false,
        bool isEnabled = true,
        int sort = 0,
        string? remark = null)
    {
        return new StorageConfigCreateCommand(
            configCode,
            configName,
            storageType,
            endpoint,
            region,
            bucketName,
            accessKeyId,
            secretAccessKey,
            isDefault,
            isEnabled,
            sort,
            remark);
    }

    private static StorageConfigUpdateCommand BuildUpdateCommand(
        long basicId = 5,
        string configName = "主存储",
        StorageConfigType storageType = StorageConfigType.S3,
        string? endpoint = "https://s3.example.com",
        string? region = "cn-north-1",
        string? bucketName = "bucket",
        string? accessKeyId = "ak",
        string? secretAccessKey = null,
        int sort = 0,
        string? remark = null)
    {
        return new StorageConfigUpdateCommand(
            basicId,
            configName,
            storageType,
            endpoint,
            region,
            bucketName,
            accessKeyId,
            secretAccessKey,
            sort,
            remark);
    }

    /// <summary>
    /// 存储配置领域服务的依赖装配夹具：密钥保护器以可预测前缀模拟加密。
    /// </summary>
    private sealed class StorageConfigTestContext
    {
        internal StorageConfigTestContext()
        {
            StorageConfigRepository = new Mock<IStorageConfigRepository>();
            FileStorageRepository = new Mock<IFileStorageRepository>();
            SecretProtector = new Mock<IStorageSecretProtector>();

            _ = SecretProtector
                .Setup(protector => protector.Protect(It.IsAny<string?>()))
                .Returns((string? plaintext) => plaintext is null ? null : $"protected:{plaintext}");
            _ = StorageConfigRepository
                .Setup(repo => repo.AnyAsync(
                    It.IsAny<Expression<Func<SysStorageConfig, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _ = FileStorageRepository
                .Setup(repo => repo.AnyAsync(
                    It.IsAny<Expression<Func<SysFileStorage, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _ = StorageConfigRepository
                .Setup(repo => repo.AddAsync(It.IsAny<SysStorageConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysStorageConfig entity, CancellationToken _) => entity);
            _ = StorageConfigRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysStorageConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysStorageConfig entity, CancellationToken _) => entity);
            _ = StorageConfigRepository
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Expression<Func<SysStorageConfig, SysStorageConfig>>>(),
                    It.IsAny<Expression<Func<SysStorageConfig, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Expression<Func<SysStorageConfig, SysStorageConfig>>, Expression<Func<SysStorageConfig, bool>>, CancellationToken>(
                    (_, predicate, _) => CapturedClearDefaultPredicate = predicate)
                .ReturnsAsync(true);
            _ = StorageConfigRepository
                .Setup(repo => repo.DeleteAsync(It.IsAny<SysStorageConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            Service = new StorageConfigDomainService(
                StorageConfigRepository.Object,
                FileStorageRepository.Object,
                SecretProtector.Object);
        }

        internal Expression<Func<SysStorageConfig, bool>>? CapturedClearDefaultPredicate { get; private set; }

        internal Mock<IFileStorageRepository> FileStorageRepository { get; }

        internal Mock<IStorageSecretProtector> SecretProtector { get; }

        internal StorageConfigDomainService Service { get; }

        internal Mock<IStorageConfigRepository> StorageConfigRepository { get; }

        internal SysStorageConfig SetupExistingConfig()
        {
            var config = new SysStorageConfig
            {
                ConfigCode = "s3-main",
                ConfigName = "主存储",
                StorageType = StorageConfigType.S3,
                BucketName = "bucket",
                AccessKeyId = "ak",
                IsEnabled = true
            };
            SaasTestHelper.SetBasicId(config, 5);
            _ = StorageConfigRepository
                .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            return config;
        }

        internal void SetupConfigCodeExists()
        {
            _ = StorageConfigRepository
                .Setup(repo => repo.AnyAsync(
                    It.IsAny<Expression<Func<SysStorageConfig, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }
    }
}
