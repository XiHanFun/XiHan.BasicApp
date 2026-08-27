/**
 * packages/diagram/use-node.ts 的节点数据读取。
 *
 * 职责边界：验证 inject('getNode') 契约的三种处境（未注入 / 已注入 / 数据变更），
 * 以及**组件卸载必须解绑 change:data 监听**——不解绑会在节点复用时把旧组件的 ref 一起写坏。
 * X6 Node 用最小替身，只保留 id / getData / on / off。
 */
import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { defineComponent, h } from 'vue'
import { useDiagramNode } from './use-node'

interface NodeProbe {
  id: string
  data: Record<string, unknown>
  handlers: Map<string, ((...args: unknown[]) => void)[]>
  offCalls: string[]
  emitDataChange: () => void
}

function createNodeProbe(id: string, data: Record<string, unknown>): NodeProbe {
  const handlers = new Map<string, ((...args: unknown[]) => void)[]>()
  const probe: NodeProbe = {
    id,
    data,
    handlers,
    offCalls: [],
    emitDataChange() {
      for (const handler of handlers.get('change:data') ?? []) {
        handler()
      }
    },
  }
  return probe
}

/** 把探针包装成 use-node 需要的 X6 Node 形态 */
function asNode(probe: NodeProbe) {
  return {
    id: probe.id,
    getData: () => probe.data,
    on(event: string, handler: (...args: unknown[]) => void) {
      const list = probe.handlers.get(event) ?? []
      list.push(handler)
      probe.handlers.set(event, list)
    },
    off(event: string, handler: (...args: unknown[]) => void) {
      probe.offCalls.push(event)
      const list = (probe.handlers.get(event) ?? []).filter(item => item !== handler)
      probe.handlers.set(event, list)
    },
  }
}

interface Captured {
  id: string
  read: () => Record<string, unknown>
}

function mountWithNode(probe: NodeProbe | null, fallback: Record<string, unknown>) {
  let captured: Captured | null = null
  const wrapper = mount(defineComponent({
    setup() {
      const node = useDiagramNode(fallback)
      captured = { id: node.id, read: () => node.data.value }
      return () => h('div')
    },
  }), {
    global: {
      provide: probe ? { getNode: () => asNode(probe) } : {},
    },
  })
  return { captured: captured as unknown as Captured, unmount: () => wrapper.unmount() }
}

describe('未注入 getNode 的处境', () => {
  it('脱离 X6 单独渲染节点组件时用 fallback 兜底，id 为空串而不是 undefined', () => {
    const { captured, unmount } = mountWithNode(null, { label: '默认名', count: 0 })

    expect(captured.id).toBe('')
    expect(captured.read()).toStrictEqual({ label: '默认名', count: 0 })
    unmount()
  })

  it('未注入时卸载不抛错', () => {
    const { unmount } = mountWithNode(null, { label: '默认名' })

    expect(() => unmount()).not.toThrow()
  })
})

describe('已注入 getNode 的处境', () => {
  it('节点数据覆盖 fallback，节点没给的字段保留 fallback 值', () => {
    const probe = createNodeProbe('n1', { label: '节点名' })

    const { captured, unmount } = mountWithNode(probe, { label: '默认名', color: 'blue' })

    expect(captured.id).toBe('n1')
    expect(captured.read()).toStrictEqual({ label: '节点名', color: 'blue' })
    unmount()
  })

  it('节点数据为空对象时完全落回 fallback', () => {
    const probe = createNodeProbe('n2', {})

    const { captured, unmount } = mountWithNode(probe, { label: '默认名' })

    expect(captured.read()).toStrictEqual({ label: '默认名' })
    unmount()
  })

  it('change:data 事件把 ref 全量替换为节点最新数据，fallback 不再兜底', () => {
    const probe = createNodeProbe('n3', { label: '旧', color: 'blue' })
    const { captured, unmount } = mountWithNode(probe, { label: '默认名', color: 'red' })

    probe.data = { label: '新' }
    probe.emitDataChange()

    expect(captured.read()).toStrictEqual({ label: '新' })
    unmount()
  })

  it('订阅的正是 change:data 事件', () => {
    const probe = createNodeProbe('n4', {})
    const { unmount } = mountWithNode(probe, {})

    expect(probe.handlers.get('change:data')).toHaveLength(1)
    unmount()
  })
})

describe('卸载清理', () => {
  it('组件卸载后解绑 change:data，节点复用时不会再回写已销毁组件的 ref', () => {
    const probe = createNodeProbe('n5', { label: '旧' })
    const { captured, unmount } = mountWithNode(probe, { label: '默认名' })

    unmount()

    expect(probe.offCalls).toStrictEqual(['change:data'])
    expect(probe.handlers.get('change:data')).toStrictEqual([])

    probe.data = { label: '卸载后的变更' }
    probe.emitDataChange()
    expect(captured.read()).toStrictEqual({ label: '旧' })
  })
})
