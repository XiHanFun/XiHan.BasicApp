#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAiProvider
// Guid:a11c0de0-1001-4a10-9a00-00000000ai10
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 系统 AI 服务提供商配置实体
/// 存储对接外部大模型（OpenAI 兼容协议）的 provider 配置：端点、模型、密钥、采样参数等
/// </summary>
/// <remarks>
/// 定位：
/// - 映射框架 <c>AiProviderOptions</c>（provider/apiKey/baseUrl/model/maxTokens/temperature/timeout/extraJson）
/// - 一个适配器打天下：OpenAI/DeepSeek/Azure/vLLM/自训模型皆走 OpenAI 兼容协议，BaseUrl 指向对应端点即可
///
/// 写入：
/// - ConfigCode 租户内唯一（UX_TeId_CoCd，末列附 IsDeleted，软删后可复用编码），上层解析器/配置源以 ConfigCode 为键
/// - ApiKey 可逆加密落库（DataProtection，独立 Purpose），[JsonIgnore] 双注解防止密文外泄前端
/// - 单默认约束：SetDefault 时领域服务清除其它行 IsDefault（软约束，配置源按 IsDefault+启用取首条）
///
/// 查询：
/// - 配置源按「默认且启用」取运行 provider；管理后台列表：IX_TeId_St
///
/// 删除：
/// - 软删；删除后解析器缓存失效（应用服务调 IAiChatClientResolver.Invalidate）
///
/// 状态：
/// - Status: Enabled/Disabled；IsEnabled 另作运行开关（禁用后不参与配置源解析）
/// </remarks>
[SugarTable(TableName = "Sys_Ai_Provider", TableDescription = "系统 AI 服务提供商配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoCd", nameof(TenantId), OrderByType.Asc, nameof(ConfigCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysAiProvider : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 配置编码（租户内唯一，上层以此为 provider 键）
    /// </summary>
    [SugarColumn(ColumnName = "Config_Code", ColumnDescription = "配置编码", Length = 100, IsNullable = false)]
    public virtual string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    [SugarColumn(ColumnName = "Config_Name", ColumnDescription = "配置名称", Length = 200, IsNullable = false)]
    public virtual string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 提供商/协议标识（OpenAI/DeepSeek/Azure/vLLM/Custom 等，自由字符串，仅作分组与展示）
    /// </summary>
    [SugarColumn(ColumnName = "Provider", ColumnDescription = "提供商标识", Length = 50, IsNullable = false)]
    public virtual string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    [SugarColumn(ColumnName = "Model", ColumnDescription = "模型名称", Length = 100, IsNullable = false)]
    public virtual string Model { get; set; } = string.Empty;

    /// <summary>
    /// 端点地址（空则用提供商默认端点，如 OpenAI 官方）
    /// </summary>
    [SugarColumn(ColumnName = "Base_Url", ColumnDescription = "端点地址", Length = 500, IsNullable = true)]
    public virtual string? BaseUrl { get; set; }

    /// <summary>
    /// API 密钥（可逆加密落库，禁止序列化到前端）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Api_Key", ColumnDescription = "API 密钥（加密）", Length = 500, IsNullable = true)]
    public virtual string? ApiKey { get; set; }

    /// <summary>
    /// 最大输出 token 数
    /// </summary>
    [SugarColumn(ColumnName = "Max_Output_Tokens", ColumnDescription = "最大输出 token 数", IsNullable = true)]
    public virtual int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 采样温度
    /// </summary>
    [SugarColumn(ColumnName = "Temperature", ColumnDescription = "采样温度", IsNullable = true)]
    public virtual float? Temperature { get; set; }

    /// <summary>
    /// 请求超时（秒）
    /// </summary>
    [SugarColumn(ColumnName = "Timeout_Seconds", ColumnDescription = "请求超时（秒）", IsNullable = true)]
    public virtual int? TimeoutSeconds { get; set; }

    /// <summary>
    /// 自定义参数（JSON，免改表结构扩展）
    /// </summary>
    [SugarColumn(ColumnName = "Extra_Json", ColumnDescription = "自定义参数(JSON)", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtraJson { get; set; }

    /// <summary>
    /// 是否默认 provider（租户内单默认，软约束）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用（禁用后不参与配置源解析）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
