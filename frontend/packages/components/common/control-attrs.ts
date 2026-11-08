/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { computed, useAttrs } from 'vue'

/**
 * 是否角色标记：解剖位、随视觉盒走的形态轴与家族标记
 * 与组件库 asChild 合并时滤掉同一组：落到真正的控件上会盖掉部件自己的角色标记，
 * 皮肤整条选不中，或在控件外再画一层视觉盒
 */
function isRoleMarker(key: string): boolean {
  return key === 'data-scope' || key === 'data-part' || key === 'data-variant' || key.startsWith('data-xh-')
}

/**
 * 把薄封装标签上的属性转交给里面真正的控件
 * 字段接线（id 与 aria-*）也只走这条：XhFieldControl 经 asChild 合并到唯一子节点即封装的 attrs，原样转交只落一次；不从字段上下文取，否则子树里的弹窗搜索框、输入组、字段数组各行会拿到同一 id，label 的 for 落空、读屏念成外层字段
 * 封装须做 XhFieldControl 的直接子节点（隔层要透传 attrs，外层 `:as-child="false"` 时由那层调 useFieldControl 显式 v-bind）；组件声明 `inheritAttrs: false`，根上绑 attrs.class 与 attrs.style，可聚焦部件上写 `v-bind="controlAttrs"`
 */
export function useControlAttrs() {
  const attrs = useAttrs()

  const controlAttrs = computed(() => {
    const rest: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(attrs)) {
      if (key === 'class' || key === 'style' || isRoleMarker(key))
        continue
      rest[key] = value
    }
    return rest
  })

  return { attrs, controlAttrs }
}
