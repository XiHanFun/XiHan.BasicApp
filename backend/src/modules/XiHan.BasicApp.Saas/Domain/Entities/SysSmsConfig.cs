// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统短信网关配置实体
/// 短信服务商配置表：承载各服务商访问凭证、签名及模板映射，驱动短信发送器运行时构建网关客户端
/// </summary>
/// <remarks>
/// 关联：
/// - SysSms.Provider 记录发送时实际使用的服务商（发送器写入）
/// - SysSms.TemplateCode（内部模板码）经本表 TemplateMap 映射为服务商模板码后发送
///
/// 写入：
/// - ConfigCode 在租户内必须唯一（UX_TeId_CoCd）
/// - 同一租户下有且仅有一条 IsDefault=true 的记录；服务层必须保证互斥
/// - AccessKeySecret 为敏感字段，Data Protection 加密落库，读侧永不回显
///
/// 查询：
/// - 获取默认网关：WHERE IsDefault=true AND IsEnabled=true
/// - 发送器按指纹缓存构建客户端，改配置即热生效（镜像存储配置范式）
///
/// 场景：
/// - 多服务商切换（阿里云/腾讯云）
/// - 租户自配签名与模板（TenantId=0 为平台全局配置）
/// </remarks>
[SugarTable(TableName = "Sys_Sms_Config", TableDescription = "系统短信网关配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoCd", nameof(TenantId), OrderByType.Asc, nameof(ConfigCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_Prov", nameof(TenantId), OrderByType.Asc, nameof(Provider), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe_IsEn", nameof(TenantId), OrderByType.Asc, nameof(IsDefault), OrderByType.Desc, nameof(IsEnabled), OrderByType.Asc)]
public partial class SysSmsConfig : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 配置编码（租户内唯一标识）
    /// </summary>
    [SugarColumn(ColumnName = "Config_Code", ColumnDescription = "配置编码", Length = 100, IsNullable = false)]
    public virtual string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    [SugarColumn(ColumnName = "Config_Name", ColumnDescription = "配置名称", Length = 200, IsNullable = false)]
    public virtual string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 短信服务商
    /// </summary>
    [SugarColumn(ColumnName = "Provider", ColumnDescription = "短信服务商")]
    public virtual SmsProviderType Provider { get; set; } = SmsProviderType.Aliyun;

    /// <summary>
    /// 访问密钥ID（阿里云 AccessKeyId / 腾讯云 SecretId）
    /// </summary>
    [SugarColumn(ColumnName = "Access_Key_Id", ColumnDescription = "访问密钥ID", Length = 200, IsNullable = false)]
    public virtual string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥（阿里云 AccessKeySecret / 腾讯云 SecretKey；敏感字段，加密落库，读侧不回显）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Access_Key_Secret", ColumnDescription = "访问密钥", Length = 500, IsNullable = false)]
    public virtual string AccessKeySecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用ID（腾讯云 SmsSdkAppId，腾讯云必填；阿里云不使用）
    /// </summary>
    [SugarColumn(ColumnName = "Sdk_App_Id", ColumnDescription = "应用ID", Length = 100, IsNullable = true)]
    public virtual string? SdkAppId { get; set; }

    /// <summary>
    /// 短信签名（服务商控制台审核通过的签名名称）
    /// </summary>
    [SugarColumn(ColumnName = "Sign_Name", ColumnDescription = "短信签名", Length = 100, IsNullable = false)]
    public virtual string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 地域（腾讯云必填，如 ap-guangzhou；阿里云不使用，端点固定）
    /// </summary>
    [SugarColumn(ColumnName = "Region", ColumnDescription = "地域", Length = 100, IsNullable = true)]
    public virtual string? Region { get; set; }

    /// <summary>
    /// 模板映射（JSON：内部模板码 → 服务商模板码 + 参数序）
    /// </summary>
    /// <remarks>
    /// 形如 {"auth-sms-login-code":{"templateCode":"SMS_123456","paramOrder":["code","minutes"]}}；
    /// 阿里云按命名 JSON 参数发送（参数名须与服务商模板变量名一致），paramOrder 供腾讯云位置参数数组使用。
    /// </remarks>
    [SugarColumn(ColumnName = "Template_Map", ColumnDescription = "模板映射", Length = 4000, IsNullable = true)]
    public virtual string? TemplateMap { get; set; }

    /// <summary>
    /// 是否默认网关（同一租户有且仅有一条为true）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认网关")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
