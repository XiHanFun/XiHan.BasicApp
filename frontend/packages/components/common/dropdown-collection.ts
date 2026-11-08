/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { MenuNode } from '@xihan-ui/headless'
import type { AppDropdownOption } from '~/types'

/**
 * 把应用侧的下拉条目摊成组件库的 collection
 * 应用侧（全站几十处）写独立的 divider 条，折进下一条的 separatorBefore，末尾的丢掉；首条上的分隔线组件库本就不产出
 * MenuNode 没有 children，代铺路径只吃扁平条目，带子层的丢掉并先告警；要子菜单改摆部件（见 TabbarContextMenu.vue）
 */
export function toDropdownCollection(options: ReadonlyArray<AppDropdownOption>): MenuNode[] {
  const out: MenuNode[] = []
  let pendingSeparator = false

  for (const option of options) {
    if (option.type === 'divider') {
      pendingSeparator = true
      continue
    }
    if (import.meta.env.DEV && option.children?.length) {
      console.warn(
        `[toDropdownCollection] 条目 ${option.key} 带了 children，但组件库的代铺路径只吃扁平条目，`
        + '子层会被丢掉。要子菜单请改摆 XhMenuSub 部件（参照 TabbarContextMenu.vue）。',
      )
    }
    out.push({
      value: option.key,
      label: typeof option.label === 'string'
        ? option.label
        : option.key,
      ...(option.disabled
        ? { disabled: true }
        : {}),
      ...(option.tone
        ? { tone: option.tone }
        : {}),
      ...(pendingSeparator || option.divider
        ? { separatorBefore: true }
        : {}),
    })
    pendingSeparator = false
  }

  return out
}

/** 按 key 建索引，供条目插槽回查图标与富标签 */
export function indexDropdownOptions(
  options: ReadonlyArray<AppDropdownOption>,
): Map<string, AppDropdownOption> {
  return new Map(options.filter(o => o.type !== 'divider').map(o => [o.key, o]))
}
