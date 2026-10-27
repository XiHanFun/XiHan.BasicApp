import { computed, useAttrs } from 'vue'

/**
 * 角色标记：解剖两位（data-scope / data-part）、随视觉盒走的形态轴（data-variant）与家族标记（data-xh-*）。
 *
 * 与组件库 asChild 合并时滤掉的是同一组：落到真正的控件上会盖掉部件自己的角色标记，
 * 皮肤整条选不中，或者在控件外再画一层视觉盒。
 */
function isRoleMarker(key: string): boolean {
  return key === 'data-scope' || key === 'data-part' || key === 'data-variant' || key.startsWith('data-xh-')
}

/**
 * 薄封装把落在自己标签上的属性转交给里面真正的控件。
 *
 * 字段的接线属性（id 与 aria-*）也只走这一条：XhFieldControl 默认（asChild）把接线合并到它唯一的子节点上，
 * 子节点是封装时这组属性就是封装的 attrs，原样转交即落在真正的控件上，而且只落一次。
 * 不再另从字段上下文取：上下文对整棵子树可见，弹窗里的搜索框、输入组里的控件、字段数组的每一行
 * 都会拿到同一份 id 与名字——id 重复，label 的 for 落空，读屏把它们都念成外层字段。
 *
 * 对调用方的要求：
 * - 封装做 XhFieldControl 的直接子节点；中间隔着别的组件时，那一层要把 attrs 透传到封装上；
 * - 外层写了 `:as-child="false"` 时接线不会自动下发，由那一层组件调用 useFieldControl，
 *   把结果显式 v-bind 到它真正的控件（封装）上。
 *
 * 用法：组件声明 `inheritAttrs: false`，根上写 `:class="[..., attrs.class]" :style="attrs.style"`，
 * 可聚焦的那个部件上写 `v-bind="controlAttrs"`。
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
