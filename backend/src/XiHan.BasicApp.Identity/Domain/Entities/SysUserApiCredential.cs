// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 开放接口凭证
/// </summary>
/// <remarks>
/// 开放接口凭证跟着账号走，落平台库、不分租户：签名调用在 Authenticate 阶段按 AppKey 认人，那时还没有租户上下文；每人最多 5 个
/// 密钥经 <c>ISecretProtector</c> 加密存而不是存散列：验签要用明文重算签名；明文只在创建与滚动时返回一次
/// AppKey 全局唯一，部分唯一索引 <c>ux_sys_user_api_credential_app_key</c> 只约束未删除的行
/// 签名算法与 IP 白名单放在账号安全上（<see cref="SysUserSecurity"/>），按人设一次、作用于本人全部凭证
/// OpenPlatform 经 Contracts 的凭证目录取凭证，不缓存：停用、删除、滚动密钥与改白名单立即生效；归属人停用或锁定中照样取出，由调用方按账号状态拒绝
/// <see cref="LastUsedAt"/> 在验签通过后回写，同一实例一分钟内只写一次、在独立作用域里提交：不让每个签名请求都写库
/// 存的是密钥密文，标 <c>[DiffLogIgnored]</c> 不进数据变更日志；删除用户不连带删除凭证，归属人不在了凭证目录就取不出它
/// </remarks>
[SugarTable(TableName = "sys_user_api_credential", TableDescription = "开放接口凭证")]
[ActiveUniqueIndex(nameof(AppKey), Name = "ux_sys_user_api_credential_app_key")]
[SugarIndex("ix_sys_user_api_credential_user_id", nameof(UserId), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserApiCredential : AppAuditedEntity
{
    /// <summary>
    /// 归属人
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "归属人")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// AppKey（<c>ak_</c> + 24 位字母数字，全局唯一）
    /// </summary>
    [SugarColumn(ColumnName = "app_key", Length = 64, IsNullable = false, ColumnDescription = "AppKey（ak_ + 24 位字母数字，全局唯一）")]
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 密钥密文（明文为 <c>sk_</c> + 43 位字母数字）
    /// </summary>
    [SugarColumn(ColumnName = "secret_cipher", Length = 1024, IsNullable = false, ColumnDescription = "密钥密文（明文为 sk_ + 43 位字母数字）")]
    public string SecretCipher { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 最近一次验签通过的时间
    /// </summary>
    [SugarColumn(ColumnName = "last_used_at", IsNullable = true, ColumnDescription = "最近一次验签通过的时间")]
    public DateTimeOffset? LastUsedAt { get; set; }
}
