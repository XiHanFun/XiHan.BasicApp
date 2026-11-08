/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { MenuNodeMeta } from '@xihan-ui/headless'
import type { VNode } from 'vue'
import type { ActionSchema } from './types'
import { h } from 'vue'
import { Icon } from '~/iconify'

/**
 * 操作落进下拉菜单时的行首图标，经 item-prefix 插槽落在 item-icon 格
 * MenuNode 的 icon 只收 @xihan-ui/icons 的图标记录，Iconify 图标走插槽；有一条带图标就整列铺开（无图标的留空位对齐），一条都没有不给插槽
 * 尺寸见 ui.css，颜色随条目字色（危险条目随字转红）
 */
export function actionMenuPrefix(
  actions: ReadonlyArray<Pick<ActionSchema, 'key' | 'icon'>>,
): ((node: MenuNodeMeta) => VNode[]) | undefined {
  const icons = new Map(actions.flatMap(a => (a.icon
    ? [
        [a.key, a.icon] as const,
      ]
    : [])))
  if (icons.size === 0)
    return undefined
  return (node) => {
    const icon = icons.get(node.value)
    return icon
      ? [
          h(Icon, { icon }),
        ]
      : []
  }
}
