// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 短信配置（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 分侧、缺省、平台代发与发送时才取的规则与 <see cref="SysEmailConfig"/> 相同
/// 模板映射（本应用的模板编码 → 服务商的模板 ID）放在配置上而不是模板上：服务商模板 ID 属于这个服务商账号，换配置即换一套映射，模板与业务调用方只认本应用的编码；映射只整体读写，存成 JSON 列
/// 访问密钥加密存储、不回显，修改时不填即保留；解不开就发不出，不把库里的值当明文用
/// </remarks>
[SugarTable(TableName = "sys_sms_config", TableDescription = "短信配置（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_sms_config_owner_tenant_id_code")]
public sealed class SysSmsConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一）
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 64, IsNullable = false, ColumnDescription = "编码（同一侧唯一）")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 128, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 服务商
    /// </summary>
    [SugarColumn(ColumnName = "provider", IsNullable = false, ColumnDescription = "服务商")]
    public SmsProvider Provider { get; set; } = SmsProvider.Aliyun;

    /// <summary>
    /// 访问密钥 ID（阿里云 AccessKeyId、腾讯云 SecretId）
    /// </summary>
    [SugarColumn(ColumnName = "access_key_id", Length = 256, IsNullable = false, ColumnDescription = "访问密钥 ID（阿里云 AccessKeyId、腾讯云 SecretId）")]
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥（密文）
    /// </summary>
    [SugarColumn(ColumnName = "access_key_secret", Length = 1024, IsNullable = true, ColumnDescription = "访问密钥（密文）")]
    public string? AccessKeySecret { get; set; }

    /// <summary>
    /// 应用 ID（腾讯云 SmsSdkAppId）
    /// </summary>
    [SugarColumn(ColumnName = "sdk_app_id", Length = 64, IsNullable = true, ColumnDescription = "应用 ID（腾讯云 SmsSdkAppId）")]
    public string? SdkAppId { get; set; }

    /// <summary>
    /// 签名
    /// </summary>
    [SugarColumn(ColumnName = "sign_name", Length = 64, IsNullable = false, ColumnDescription = "签名")]
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 地域（腾讯云如 <c>ap-guangzhou</c>）
    /// </summary>
    [SugarColumn(ColumnName = "region", Length = 64, IsNullable = true, ColumnDescription = "地域（腾讯云如 ap-guangzhou）")]
    public string? Region { get; set; }

    /// <summary>
    /// 模板映射（JSON 对象：本应用的模板编码 → 服务商的模板 ID）
    /// </summary>
    [SugarColumn(ColumnName = "template_map", IsNullable = true, ColumnDescription = "模板映射（JSON 对象：本应用的模板编码 → 服务商的模板 ID）")]
    [LongText]
    public string? TemplateMap { get; set; }

    /// <summary>
    /// 是否本侧的缺省配置
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否本侧的缺省配置")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
