#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserStatisticsDetailDto
// Guid:4600f187-8645-4980-9ebd-aecde2f960db
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户统计详情 DTO
/// </summary>
public sealed class UserStatisticsDetailDto : UserStatisticsListItemDto
{
    /// <summary>
    /// 文件上传次数
    /// </summary>
    public int FileUploadCount { get; set; }

    /// <summary>
    /// 文件下载次数
    /// </summary>
    public int FileDownloadCount { get; set; }

    /// <summary>
    /// 发送邮件次数
    /// </summary>
    public int EmailSentCount { get; set; }

    /// <summary>
    /// 发送短信次数
    /// </summary>
    public int SmsSentCount { get; set; }

    /// <summary>
    /// 发送通知次数
    /// </summary>
    public int NotificationSentCount { get; set; }

    /// <summary>
    /// 接收通知次数
    /// </summary>
    public int NotificationReceivedCount { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

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
