namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 根据编码查询租户
/// </summary>
public class TenantByCodeQuery
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;
}
