/**
 * Setup Store 的 $reset 兜底插件单元测试。
 * 职责边界：只测插件本身——自定义 $reset 优先、无自定义时用初始快照回退、
 * 快照是深拷贝（嵌套对象不共享引用）、以及 JSON 深拷贝带来的表达力边界。
 */
import { createPinia, defineStore, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createApp, ref } from 'vue'
import { resetSetupStorePlugin } from './reset-store'

let seq = 0
/** 每个用例用独立 store id，避免同一个 pinia 实例外的定义互相串味 */
function nextId(prefix: string): string {
  seq += 1
  return `${prefix}-${seq}`
}

/**
 * pinia.use 注册的插件要等宿主 app 安装 pinia 后才会进入 _p 生效，
 * 这里补一个空 app —— 少了它插件永远不会跑，用例会「假失败」在原生 $reset 上。
 */
function usePluginPinia(): void {
  const pinia = createPinia()
  pinia.use(resetSetupStorePlugin())
  createApp({ render: () => null }).use(pinia)
  setActivePinia(pinia)
}

beforeEach(() => {
  usePluginPinia()
})

describe('无自定义 $reset 的 setup store', () => {
  it('原生 $reset 抛错时回退到初始快照，基础字段被还原', () => {
    const useCounter = defineStore(nextId('counter'), () => {
      const count = ref(0)
      const label = ref('初始')
      return { count, label }
    })
    const store = useCounter()
    store.count = 42
    store.label = '改过了'

    store.$reset()

    expect(store.count).toBe(0)
    expect(store.label).toBe('初始')
  })

  it('未加插件时 setup store 的 $reset 直接抛错 —— 这正是插件存在的理由', () => {
    setActivePinia(createPinia())
    const useBare = defineStore(nextId('bare'), () => ({ count: ref(0) }))
    const store = useBare()

    expect(() => store.$reset()).toThrow(/setup syntax/)
  })

  it('嵌套对象按深拷贝还原，改动不会渗回快照', () => {
    const useNested = defineStore(nextId('nested'), () => {
      const config = ref({ list: [1, 2], inner: { flag: false } })
      return { config }
    })
    const store = useNested()
    store.config.list.push(3)
    store.config.inner.flag = true

    store.$reset()

    expect(store.config).toEqual({ list: [1, 2], inner: { flag: false } })
  })

  it('连续两次 reset 都能还原 —— 快照没有被上一次还原污染', () => {
    const useNested = defineStore(nextId('nested-twice'), () => ({
      config: ref({ list: [1] }),
    }))
    const store = useNested()

    store.config.list.push(2)
    store.$reset()
    store.config.list.push(3)
    store.$reset()

    expect(store.config.list).toEqual([1])
  })

  it('reset 后写入新值不会改动快照，第三次 reset 仍回到最初值', () => {
    const useCounter = defineStore(nextId('counter-again'), () => ({ count: ref(7) }))
    const store = useCounter()

    store.$reset()
    store.count = 100
    store.$reset()

    expect(store.count).toBe(7)
  })

  it('快照只覆盖已有键，不会删除 reset 之前新增的状态键', () => {
    const useLoose = defineStore(nextId('loose'), () => {
      const known = ref(1)
      return { known }
    })
    const store = useLoose()
    store.known = 2
    ;(store.$state as unknown as Record<string, unknown>).extra = '后加的'

    store.$reset()

    expect(store.known).toBe(1)
    expect((store.$state as unknown as Record<string, unknown>).extra).toBe('后加的')
  })
})

describe('深拷贝快照的表达力边界（当前真实行为）', () => {
  it('初始值为 undefined 的字段会被 JSON 序列化丢弃，reset 还原不回来', () => {
    const useUndef = defineStore(nextId('undef'), () => ({
      maybe: ref<string | undefined>(undefined),
    }))
    const store = useUndef()
    store.maybe = '有值了'

    store.$reset()

    expect(store.maybe).toBe('有值了')
  })

  it('初始值为 Date 的字段 reset 后变成 ISO 字符串而非 Date 实例', () => {
    const useDate = defineStore(nextId('date'), () => ({
      at: ref<Date | string>(new Date('2024-01-01T00:00:00.000Z')),
    }))
    const store = useDate()
    store.at = new Date('2030-01-01T00:00:00.000Z')

    store.$reset()

    expect(store.at).toBe('2024-01-01T00:00:00.000Z')
    expect(store.at instanceof Date).toBe(false)
  })
})

describe('自定义 $reset 优先', () => {
  it('store 自带 $reset 时插件调用它，而不是套用快照', () => {
    const spy = vi.fn()
    const useCustom = defineStore(nextId('custom'), () => {
      const value = ref(1)
      function $reset() {
        spy()
        value.value = 99
      }
      return { value, $reset }
    })
    const store = useCustom()
    store.value = 5

    store.$reset()

    expect(spy).toHaveBeenCalledTimes(1)
    expect(store.value).toBe(99)
  })

  it('自定义 $reset 自己抛异常时，插件兜底套用初始快照', () => {
    const useThrowing = defineStore(nextId('throwing'), () => {
      const value = ref(3)
      function $reset(): void {
        throw new Error('自定义重置失败')
      }
      return { value, $reset }
    })
    const store = useThrowing()
    store.value = 88

    expect(() => store.$reset()).not.toThrow()
    expect(store.value).toBe(3)
  })

  // 回归锚点（缺陷 33）：插件曾以 originalReset() 裸调用保存下来的原生 $reset，
  // options store 的原生实现内部依赖 this.$patch，丢 this 必抛 TypeError 并被 catch 吞掉，
  // 退化成快照回退——state() 里的动态默认值（递增序号 / Date.now() / 随机 id）不会重新生成。
  it('options store 的原生 $reset 被绑回 store 调用，state() 的动态默认值重新生成', () => {
    let seed = 0
    const useSeeded = defineStore(nextId('options-seeded'), {
      state: () => ({ seed: ++seed }),
    })
    const store = useSeeded()
    expect(store.seed).toBe(1)
    store.seed = 99

    store.$reset()

    // 原生语义：state() 重新求值并被 $patch 应用，得到新的一轮默认值 2，而不是初始快照的 1
    expect(seed).toBe(2)
    expect(store.seed).toBe(2)
  })
})
