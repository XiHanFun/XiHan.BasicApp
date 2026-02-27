using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : BasicAppDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }
}
