import type { PropertyEditor, PropertyEditorContext } from '@xihan-ui/designer-kit/properties'
import type { AppContext, VNode } from 'vue'
import { defineComponent, h, render, shallowRef } from 'vue'

/**
 * 把一段 Vue 渲染函数做成设计器的属性编辑器：设计器每次交来新的上下文（值、只读、被编辑的对象）就重渲一遍。
 *
 * 渲染挂在调用方给的应用上下文下，编辑器里照常能用 i18n、Pinia 与应用组件；
 * 设计器撤掉编辑器时卸载这棵子树。
 */
export function createVuePropertyEditor<D, T>(
  appContext: AppContext | null,
  renderEditor: (context: PropertyEditorContext<D, T>) => VNode,
): PropertyEditor<D, T> {
  return (initial) => {
    const element = document.createElement('div')
    const context = shallowRef(initial)
    const vnode = h(defineComponent({ setup: () => () => renderEditor(context.value) }))
    vnode.appContext = appContext
    render(vnode, element)
    return {
      element,
      update: (next) => {
        context.value = next
      },
      destroy: () => render(null, element),
    }
  }
}
