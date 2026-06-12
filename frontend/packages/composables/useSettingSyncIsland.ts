import { islandStart } from './useDynamicIsland'

/**
 * 用户设置同步灵动岛统一封装。
 * 偏好 / 收藏夹 / 搜索设置 / 表格设置 / 视图等所有用户设置的
 * 上行保存、登录水合、远端推送应用，共用同一套提示文案与节奏。
 */

/** 同步任务句柄：按结果收尾（成功 / 失败 / 静默） */
export interface SettingSyncIslandTask {
  /** 成功收尾（「{name}已同步」） */
  success: () => void
  /** 失败收尾（「{name}同步失败」） */
  error: () => void
  /** 静默收尾（不弹结果，用于高频场景避免刷屏） */
  dismiss: () => void
}

/**
 * 上行保存 / 拉取水合：「正在同步{name}…」起始，按结果收尾。
 *
 * @param id 灵动岛任务标识（同 id 复用同一条）
 * @param name 设置名称（如「偏好设置」「收藏夹」「表格设置」）
 */
export function settingSyncIsland(id: string, name: string): SettingSyncIslandTask {
  const task = islandStart(id, `正在同步${name}…`)
  return {
    success: () => task.success(`${name}已同步`),
    error: () => task.error(`${name}同步失败`),
    dismiss: () => task.dismiss(),
  }
}

/**
 * 远端推送已应用（下行）：瞬时提示「{name}已从其他设备同步」。
 * 在变更实际落地本端后调用。
 *
 * @param id 灵动岛任务标识
 * @param name 设置名称
 */
export function settingSyncRemoteApplied(id: string, name: string): void {
  islandStart(id, `正在应用其他设备的${name}变更…`).success(`${name}已从其他设备同步`)
}
