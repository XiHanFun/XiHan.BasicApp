#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileType
// Guid:8127a3ce-b9c1-4682-b396-a801c7c9f267
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 文件类型枚举
/// </summary>
public enum FileType
{
    /// <summary>
    /// 图片
    /// </summary>
    Image = 0,

    /// <summary>
    /// 文档
    /// </summary>
    Document = 1,

    /// <summary>
    /// 视频
    /// </summary>
    Video = 2,

    /// <summary>
    /// 音频
    /// </summary>
    Audio = 3,

    /// <summary>
    /// 压缩包
    /// </summary>
    Archive = 4,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}
