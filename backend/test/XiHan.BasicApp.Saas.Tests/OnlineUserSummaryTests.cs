// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 在线用户概览测试
/// </summary>
public class OnlineUserSummaryTests
{
    /// <summary>
    /// 概览直接使用数据库计数，不为统计物化全部会话实体
    /// </summary>
    [Fact]
    public async Task GetOnlineUserSummaryAsync_UsesAggregateCounts()
    {
        var sessions = new Mock<IUserSessionRepository>();
        sessions
            .Setup(repository => repository.CountAsync(
                It.IsAny<Expression<Func<SysUserSession, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(7L);
        sessions
            .Setup(repository => repository.CountActiveUsersAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(6L);

        var connections = new Mock<IConnectionManager>();
        connections.Setup(manager => manager.GetOnlineUserCountAsync()).ReturnsAsync(1);

        var service = new OnlineUserQueryService(
            sessions.Object,
            new Mock<IUserRepository>().Object,
            connections.Object,
            new Mock<IFieldSecurityService>().Object);

        var summary = await service.GetOnlineUserSummaryAsync(CancellationToken.None);

        Assert.Equal(1, summary.RealtimeOnlineUsers);
        Assert.Equal(7, summary.ActiveSessions);
        Assert.Equal(6, summary.ActiveUsers);
        sessions.Verify(repository => repository.GetListAsync(
            It.IsAny<Expression<Func<SysUserSession, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
