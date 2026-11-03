// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块的错误码
/// </summary>
/// <remarks>
/// 格式为 <c>PRINTING_&lt;KIND&gt;_&lt;NNN&gt;</c>。并发戳过期由框架仓储报（<c>PERSISTENCE_CONFLICT_*</c>）
/// </remarks>
public static class PrintingErrors
{
    /// <summary>
    /// 模板不存在或不属于当前一侧
    /// </summary>
    public const string TemplateNotFound = "PRINTING_NOTFOUND_001";

    /// <summary>
    /// 按编码与作用域解析不到可用的模板
    /// </summary>
    public const string NoUsableTemplate = "PRINTING_NOTFOUND_002";

    /// <summary>
    /// 本侧已有同编码的模板
    /// </summary>
    public const string TemplateCodeTaken = "PRINTING_CONFLICT_001";

    /// <summary>
    /// 模板仍在启用，须先停用才能删除
    /// </summary>
    public const string TemplateEnabled = "PRINTING_CONFLICT_002";

    /// <summary>
    /// 列表不支持该筛选或排序
    /// </summary>
    public const string QueryUnsupported = "PRINTING_VALIDATION_001";

    /// <summary>
    /// 模板 JSON 不合格式
    /// </summary>
    public const string TemplateJsonInvalid = "PRINTING_VALIDATION_002";

    /// <summary>
    /// 数据源编码没有登记
    /// </summary>
    public const string DataSourceUnregistered = "PRINTING_VALIDATION_003";

    /// <summary>
    /// 平台一侧不能用租户作用域
    /// </summary>
    public const string ScopeInvalid = "PRINTING_VALIDATION_004";

    /// <summary>
    /// 维护平台全局模板缺少全局管理权限
    /// </summary>
    public const string GlobalManageRequired = "PRINTING_DENIED_001";

    /// <summary>
    /// 全局模板没有向租户开放或已停用
    /// </summary>
    public const string GlobalNotOpen = "PRINTING_DENIED_002";
}
