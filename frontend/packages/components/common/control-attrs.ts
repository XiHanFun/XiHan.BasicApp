import { useFieldControl } from '@xihan-ui/vue'
import { computed, useAttrs } from 'vue'

/**
 * 薄封装把落在自己标签上的属性、以及字段的控件接线属性，一并转交给里面真正的控件。
 *
 * 接线属性（id 与 aria-*）直接从字段上下文取，不经封装根节点中转：
 * 标签的 `for` 只对可标注元素生效，落在封装的根 div 上什么也不会发生，且不报错。
 * 不在字段里时这一份为空，封装照常工作。
 *
 * 用法：组件声明 `inheritAttrs: false`，根上写 `:class="[..., attrs.class]" :style="attrs.style"`，
 * 可聚焦的那个部件上写 `v-bind="controlAttrs"`。
 */
export function useControlAttrs() {
  const attrs = useAttrs()
  const fieldControl = useFieldControl()

  const controlAttrs = computed(() => {
    const rest: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(attrs)) {
      if (key === 'class' || key === 'style' || key === 'data-scope' || key === 'data-part')
        continue
      rest[key] = value
    }
    return { ...rest, ...fieldControl.value }
  })

  return { attrs, controlAttrs }
}
