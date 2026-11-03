// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 数据源、表配置、列配置与模板的启停状态
/// </summary>
/// <remarks>
/// 4.x 的模板有 <c>IsEnabled</c> 与 <c>Status</c> 两个开关，6.0 合成这一个（方案 C2）
/// </remarks>
public enum CodeGenStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}

/// <summary>
/// 数据源的数据库类型
/// </summary>
/// <remarks>
/// 照 4.x 的五种；建外部库连接时换成 SqlSugar 的数据库类型名（框架运行时数据源按它选驱动）
/// </remarks>
public enum CodeGenDatabaseType
{
    /// <summary>
    /// MySQL
    /// </summary>
    MySql = 0,

    /// <summary>
    /// SQL Server
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 3,

    /// <summary>
    /// SQLite
    /// </summary>
    Sqlite = 4,
}

/// <summary>
/// 生成模式
/// </summary>
/// <remarks>
/// 表配置取单表、树表、主从之一；通用只给模板用，表示适用于全部模式（照 4.x）
/// </remarks>
public enum CodeGenTemplateType
{
    /// <summary>
    /// 单表
    /// </summary>
#pragma warning disable CA1720 // 照 4.x 的线上取值 Single（前端按名称取值）
    Single = 0,
#pragma warning restore CA1720

    /// <summary>
    /// 树表
    /// </summary>
    Tree = 1,

    /// <summary>
    /// 主从
    /// </summary>
    MasterDetail = 2,

    /// <summary>
    /// 通用（只用于模板）
    /// </summary>
    Universal = 3,
}

/// <summary>
/// 生成结果
/// </summary>
public enum CodeGenGenerationStatus
{
    /// <summary>
    /// 未生成
    /// </summary>
    NotGenerated = 0,

    /// <summary>
    /// 已生成
    /// </summary>
    Generated = 1,

    /// <summary>
    /// 生成失败
    /// </summary>
    Failed = 2,
}

/// <summary>
/// 生成产物的交付方式
/// </summary>
public enum CodeGenDeliveryType
{
    /// <summary>
    /// 产物包（存成临时文件下载）
    /// </summary>
    Zip = 0,

    /// <summary>
    /// 写进项目
    /// </summary>
    /// <remarks>
    /// 只在 Development 且配置打开时可用（方案 C1）
    /// </remarks>
    Project = 1,
}

/// <summary>
/// 模板引擎
/// </summary>
/// <remarks>
/// 4.x 另有 T4，选了即抛不支持，6.0 去掉（方案 C2），字段保留给以后扩展
/// </remarks>
public enum CodeGenTemplateEngine
{
    /// <summary>
    /// Scriban
    /// </summary>
    Scriban = 1,
}

/// <summary>
/// 列的查询方式
/// </summary>
public enum CodeGenQueryType
{
    /// <summary>
    /// 等于
    /// </summary>
    Equal = 0,

    /// <summary>
    /// 不等于
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// 大于
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// 大于等于
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// 小于
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// 小于等于
    /// </summary>
    LessThanOrEqual = 5,

    /// <summary>
    /// 模糊匹配
    /// </summary>
    Like = 6,

    /// <summary>
    /// 左模糊
    /// </summary>
    LikeLeft = 7,

    /// <summary>
    /// 右模糊
    /// </summary>
    LikeRight = 8,

    /// <summary>
    /// 区间
    /// </summary>
    Between = 9,

    /// <summary>
    /// 在其中
    /// </summary>
    In = 10,

    /// <summary>
    /// 不在其中
    /// </summary>
    NotIn = 11,
}

/// <summary>
/// 列的表单控件
/// </summary>
public enum CodeGenHtmlType
{
    /// <summary>
    /// 文本框
    /// </summary>
    Input = 0,

    /// <summary>
    /// 文本域
    /// </summary>
    Textarea = 1,

    /// <summary>
    /// 下拉框
    /// </summary>
    Select = 2,

    /// <summary>
    /// 单选框
    /// </summary>
    Radio = 3,

    /// <summary>
    /// 复选框
    /// </summary>
    Checkbox = 4,

    /// <summary>
    /// 日期
    /// </summary>
    DatePicker = 5,

    /// <summary>
    /// 日期时间
    /// </summary>
    DateTimePicker = 6,

    /// <summary>
    /// 时间
    /// </summary>
    TimePicker = 7,

    /// <summary>
    /// 图片上传
    /// </summary>
    ImageUpload = 8,

    /// <summary>
    /// 文件上传
    /// </summary>
    FileUpload = 9,

    /// <summary>
    /// 富文本
    /// </summary>
    Editor = 10,

    /// <summary>
    /// 数字输入框
    /// </summary>
    InputNumber = 11,

    /// <summary>
    /// 开关
    /// </summary>
    Switch = 12,

    /// <summary>
    /// 树形选择
    /// </summary>
    TreeSelect = 13,
}

/// <summary>
/// 列的选项来源
/// </summary>
public enum CodeGenDictSelectorType
{
    /// <summary>
    /// 系统字典（字典码）
    /// </summary>
    DictSelector = 0,

    /// <summary>
    /// 枚举类型（枚举全名）
    /// </summary>
    EnumSelector = 1,

    /// <summary>
    /// 常量数组（内联 JSON）
    /// </summary>
    ConstSelector = 2,

    /// <summary>
    /// 关联表下拉（外键指向另一张表配置）
    /// </summary>
    TableSelector = 3,

    /// <summary>
    /// 关联树选择（外键指向另一张树表配置）
    /// </summary>
    TreeSelector = 4,
}

/// <summary>
/// 产物的写入方式
/// </summary>
/// <remarks>
/// 自动文件由表结构推导、重新生成时覆盖；手写文件只在第一次生成时创建，此后不再碰（照 4.x「重新生成不覆盖手改」）
/// </remarks>
public enum CodeGenWriteMode
{
    /// <summary>
    /// 每次覆盖
    /// </summary>
    AlwaysOverwrite = 0,

    /// <summary>
    /// 只在第一次写
    /// </summary>
    WriteOnce = 1,
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
