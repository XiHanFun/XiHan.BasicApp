using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Domain.ValueObjects;

/// <summary>
/// 租户数据库连接值对象
/// </summary>
public readonly record struct TenantConnectionValueObject
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public TenantDatabaseType? DatabaseType { get; init; }

    /// <summary>
    /// 数据库主机
    /// </summary>
    public string? Host { get; init; }

    /// <summary>
    /// 端口
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// 完整连接串
    /// </summary>
    public string? ConnectionString { get; init; }
}
