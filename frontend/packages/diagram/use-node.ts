import type { Node } from '@antv/x6'
import type { Ref } from 'vue'
import { inject, onBeforeUnmount, ref } from 'vue'

/**
 * Vue 节点组件内读取节点数据（响应 updateNodeData 的全量替换）
 *
 * 封装 x6-vue-shape 的 `inject('getNode')` 契约，节点组件零 X6 依赖。
 * 只能在经 registerVueShape 注册的组件 setup 中调用。
 */
export function useDiagramNode<T extends Record<string, unknown>>(fallback: T): { id: string, data: Ref<T> } {
  const getNode = inject<() => Node>('getNode')
  const node = getNode?.()

  const data = ref<T>({ ...fallback, ...(node?.getData() as T | undefined) }) as Ref<T>

  function sync() {
    if (node)
      data.value = { ...(node.getData() as T) }
  }

  node?.on('change:data', sync)
  onBeforeUnmount(() => {
    node?.off('change:data', sync)
  })

  return { id: node?.id ?? '', data }
}
