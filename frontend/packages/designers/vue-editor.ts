/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PropertyEditor, PropertyEditorContext } from '@xihan-ui/designer-kit/properties'
import type { AppContext, VNode } from 'vue'
import { defineComponent, h, render, shallowRef } from 'vue'

/**
 * Vue 渲染函数做成设计器的属性编辑器，上下文每次更新即重渲
 * 挂在调用方给的应用上下文下，照常能用 i18n、Pinia 与应用组件；设计器撤掉编辑器时卸载
 */
export function createVuePropertyEditor<D, T>(
  appContext: AppContext | null,
  renderEditor: (context: PropertyEditorContext<D, T>) => VNode,
): PropertyEditor<D, T> {
  return (initial) => {
    const element = document.createElement('div')
    const context = shallowRef(initial)
    const vnode = h(defineComponent({
      setup: () => () => renderEditor(context.value),
    }))
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
