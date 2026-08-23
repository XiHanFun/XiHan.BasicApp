import type { PropType, VNode, VNodeChild } from 'vue'
import { defineComponent } from 'vue'

/**
 * 把一段 VNodeChild 塞进模板里。
 *
 * Schema 层的列渲染、操作渲染都以 `(row) => VNodeChild` 的形式给出，模板里没有直接
 * 渲染它的语法。写成 `<component :is="() => fn()">` 会让每次重渲都产出一个新的组件
 * 类型，整棵子树随之卸载重挂；本组件是稳定的类型，只有 content 变。
 */
export const VNodeRender = defineComponent({
  name: 'VNodeRender',
  props: {
    content: {
      type: [String, Number, Boolean, Object, Array] as PropType<VNodeChild>,
      default: null,
    },
  },
  setup(props) {
    return () => props.content as VNode
  },
})
