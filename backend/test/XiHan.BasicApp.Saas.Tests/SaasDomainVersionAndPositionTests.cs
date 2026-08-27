// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 系统版本与岗位领域服务测试。
/// 版本记录承载「是否正在升级」这一开关：置为升级中时必须补齐升级开始时间，升级中的记录禁止删除；
/// 岗位编码是租户内唯一键，创建时查重、更新时不可改码也不可改状态（状态走独立命令）。
/// </summary>
public sealed class SaasDomainVersionAndPositionTests
{
    /// <summary>
    /// 创建版本时对必填与可选文本统一去空白，纯空白的可选字段折叠为 null。
    /// </summary>
    [Fact]
    public async Task CreateVersion_ShouldTrimTextAndNormalizeBlankOptionalFields()
    {
        var context = new VersionTestContext();

        var result = await context.Service.CreateVersionAsync(new VersionCreateCommand(
            "  1.2.3  ", "  0.9.0  ", "   ", false, "   ", null));

        Assert.Equal("1.2.3", result.Version.AppVersion, StringComparer.Ordinal);
        Assert.Equal("0.9.0", result.Version.DbVersion, StringComparer.Ordinal);
        Assert.Null(result.Version.MinSupportVersion);
        Assert.Null(result.Version.UpgradeNode);
    }

    /// <summary>
    /// 非升级中且未显式给出开始时间时，升级开始时间保持为空，不得凭空补当前时间。
    /// </summary>
    [Fact]
    public async Task CreateVersion_NotUpgrading_ShouldKeepUpgradeStartTimeNull()
    {
        var context = new VersionTestContext();

        var result = await context.Service.CreateVersionAsync(new VersionCreateCommand(
            "1.2.3", "0.9.0", null, false, null, null));

        Assert.False(result.Version.IsUpgrading);
        Assert.Null(result.Version.UpgradeStartTime);
    }

    /// <summary>
    /// 标记为升级中而未给开始时间时，必须自动补一个当前时刻，避免出现「升级中但不知何时开始」的记录。
    /// </summary>
    [Fact]
    public async Task CreateVersion_UpgradingWithoutStartTime_ShouldFillCurrentTime()
    {
        var context = new VersionTestContext();
        var before = DateTimeOffset.UtcNow;

        var result = await context.Service.CreateVersionAsync(new VersionCreateCommand(
            "1.2.3", "0.9.0", null, true, "node-1", null));

        var after = DateTimeOffset.UtcNow;
        Assert.True(result.Version.IsUpgrading);
        Assert.NotNull(result.Version.UpgradeStartTime);
        Assert.InRange(result.Version.UpgradeStartTime!.Value, before, after);
    }

    /// <summary>
    /// 显式给出的升级开始时间必须原样保留，不被当前时间覆盖。
    /// </summary>
    [Fact]
    public async Task CreateVersion_UpgradingWithExplicitStartTime_ShouldKeepGivenTime()
    {
        var context = new VersionTestContext();
        var startTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await context.Service.CreateVersionAsync(new VersionCreateCommand(
            "1.2.3", "0.9.0", null, true, "node-1", startTime));

        Assert.Equal(startTime, result.Version.UpgradeStartTime);
    }

    /// <summary>
    /// 应用版本与数据库版本必填，超过 64 字符或升级节点超过 128 字符时拒绝。
    /// </summary>
    [Fact]
    public async Task CreateVersion_TextConstraints_ShouldRejectBlankRequiredAndOverLongValues()
    {
        var context = new VersionTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateVersionAsync(new VersionCreateCommand("   ", "0.9.0", null, false, null, null)));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreateVersionAsync(new VersionCreateCommand("1.2.3", null!, null, false, null, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateVersionAsync(new VersionCreateCommand(new string('v', 65), "0.9.0", null, false, null, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateVersionAsync(new VersionCreateCommand("1.2.3", "0.9.0", new string('m', 65), false, null, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreateVersionAsync(new VersionCreateCommand("1.2.3", "0.9.0", null, false, new string('n', 129), null)));
    }

    /// <summary>
    /// 开始升级会置位升级中并写入开始时间；缺省开始时间时补当前时刻。
    /// </summary>
    [Fact]
    public async Task StartVersionUpgrade_ShouldMarkUpgradingAndStampStartTime()
    {
        var context = new VersionTestContext();
        var version = context.SetupExistingVersion();
        var before = DateTimeOffset.UtcNow;

        var result = await context.Service.StartVersionUpgradeAsync(new VersionUpgradeStartCommand(5, "  node-1  ", null));

        Assert.True(result.Version.IsUpgrading);
        Assert.Equal("node-1", version.UpgradeNode, StringComparer.Ordinal);
        Assert.NotNull(version.UpgradeStartTime);
        Assert.InRange(version.UpgradeStartTime!.Value, before, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 完成升级会清除升级中标记；应用版本与数据库版本只有在命令显式给出非空值时才覆盖。
    /// </summary>
    [Fact]
    public async Task FinishVersionUpgrade_ShouldClearFlagAndOnlyOverwriteProvidedVersions()
    {
        var context = new VersionTestContext();
        var version = context.SetupExistingVersion();
        version.IsUpgrading = true;
        version.AppVersion = "1.0.0";
        version.DbVersion = "0.9.0";
        version.MinSupportVersion = "0.8.0";

        _ = await context.Service.FinishVersionUpgradeAsync(new VersionUpgradeFinishCommand(5, "  2.0.0  ", "   ", null));

        Assert.False(version.IsUpgrading);
        Assert.Equal("2.0.0", version.AppVersion, StringComparer.Ordinal);
        Assert.Equal("0.9.0", version.DbVersion, StringComparer.Ordinal);
        Assert.Equal("0.8.0", version.MinSupportVersion, StringComparer.Ordinal);
    }

    /// <summary>
    /// 升级中的版本记录禁止删除，防止把正在执行的升级上下文抹掉。
    /// </summary>
    [Fact]
    public async Task DeleteVersion_WhileUpgrading_ShouldReject()
    {
        var context = new VersionTestContext();
        var version = context.SetupExistingVersion();
        version.IsUpgrading = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.DeleteVersionAsync(5));

        Assert.Equal("系统升级中的版本记录不能删除。", exception.Message, StringComparer.Ordinal);
        context.Repository.Verify(
            repo => repo.DeleteAsync(It.IsAny<SysVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 删除失败必须显式抛出，不能静默当成成功。
    /// </summary>
    [Fact]
    public async Task DeleteVersion_RepositoryFailure_ShouldThrowInvalidOperationException()
    {
        var context = new VersionTestContext();
        var version = context.SetupExistingVersion();
        _ = context.Repository
            .Setup(repo => repo.DeleteAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.DeleteVersionAsync(5));

        Assert.Equal("系统版本删除失败。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 版本主键必须为正数，目标不存在时给出明确拒绝。
    /// </summary>
    [Fact]
    public async Task VersionCommands_InvalidIdOrMissingTarget_ShouldReject()
    {
        var context = new VersionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.Service.DeleteVersionAsync(0));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdateVersionAsync(new VersionUpdateCommand(0, "1.2.3", "0.9.0", null, false, null, null)));

        _ = context.Repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysVersion?)null);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.StartVersionUpgradeAsync(new VersionUpgradeStartCommand(5, null, null)));
        Assert.Equal("系统版本不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 版本命令对象为空必须抛空引用异常，取消令牌必须在查库之前生效。
    /// </summary>
    [Fact]
    public async Task VersionCommands_NullCommandAndCancelledToken_ShouldThrow()
    {
        var context = new VersionTestContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.CreateVersionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdateVersionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.StartVersionUpgradeAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.FinishVersionUpgradeAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.DeleteVersionAsync(5, cancellation.Token));
        context.Repository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 岗位编码去空白后落库，且创建时按「不排除任何记录」的口径查重。
    /// </summary>
    [Fact]
    public async Task CreatePosition_ShouldTrimCodeAndCheckUniquenessWithoutExclusion()
    {
        var context = new PositionTestContext();

        var result = await context.Service.CreatePositionAsync(new PositionCreateCommand(
            "  dev  ", "  研发岗  ", EnableStatus.Enabled, 10, "   "));

        Assert.Equal("dev", result.Position.PositionCode, StringComparer.Ordinal);
        Assert.Equal("研发岗", result.Position.PositionName, StringComparer.Ordinal);
        Assert.Null(result.Position.Remark);
        context.Repository.Verify(
            repo => repo.ExistsCodeAsync("dev", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 岗位编码重复时拒绝创建，且不得落库。
    /// </summary>
    [Fact]
    public async Task CreatePosition_DuplicateCode_ShouldThrowInvalidOperationException()
    {
        var context = new PositionTestContext();
        _ = context.Repository
            .Setup(repo => repo.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev", "研发岗", EnableStatus.Enabled, 0, null)));

        Assert.Equal("岗位编码已存在。", exception.Message, StringComparer.Ordinal);
        context.Repository.Verify(
            repo => repo.AddAsync(It.IsAny<SysPosition>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 岗位编码不得含空白字符（编码是精确匹配键），中间带空格的编码必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePosition_CodeWithInnerWhitespace_ShouldReject()
    {
        var context = new PositionTestContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev ops", "研发岗", EnableStatus.Enabled, 0, null)));

        Assert.Equal("岗位编码不能包含空白字符。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 岗位编码与名称必填且不超过 100 字符，备注不超过 500 字符，状态枚举须已定义。
    /// </summary>
    [Fact]
    public async Task CreatePosition_TextAndEnumConstraints_ShouldReject()
    {
        var context = new PositionTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("   ", "研发岗", EnableStatus.Enabled, 0, null)));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev", null!, EnableStatus.Enabled, 0, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand(new string('c', 101), "研发岗", EnableStatus.Enabled, 0, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev", new string('n', 101), EnableStatus.Enabled, 0, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev", "研发岗", EnableStatus.Enabled, 0, new string('r', 501))));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePositionAsync(new PositionCreateCommand("dev", "研发岗", (EnableStatus)9, 0, null)));
    }

    /// <summary>
    /// 更新岗位不改动编码与状态：编码是历史引用键，状态另有独立命令，命令里也没有这两个字段。
    /// </summary>
    [Fact]
    public async Task UpdatePosition_ShouldNotChangeCodeOrStatus()
    {
        var context = new PositionTestContext();
        var position = context.SetupExistingPosition();
        position.PositionCode = "dev";
        position.Status = EnableStatus.Disabled;

        _ = await context.Service.UpdatePositionAsync(new PositionUpdateCommand(5, "新名称", 3, "备注"));

        Assert.Equal("dev", position.PositionCode, StringComparer.Ordinal);
        Assert.Equal(EnableStatus.Disabled, position.Status);
        Assert.Equal("新名称", position.PositionName, StringComparer.Ordinal);
        Assert.Equal(3, position.Sort);
        Assert.Equal("备注", position.Remark, StringComparer.Ordinal);
        context.Repository.Verify(
            repo => repo.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 更新岗位时纯空白备注会被清空为 null（与状态命令的「保留原备注」语义不同）。
    /// </summary>
    [Fact]
    public async Task UpdatePosition_BlankRemark_ShouldClearRemark()
    {
        var context = new PositionTestContext();
        var position = context.SetupExistingPosition();
        position.Remark = "原备注";

        _ = await context.Service.UpdatePositionAsync(new PositionUpdateCommand(5, "研发岗", 0, "   "));

        Assert.Null(position.Remark);
    }

    /// <summary>
    /// 岗位状态命令的空白备注保留原值，不抹掉历史说明。
    /// </summary>
    [Fact]
    public async Task UpdatePositionStatus_BlankRemark_ShouldKeepExistingRemark()
    {
        var context = new PositionTestContext();
        var position = context.SetupExistingPosition();
        position.Remark = "原备注";

        _ = await context.Service.UpdatePositionStatusAsync(new PositionStatusChangeCommand(5, EnableStatus.Disabled, "   "));

        Assert.Equal(EnableStatus.Disabled, position.Status);
        Assert.Equal("原备注", position.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 岗位主键必须为正数；目标不存在与删除失败分别给出可区分的拒绝。
    /// </summary>
    [Fact]
    public async Task PositionCommands_InvalidIdMissingTargetAndFailedDelete_ShouldReject()
    {
        var context = new PositionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdatePositionAsync(new PositionUpdateCommand(0, "研发岗", 0, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdatePositionStatusAsync(new PositionStatusChangeCommand(0, EnableStatus.Enabled, null)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => context.Service.DeletePositionAsync(0));

        _ = context.Repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPosition?)null);
        var missingException = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Service.DeletePositionAsync(5));
        Assert.Equal("岗位不存在。", missingException.Message, StringComparer.Ordinal);

        var deleteContext = new PositionTestContext();
        var position = deleteContext.SetupExistingPosition();
        _ = deleteContext.Repository
            .Setup(repo => repo.DeleteAsync(position, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var failureException = await Assert.ThrowsAsync<InvalidOperationException>(() => deleteContext.Service.DeletePositionAsync(5));
        Assert.Equal("岗位删除失败。", failureException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 岗位命令对象为空必须抛空引用异常，取消令牌必须在查库之前生效。
    /// </summary>
    [Fact]
    public async Task PositionCommands_NullCommandAndCancelledToken_ShouldThrow()
    {
        var context = new PositionTestContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.CreatePositionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdatePositionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => context.Service.UpdatePositionStatusAsync(null!));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.DeletePositionAsync(5, cancellation.Token));
        context.Repository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 版本领域服务的依赖装配夹具。
    /// </summary>
    private sealed class VersionTestContext
    {
        internal VersionTestContext()
        {
            Repository = new Mock<IVersionRepository>();
            _ = Repository
                .Setup(repo => repo.AddAsync(It.IsAny<SysVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysVersion entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysVersion entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.DeleteAsync(It.IsAny<SysVersion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Service = new VersionDomainService(Repository.Object);
        }

        internal Mock<IVersionRepository> Repository { get; }

        internal VersionDomainService Service { get; }

        internal SysVersion SetupExistingVersion()
        {
            var version = new SysVersion { AppVersion = "1.0.0", DbVersion = "0.9.0" };
            SaasTestHelper.SetBasicId(version, 5);
            _ = Repository
                .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(version);
            return version;
        }
    }

    /// <summary>
    /// 岗位领域服务的依赖装配夹具：默认编码不冲突。
    /// </summary>
    private sealed class PositionTestContext
    {
        internal PositionTestContext()
        {
            Repository = new Mock<IPositionRepository>();
            _ = Repository
                .Setup(repo => repo.ExistsCodeAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _ = Repository
                .Setup(repo => repo.AddAsync(It.IsAny<SysPosition>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysPosition entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysPosition>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysPosition entity, CancellationToken _) => entity);
            _ = Repository
                .Setup(repo => repo.DeleteAsync(It.IsAny<SysPosition>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Service = new PositionDomainService(Repository.Object);
        }

        internal Mock<IPositionRepository> Repository { get; }

        internal PositionDomainService Service { get; }

        internal SysPosition SetupExistingPosition()
        {
            var position = new SysPosition { PositionCode = "dev", PositionName = "研发岗" };
            SaasTestHelper.SetBasicId(position, 5);
            _ = Repository
                .Setup(repo => repo.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(position);
            return position;
        }
    }
}
