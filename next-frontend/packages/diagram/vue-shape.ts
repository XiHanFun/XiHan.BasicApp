import type { Component } from 'vue'
import { getTeleport, register } from '@antv/x6-vue-shape'

/**
 * Vue 组件节点注册
 *
 * 节点组件内经 `inject('getNode')` 拿到 X6 节点并读取 `getData()`；
 * `effect: ['data']` 使 updateNodeData 后组件自动重渲染。
 * 端口分组统一为 in（左）/ out（右），每个节点实例的端口开关由 DiagramNode.ports 决定。
 */

export interface VueShapeDefinition {
  /** 形状名（DiagramNode.shape 引用） */
  shape: string
  component: Component
  width: number
  height: number
}

const PORT_GROUPS = {
  in: {
    position: 'left',
    attrs: {
      circle: { r: 4, magnet: true, stroke: '#94a3b8', fill: '#fff', strokeWidth: 1.5 },
    },
  },
  out: {
    position: 'right',
    attrs: {
      circle: { r: 4, magnet: true, stroke: '#94a3b8', fill: '#fff', strokeWidth: 1.5 },
    },
  },
}

const registered = new Set<string>()

/** 注册 Vue 组件形状（幂等，可在页面 setup 阶段安全重复调用） */
export function registerVueShape(definition: VueShapeDefinition): void {
  if (registered.has(definition.shape))
    return
  registered.add(definition.shape)

  register({
    shape: definition.shape,
    width: definition.width,
    height: definition.height,
    component: definition.component,
    effect: ['data'],
    ports: { groups: PORT_GROUPS },
  })
}

/**
 * 传送渲染容器：把 Vue 节点渲染进宿主应用上下文（i18n/主题/注入可用）。
 * 须在使用 Vue 节点的页面渲染一次（XDiagram 已内置）。
 */
export const DiagramTeleport = getTeleport()
