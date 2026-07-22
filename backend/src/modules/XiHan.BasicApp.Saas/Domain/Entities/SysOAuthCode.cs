// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统 OAuth 授权码实体
/// Authorization Code Flow 中的一次性凭据：用户同意后颁发给客户端，换取 Token 使用后立即失效
/// </summary>
/// <remarks>
/// 关联：
/// - ClientId → SysOAuthApp.ClientId；UserId → SysUser
///
/// 写入：
/// - Code 全局唯一（UX_Co），使用高强度随机串
/// - ExpirationTime 必须短（建议 60s~10min），过期立即作废
/// - 必须绑定 RedirectUri（颁发时记录，换 Token 时严格匹配）
/// - 支持 PKCE：若请求方传入 CodeChallenge 则必须记录，换 Token 时校验 CodeVerifier
///
/// 查询：
/// - 换 Token 入口：按 Code 精确定位 + 有效期校验
/// - 按客户端/用户审计：IX_ClId / IX_UsId
/// - 清理过期：IX_ExTi，筛选早于当前时间的 ExpirationTime
///
/// 删除：
/// - 使用后立即硬删（或标记已使用）防止重放
/// - 过期码由定时任务批量清理
///
/// 场景：
/// - OAuth2 标准授权码流程
/// - PKCE 增强的 SPA/Mobile 授权
/// </remarks>
[SugarTable(TableName = "Sys_OAuth_Code", TableDescription = "系统OAuth授权码表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_Co", nameof(Code), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ClId", nameof(ClientId), OrderByType.Asc)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Desc)]
public partial class SysOAuthCode : BasicAppCreationEntity
{
    /// <summary>
    /// 授权码
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Code", ColumnDescription = "授权码", Length = 100, IsNullable = false)]
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID
    /// </summary>
    [SugarColumn(ColumnName = "Client_Id", ColumnDescription = "客户端ID", Length = 100, IsNullable = false)]
    public virtual string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 重定向URI
    /// </summary>
    [SugarColumn(ColumnName = "Redirect_Uri", ColumnDescription = "重定向URI", Length = 500, IsNullable = false)]
    public virtual string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 权限范围
    /// </summary>
    [SugarColumn(ColumnName = "Scopes", ColumnDescription = "权限范围", Length = 500, IsNullable = true)]
    public virtual string? Scopes { get; set; }

    /// <summary>
    /// CSRF 状态参数（OAuth2 防 CSRF 的随机字符串，非业务状态）
    /// </summary>
    [SugarColumn(ColumnName = "Csrf_State", ColumnDescription = "跨站请求伪造防护状态参数", Length = 200, IsNullable = true)]
    public virtual string? CsrfState { get; set; }

    /// <summary>
    /// 质询码
    /// </summary>
    [SugarColumn(ColumnName = "Code_Challenge", ColumnDescription = "质询码", Length = 100, IsNullable = true)]
    public virtual string? CodeChallenge { get; set; }

    /// <summary>
    /// 质询方法
    /// </summary>
    [SugarColumn(ColumnName = "Code_Challenge_Method", ColumnDescription = "质询方法", Length = 20, IsNullable = true)]
    public virtual string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "过期时间")]
    public virtual DateTimeOffset ExpirationTime { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    [SugarColumn(ColumnName = "Is_Used", ColumnDescription = "是否已使用")]
    public virtual bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    [SugarColumn(ColumnName = "Used_Time", ColumnDescription = "使用时间", IsNullable = true)]
    public virtual DateTimeOffset? UsedTime { get; set; }
}
