#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDetailDto
// Guid:ac255eff-bf46-4e28-9369-1076c0728ed8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件详情 DTO
/// </summary>
public sealed class FileDetailDto : FileListItemDto
{
    /// <summary>
    /// 文件哈希
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// 哈希算法
    /// </summary>
    public string? HashAlgorithm { get; set; }

    /// <summary>
    /// 上传 IP
    /// </summary>
    public string? UploadIp { get; set; }

    /// <summary>
    /// 上传来源
    /// </summary>
    public string? UploadSource { get; set; }

    /// <summary>
    /// 访问权限
    /// </summary>
    public string? AccessPermissions { get; set; }

    /// <summary>
    /// 加密密钥 ID
    /// </summary>
    public string? EncryptionKeyId { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 创建者主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改者主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    public string? ModifiedBy { get; set; }
}
