/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { IslandAccessory, IslandState, IslandTask } from '~/composables/useDynamicIsland'

/** 终态的状态图标 */
export function stateIcon(state?: IslandState): string {
  switch (state) {
    case 'success':
      return 'lucide:check'
    case 'error':
      return 'lucide:triangle-alert'
    case 'info':
      return 'lucide:info'
    default:
      return 'lucide:loader-2'
  }
}

/** 缺省左侧：进行中是进度环（环心放 icon），终态是状态图标 */
export function defaultLeading(task: IslandTask): IslandAccessory {
  return task.state === 'loading'
    ? { type: 'ring', value: task.progress, icon: task.icon }
    : { type: 'icon', icon: task.icon || stateIcon(task.state) }
}

/** 缺省右侧：进行中且有进度时显示百分比 */
export function defaultTrailing(task: IslandTask): IslandAccessory | undefined {
  return task.state === 'loading' && task.progress != null
    ? { type: 'text', text: `${Math.round(task.progress)}%`, tone: 'brand' }
    : undefined
}

/** 计时读数：m:ss，满一小时 h:mm:ss */
export function formatElapsed(ms: number): string {
  const total = Math.max(0, Math.floor(ms / 1000))
  const hours = Math.floor(total / 3600)
  const minutes = Math.floor((total % 3600) / 60)
  const seconds = String(total % 60).padStart(2, '0')
  return hours > 0
    ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}`
    : `${minutes}:${seconds}`
}

/** 两侧或内容块里是否用到计时：用到才让岛每秒走一次时钟 */
export function usesElapsed(task: IslandTask): boolean {
  const isElapsed = (item?: IslandAccessory) => item?.type === 'elapsed'
  return isElapsed(task.leading)
    || isElapsed(task.trailing)
    || (task.blocks ?? []).some(block => (block.type === 'metric' && isElapsed(block.accessory))
      || (block.type === 'list' && block.items.some(item => isElapsed(item.leading))))
}
