#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationTypeCode
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

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
    /// 执行/操作
    /// </summary>
    Execute = 4,

    /// <summary>
    /// 审批
    /// </summary>
    Approve = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 7,

    /// <summary>
    /// 下载
    /// </summary>
    Download = 8,

    /// <summary>
    /// 上传
    /// </summary>
    Upload = 9,

    /// <summary>
    /// 打印
    /// </summary>
    Print = 10,

    /// <summary>
    /// 分享
    /// </summary>
    Share = 11,

    /// <summary>
    /// 授权/授予
    /// </summary>
    Grant = 12,

    /// <summary>
    /// 撤销/收回
    /// </summary>
    Revoke = 13,

    /// <summary>
    /// 查看详情
    /// </summary>
    View = 14,

    /// <summary>
    /// 启用
    /// </summary>
    Enable = 15,

    /// <summary>
    /// 禁用
    /// </summary>
    Disable = 16,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}
