/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { Tone } from '@xihan-ui/core'
import type { MenuNode } from '@xihan-ui/headless'
import type { VNode } from 'vue'
import { XhButton, XhMenuRoot } from '@xihan-ui/vue'
import { h } from 'vue'
import { Icon } from '~/iconify'
import { actionMenuPrefix } from './action-menu'
import { actionMenuTone } from './action-tone'

/** 落进行操作菜单的一条操作 */
export interface RowMenuAction {
  key: string
  title: string
  /** iconify 图标名，落在条目行首 */
  icon?: string
  /** 只有 danger / warning 会着色 */
  tone?: Tone
  disabled?: boolean
}

/**
 * 行操作列：一颗「更多」描边钮，点开是这一行的操作菜单
 * 列表页（SchemaPage）与页内自写的表格共用，外观与交互一致；没有可用操作时显示「-」
 * @param actions 这一行此刻可见的操作
 * @param moreLabel 触发钮文案（调用方按自己的 i18n 取）
 * @param onSelect 选中条目时回调条目的 key
 */
export function renderRowActionMenu(actions: ReadonlyArray<RowMenuAction>, moreLabel: string, onSelect: (key: string) => void): VNode {
  if (actions.length === 0) {
    return h('span', { class: 'text-foreground/30' }, '-')
  }

  const collection: MenuNode[] = actions.map(action => ({
    value: action.key,
    label: action.title,
    disabled: action.disabled ?? false,
    tone: actionMenuTone(action.tone),
  }))
  const itemPrefix = actionMenuPrefix(actions)
  return h(
    XhMenuRoot,
    {
      collection,
      // 借用下面那颗按钮当触发器。不借的话菜单根会自己包一颗裸 button，
      // 而 menu 皮肤没有 trigger 这一部件，露出的是浏览器默认按钮样式
      triggerAsChild: true,
      // 按落定那一侧的可用高度铺开：组件库缺省的上限是 20rem，本应用根字号 14px 下只有 280px，八项就露不全
      fitAvailableHeight: true,
      onItemSelect: (details: { value: string }) => onSelect(details.value),
    },
    {
      trigger: () => h(
        XhButton,
        { variant: 'outline', size: 'sm' },
        () => [
          moreLabel,
          h(Icon, { icon: 'lucide:chevron-down' }),
        ],
      ),
      ...(itemPrefix
        ? { 'item-prefix': itemPrefix }
        : {}),
    },
  )
}
