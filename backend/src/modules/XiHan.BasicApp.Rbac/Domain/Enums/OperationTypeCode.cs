#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationTypeCode
// Guid:4246c9f9-994d-42af-acf9-727fe72749f5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Enums;

/// <summary>
/// 操作类型枚举（RBAC 权限操作）
/// </summary>
public enum OperationTypeCode
{
    /// <summary>
    /// 创建/新增
    /// </summary>
    Create = 0,

    /// <summary>
    /// 读取/查询
    /// </summary>
    Read = 1,

    /// <summary>
    /// 更新/修改
    /// </summary>
    Update = 2,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 查看详情
    /// </summary>
    View = 4,

    /// <summary>
    /// 审批
    /// </summary>
    Approve = 10,

    /// <summary>
    /// 执行/操作
    /// </summary>
    Execute = 11,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 20,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 21,

    /// <summary>
    /// 上传
    /// </summary>
    Upload = 22,

    /// <summary>
    /// 下载
    /// </summary>
    Download = 23,

    /// <summary>
    /// 打印
    /// </summary>
    Print = 24,

    /// <summary>
    /// 分享
    /// </summary>
    Share = 25,

    /// <summary>
    /// 授权/授予
    /// </summary>
    Grant = 30,

    /// <summary>
    /// 撤销/收回
    /// </summary>
    Revoke = 31,

    /// <summary>
    /// 启用
    /// </summary>
    Enable = 32,

    /// <summary>
    /// 禁用
    /// </summary>
    Disable = 33,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}
