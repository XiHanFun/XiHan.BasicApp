// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的表配置
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。由导入表结构建出，一张库表一条（表名唯一，与数据源无关），删除时连带软删它的列配置
/// 生成的页面登记在所属包的 <c>Pages.cs</c>、目录按包；生成目标是新建包或加入已有包
/// </remarks>
[ActiveUniqueIndex(nameof(TableName))]
[SugarIndex("ix_{table}_class_name", nameof(ClassName), OrderByType.Asc)]
[SugarIndex("ix_{table}_module_name", nameof(ModuleName), OrderByType.Asc)]
public sealed class SysCodeGenTable : AppAuditedEntity
{
    /// <summary>
    /// 库里的表名
    /// </summary>
    /// <remarks>
    /// 照读库结构返回的原名（大小写混合的照原样）
    /// </remarks>
    [StringLength(200)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表注释
    /// </summary>
    [StringLength(500)]
    public string? TableComment { get; set; }

    /// <summary>
    /// 实体类名
    /// </summary>
    [StringLength(200)]
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 包的根命名空间
    /// </summary>
    /// <remarks>
    /// 如 <c>XiHan.BasicApp.Demo</c>，即包的工程名
    /// </remarks>
    [StringLength(500)]
    public string? Namespace { get; set; }

    /// <summary>
    /// 包名
    /// </summary>
    /// <remarks>
    /// 如 <c>Demo</c>：权限码、页面码与前端模块目录取它的小写
    /// </remarks>
    [StringLength(100)]
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名
    /// </summary>
    [StringLength(100)]
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名
    /// </summary>
    [StringLength(100)]
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [StringLength(100)]
    public string? Author { get; set; }

    /// <summary>
    /// 生成模式
    /// </summary>
    public CodeGenTemplateType TemplateType { get; set; } = CodeGenTemplateType.Single;

    /// <summary>
    /// 交付方式
    /// </summary>
    public CodeGenDeliveryType DeliveryType { get; set; } = CodeGenDeliveryType.Zip;

    /// <summary>
    /// 生成范围
    /// </summary>
    public CodeGenScope Scope { get; set; } = CodeGenScope.All;

    /// <summary>
    /// 生成目标
    /// </summary>
    public CodeGenTarget Target { get; set; } = CodeGenTarget.NewPackage;

    /// <summary>
    /// 勾选的动作
    /// </summary>
    /// <remarks>
    /// 逗号分隔，取自 <c>create,update,delete,export,import,status,print</c>；为空即缺省的新增、编辑、删除、导出与导入（启停与打印要显式勾选）
    /// </remarks>
    [StringLength(200)]
    public string? EnabledActions { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    [StringLength(200)]
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表的父级列名
    /// </summary>
    [StringLength(200)]
    public string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表的名称列名
    /// </summary>
    [StringLength(200)]
    public string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主从模式里主表的表配置 ID
    /// </summary>
    /// <remarks>
    /// 本表是明细表时指向主表
    /// </remarks>
    public Guid? MasterTableId { get; set; }

    /// <summary>
    /// 主从模式里指向主表的外键列名
    /// </summary>
    [StringLength(200)]
    public string? MasterForeignKey { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    /// <remarks>
    /// 导入时按所在的库记下，类型映射按它选
    /// </remarks>
    public CodeGenDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据源 ID
    /// </summary>
    /// <remarks>
    /// 为空即主库
    /// </remarks>
    public Guid? DataSourceId { get; set; }

    /// <summary>
    /// 最近一次生成的结果
    /// </summary>
    public CodeGenGenerationStatus GenerationStatus { get; set; } = CodeGenGenerationStatus.NotGenerated;

    /// <summary>
    /// 最近一次成功生成的时间（UTC）
    /// </summary>
    public DateTimeOffset? LastGeneratedAt { get; set; }

    /// <summary>
    /// 扩展选项（JSON）
    /// </summary>
    [LongText]
    public string? Options { get; set; }

    /// <summary>
    /// 人工改过的字段
    /// </summary>
    /// <remarks>
    /// 属性名的 JSON 数组：同步结构时不覆盖这些字段
    /// </remarks>
    [StringLength(1000)]
    public string? UserModifiedFields { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 生成范围
/// </summary>
public enum CodeGenScope
{
    /// <summary>
    /// 前后端都生成
    /// </summary>
    All = 0,

    /// <summary>
    /// 只生成后端
    /// </summary>
    BackendOnly = 1,

    /// <summary>
    /// 只生成前端
    /// </summary>
    FrontendOnly = 2,
}

/// <summary>
/// 生成目标
/// </summary>
/// <remarks>
/// 新建包另出包级文件（工程、特性、权限、页面、文案……）；加入已有包只出资源相关文件与一份要人工合入的登记片段
/// </remarks>
public enum CodeGenTarget
{
    /// <summary>
    /// 新建包
    /// </summary>
    NewPackage = 0,

    /// <summary>
    /// 加入已有包
    /// </summary>
    ExistingPackage = 1,
}
