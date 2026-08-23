import { computed, useAttrs } from 'vue'

/**
 * 薄封装把落在自己标签上的属性转交给里面真正的控件。
 *
 * `XhFieldControl` 不渲染自己的节点，它把 id、aria-* 与角色标记合并到唯一的子节点上。
 * 子节点若是这些薄封装，那组属性就会落到封装的根上：id 与 aria-* 落在根上时 label 的 `for`
 * 指不到可聚焦元素，而 `data-scope` / `data-part` 会盖掉部件自己的角色标记，皮肤整条选不中——
 * 输入框、下拉、开关都会退回没有描边与高度的裸元素。
 *
 * 用法：组件声明 `inheritAttrs: false`，根上写 `:class="[..., attrs.class]" :style="attrs.style"`，
 * 可聚焦的那个部件上写 `v-bind="controlAttrs"`。
 */
export function useControlAttrs() {
  const attrs = useAttrs()

  const controlAttrs = computed(() => {
    const rest: Record<string, unknown> = {}
    for (const [key, value] of Object.entries(attrs)) {
      if (key === 'class' || key === 'style' || key === 'data-scope' || key === 'data-part')
        continue
      rest[key] = value
    }
    return rest
  })

  return { attrs, controlAttrs }
}
