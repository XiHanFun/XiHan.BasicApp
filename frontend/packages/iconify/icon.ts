/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { IconProps } from '@iconify/vue/offline'
import { Icon as OfflineIcon } from '@iconify/vue/offline'
import { defineComponent, h } from 'vue'
import { hasIconSet, isIconSetLoaded, loadIconSet } from './offline'

function prefixOf(icon: IconProps['icon']): string | undefined {
  if (typeof icon !== 'string')
    return undefined
  const index = icon.indexOf(':')
  return index > 0
    ? icon.slice(0, index)
    : undefined
}

/**
 * 离线图标：属性与插槽原样交给 Iconify 的离线 Icon
 * 图标所在的图标集还没装时先装，装好后换 key 重新挂载（离线 Icon 不会因为图标集后到而重渲染）
 */
export const Icon = defineComponent<IconProps>((props, { attrs, slots }) => () => {
  const prefix = prefixOf(props.icon)
  let ready = true
  if (prefix && hasIconSet(prefix) && !isIconSetLoaded(prefix)) {
    ready = false
    void loadIconSet(prefix)
  }
  return h(OfflineIcon, {
    ...attrs,
    icon: props.icon,
    key: ready
      ? 'ready'
      : 'pending',
  }, slots)
}, {
  name: 'Icon',
  inheritAttrs: false,
  props: ['icon'],
})
