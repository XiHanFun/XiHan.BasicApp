/**
 * packages/diagram/vue-shape.ts 的形状注册。
 *
 * 职责边界：只验证本包对 x6-vue-shape 的封装决策——注册幂等、端口分组固定为 in(左)/out(右)、
 * 以及 effect 必须声明 'data'（少了它 updateNodeData 后 Vue 节点不会重渲染）。
 * X6 引擎整体被替身，断言的是「传给引擎什么」，不是引擎自身行为。
 */
import type { Component } from 'vue'
import { describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'

const x6 = vi.hoisted(() => ({
  registerCalls: [] as Record<string, unknown>[],
  teleport: { name: 'FakeTeleport' },
}))

vi.mock('@antv/x6-vue-shape', () => ({
  register: (definition: Record<string, unknown>) => {
    x6.registerCalls.push(definition)
  },
  getTeleport: () => x6.teleport,
}))

const { DiagramTeleport, registerVueShape } = await import('./vue-shape')

const probeComponent: Component = defineComponent({ render: () => null })

function callsFor(shape: string): Record<string, unknown>[] {
  return x6.registerCalls.filter(call => call.shape === shape)
}

describe('形状注册', () => {
  it('把形状名、组件与尺寸原样交给引擎注册', () => {
    registerVueShape({ shape: 'probe-basic', component: probeComponent, width: 180, height: 60 })

    const [call] = callsFor('probe-basic')
    expect(call).toMatchObject({
      shape: 'probe-basic',
      component: probeComponent,
      width: 180,
      height: 60,
    })
  })

  it('effect 声明 data，否则 updateNodeData 之后 Vue 节点不会重渲染', () => {
    registerVueShape({ shape: 'probe-effect', component: probeComponent, width: 10, height: 10 })

    expect(callsFor('probe-effect')[0]?.effect).toStrictEqual(['data'])
  })

  it('端口分组固定为左入右出两组，且都是可连接的磁吸点', () => {
    registerVueShape({ shape: 'probe-ports', component: probeComponent, width: 10, height: 10 })

    const ports = callsFor('probe-ports')[0]?.ports as {
      groups: Record<string, { attrs: { circle: { magnet: boolean } }, position: string }>
    }
    expect(Object.keys(ports.groups).sort()).toStrictEqual(['in', 'out'])
    expect(ports.groups.in?.position).toBe('left')
    expect(ports.groups.out?.position).toBe('right')
    expect(ports.groups.in?.attrs.circle.magnet).toBe(true)
    expect(ports.groups.out?.attrs.circle.magnet).toBe(true)
  })

  it('同名形状重复注册只落地一次，页面 setup 阶段可安全反复调用', () => {
    registerVueShape({ shape: 'probe-idempotent', component: probeComponent, width: 10, height: 10 })
    registerVueShape({ shape: 'probe-idempotent', component: probeComponent, width: 999, height: 999 })

    expect(callsFor('probe-idempotent')).toHaveLength(1)
    expect(callsFor('probe-idempotent')[0]?.width).toBe(10)
  })

  it('不同形状名各自注册互不影响', () => {
    registerVueShape({ shape: 'probe-multi-a', component: probeComponent, width: 10, height: 10 })
    registerVueShape({ shape: 'probe-multi-b', component: probeComponent, width: 20, height: 20 })

    expect(callsFor('probe-multi-a')).toHaveLength(1)
    expect(callsFor('probe-multi-b')).toHaveLength(1)
  })
})

describe('传送渲染容器', () => {
  it('直接暴露引擎的传送容器，页面渲染一次即可让 Vue 节点用上宿主上下文', () => {
    expect(DiagramTeleport).toBe(x6.teleport)
  })
})
