// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Api.Tests;

/// <summary>
/// 权限条件的数据行读取：按主键读、只读当前租户看得见的行，软删的读不到
/// </summary>
public sealed class PermissionResourceRowReaderIntegrationTests : IDisposable
{
    private const long OwnTenant = 7;
    private const long OtherTenant = 8;

    private readonly string _connectionString = $"Data Source=xihan-row-reader-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;
    private readonly SqlSugarScope _db;
    private readonly CurrentTenant _tenant = new(AsyncLocalCurrentTenantAccessor.Instance);

    /// <summary>
    /// 构造函数：建临时库，过滤器与框架同口径
    /// </summary>
    public PermissionResourceRowReaderIntegrationTests()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        var tenant = _tenant;
        _db = new SqlSugarScope(
            new ConnectionConfig { DbType = DbType.Sqlite, ConnectionString = _connectionString, IsAutoCloseConnection = true, InitKeyType = InitKeyType.Attribute },
            db =>
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
                db.QueryFilter.AddTableFilter<IMultiTenantEntity>(entity => entity.TenantId == 0 || entity.TenantId == ScopeId());
                db.QueryFilter.AddTableFilter<IStrictMultiTenantEntity>(entity => entity.TenantId == ScopeId());
                db.Aop.DataExecuting = (_, entityInfo) =>
                {
                    if (entityInfo.OperationType == DataFilterType.InsertByObject)
                    {
                        entityInfo.TrySetSnowflakeId(SnowFlakeSingle.Instance.NextId());
                        entityInfo.ToCreated(EntityAuditContext.From(null, tenant.Id));
                    }
                };
            });
        _db.CodeFirst.InitTables(typeof(SysPosition));
    }

    /// <summary>
    /// 本租户的行读得到；别的租户的、软删的读不到
    /// </summary>
    [Fact]
    public async Task ReadAsync_RespectsTenantAndSoftDeleteFilters()
    {
        var own = await InsertAsync(OwnTenant, "own", isDeleted: false);
        var deleted = await InsertAsync(OwnTenant, "deleted", isDeleted: true);
        var other = await InsertAsync(OtherTenant, "other", isDeleted: false);
        var resolver = new Mock<ISqlSugarClientResolver>();
        resolver.Setup(value => value.GetClientForEntity(It.IsAny<Type>())).Returns(_db);
        var registration = PermissionResourceRegistration.Create<SysPosition>("saas:position:update", PermissionResourceKeys.FromArgument("id"));

        IReadOnlyList<object> rows;
        using (_tenant.Change(OwnTenant))
        {
            rows = await new SqlSugarPermissionResourceRowReader(resolver.Object).ReadAsync(registration, [own, deleted, other]);
        }

        var row = Assert.Single(rows);
        Assert.Equal(own, PermissionResourceRegistration.KeyOf(row));
        Assert.Equal("own", registration.ReadValue(row, "resource.positionCode").Values.Single());
        Assert.Equal(EnableStatus.Enabled, registration.ReadValue(row, "resource.status").Values.Single());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _db.Dispose();
        _keepAlive.Dispose();
    }

    private static long ScopeId()
    {
        return AsyncLocalCurrentTenantAccessor.Instance.Current?.TenantId ?? 0;
    }

    private async Task<long> InsertAsync(long tenantId, string code, bool isDeleted)
    {
        using var scope = _tenant.Change(tenantId);
        var position = new SysPosition { PositionCode = code, PositionName = code };
        _ = await _db.Insertable(position).ExecuteCommandAsync();
        if (isDeleted)
        {
            _ = await _db.Updateable<SysPosition>().SetColumns(row => row.IsDeleted == true).Where(row => row.BasicId == position.BasicId).ExecuteCommandAsync();
        }
        return position.BasicId;
    }
}
