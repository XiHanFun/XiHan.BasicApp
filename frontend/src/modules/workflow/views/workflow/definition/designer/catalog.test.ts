/**
 * 流程设计器活动目录（catalog.ts）单元测试。
 *
 * 职责边界：这张表同时驱动左侧节点面板与右侧属性表单，且与后端内置活动、i18n 文案三方对齐。
 * 用例锁定三件事：目录自身结构合法（类型唯一、属性键唯一、下拉必须带选项）、
 * 分类顺序覆盖全部活动、以及每个 labelKey 在两语言文案里都能取到值。
 */
import type { ActivityTypeMeta } from './catalog'
import { describe, expect, it } from 'vitest'
import enUS from '../../../../locales/en-US'
import zhCN from '../../../../locales/zh-CN'
import { ACTIVITY_CATALOG, ACTIVITY_MAP, CATEGORY_ORDER } from './catalog'

type MessageTree = Record<string, unknown>

function designerNode(messages: MessageTree, section: string): MessageTree {
  const workflow = messages.workflow as MessageTree | undefined
  const designer = workflow?.designer as MessageTree | undefined
  const node = designer?.[section] as MessageTree | undefined
  if (!node) {
    throw new Error(`文案缺少 workflow.designer.${section} 节点`)
  }
  return node
}

const locales: [string, MessageTree][] = [
  ['zh-CN', zhCN as MessageTree],
  ['en-US', enUS as MessageTree],
]

function allProps(): { meta: ActivityTypeMeta, prop: ActivityTypeMeta['props'][number] }[] {
  return ACTIVITY_CATALOG.flatMap(meta => meta.props.map(prop => ({ meta, prop })))
}

describe('活动目录的自身结构', () => {
  it('活动类型编码唯一——重复会让节点面板出现两个同名节点', () => {
    const types = ACTIVITY_CATALOG.map(meta => meta.type)

    expect(new Set(types).size).toBe(types.length)
  })

  it('目录非空且包含开始与结束两个必备活动', () => {
    const types = ACTIVITY_CATALOG.map(meta => meta.type)

    expect(types.length).toBeGreaterThan(10)
    expect(types).toContain('Start')
    expect(types).toContain('End')
  })

  it('活动类型编码是大驼峰，与后端 WorkflowActivityTypes 的成员名同形', () => {
    const bad = ACTIVITY_CATALOG.filter(meta => !/^[A-Z][A-Za-z]*$/.test(meta.type)).map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('每个活动都有图标且统一走 lucide 图标集', () => {
    const bad = ACTIVITY_CATALOG
      .filter(meta => !meta.icon.startsWith('lucide:') || meta.icon.length <= 'lucide:'.length)
      .map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('labelKey 与 categoryKey 都是小写下划线形式的 i18n 键，不是可见文案', () => {
    const bad = ACTIVITY_CATALOG
      .filter(meta => !/^[a-z][a-z0-9_]*$/.test(meta.labelKey) || !/^[a-z][a-z0-9_]*$/.test(meta.categoryKey))
      .map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('开始 / 结束 / 判定 / 并行四个纯控制节点不带任何属性', () => {
    for (const type of ['Start', 'End', 'Decision', 'Parallel']) {
      expect(ACTIVITY_MAP[type]?.props).toEqual([])
    }
  })
})

describe('活动索引表与目录的一致性', () => {
  it('索引表的键集合与目录的类型集合完全一致', () => {
    expect(Object.keys(ACTIVITY_MAP).sort()).toEqual(ACTIVITY_CATALOG.map(meta => meta.type).sort())
  })

  it('索引表存的是目录里的同一个对象，不是拷贝——属性面板改不到脏副本', () => {
    for (const meta of ACTIVITY_CATALOG) {
      expect(ACTIVITY_MAP[meta.type]).toBe(meta)
    }
  })

  it('未注册的活动类型取不到元数据，调用方需自行兜底', () => {
    expect(ACTIVITY_MAP.NotRegistered).toBeUndefined()
  })
})

describe('分类顺序', () => {
  it('分类顺序无重复', () => {
    expect(new Set(CATEGORY_ORDER).size).toBe(CATEGORY_ORDER.length)
  })

  it('每个活动的分类都在顺序表里，否则该活动在面板上无处安放', () => {
    const known = new Set(CATEGORY_ORDER)
    const bad = ACTIVITY_CATALOG.filter(meta => !known.has(meta.categoryKey)).map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('顺序表里没有空分类，否则面板会渲染出空分组标题', () => {
    const used = new Set(ACTIVITY_CATALOG.map(meta => meta.categoryKey))
    const empty = CATEGORY_ORDER.filter(category => !used.has(category))

    expect(empty).toEqual([])
  })
})

describe('活动属性描述符', () => {
  it('同一活动内属性键不重复——重复会让表单两个控件写同一个 properties 字段', () => {
    const bad = ACTIVITY_CATALOG
      .filter(meta => new Set(meta.props.map(prop => prop.key)).size !== meta.props.length)
      .map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('属性键是大驼峰，与后端活动属性键同形', () => {
    const bad = allProps()
      .filter(({ prop }) => !/^[A-Z][A-Za-z]*$/.test(prop.key))
      .map(({ meta, prop }) => `${meta.type}.${prop.key}`)

    expect(bad).toEqual([])
  })

  it('input 类型只允许目录里约定的七种控件', () => {
    const allowed = new Set(['text', 'textarea', 'number', 'boolean', 'select', 'tags', 'json'])
    const bad = allProps()
      .filter(({ prop }) => !allowed.has(prop.input))
      .map(({ meta, prop }) => `${meta.type}.${prop.key} → ${prop.input}`)

    expect(bad).toEqual([])
  })

  it('select 控件必须给出非空选项，非 select 控件不得挂选项', () => {
    const missing = allProps()
      .filter(({ prop }) => prop.input === 'select' && (prop.options?.length ?? 0) === 0)
      .map(({ meta, prop }) => `${meta.type}.${prop.key}`)
    const extra = allProps()
      .filter(({ prop }) => prop.input !== 'select' && prop.options !== undefined)
      .map(({ meta, prop }) => `${meta.type}.${prop.key}`)

    expect(missing).toEqual([])
    expect(extra).toEqual([])
  })

  it('select 的选项 value 在组内唯一且非空', () => {
    const bad: string[] = []
    for (const { meta, prop } of allProps()) {
      const options = prop.options ?? []
      if (new Set(options.map(option => option.value)).size !== options.length) {
        bad.push(`${meta.type}.${prop.key} 选项重复`)
      }
      if (options.some(option => option.value.length === 0 || option.label.length === 0)) {
        bad.push(`${meta.type}.${prop.key} 选项有空值`)
      }
    }

    expect(bad).toEqual([])
  })

  it('声明了 outcomes 的活动，出口取值非空且不重复', () => {
    const bad = ACTIVITY_CATALOG
      .filter(meta => meta.outcomes !== undefined
        && (meta.outcomes.length === 0 || new Set(meta.outcomes).size !== meta.outcomes.length))
      .map(meta => meta.type)

    expect(bad).toEqual([])
  })

  it('人工任务的三种完成策略与常见出口保持约定取值', () => {
    const userTask = ACTIVITY_MAP.UserTask
    const policy = userTask?.props.find(prop => prop.key === 'CompletionPolicy')

    expect(policy?.options?.map(option => option.value)).toEqual(['Any', 'All', 'Sequential'])
    expect(userTask?.outcomes).toEqual(['approved', 'rejected', 'timeout'])
  })
})

describe('活动目录与两语言文案的对齐', () => {
  it('每个活动的 labelKey 在两语言的 workflow.designer.activity 下都有文案', () => {
    const bad: string[] = []
    for (const [locale, messages] of locales) {
      const activity = designerNode(messages, 'activity')
      for (const meta of ACTIVITY_CATALOG) {
        if (typeof activity[meta.labelKey] !== 'string') {
          bad.push(`${locale}: activity.${meta.labelKey}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('每个分类在两语言的 workflow.designer.category 下都有文案', () => {
    const bad: string[] = []
    for (const [locale, messages] of locales) {
      const category = designerNode(messages, 'category')
      for (const key of CATEGORY_ORDER) {
        if (typeof category[key] !== 'string') {
          bad.push(`${locale}: category.${key}`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('每个属性的 labelKey 在两语言的 workflow.designer.prop 下都有文案', () => {
    const bad: string[] = []
    for (const [locale, messages] of locales) {
      const prop = designerNode(messages, 'prop')
      for (const { meta, prop: descriptor } of allProps()) {
        if (typeof prop[descriptor.labelKey] !== 'string') {
          bad.push(`${locale}: prop.${descriptor.labelKey}（来自 ${meta.type}）`)
        }
      }
    }

    expect(bad).toEqual([])
  })

  it('本地校验的问题码在两语言的 workflow.designer.validate 下都有文案', () => {
    const codes = [
      'code',
      'name',
      'dup_node',
      'no_start',
      'multi_start',
      'no_end',
      'dangling_edge',
      'dead_end',
      'no_incoming',
      'decision_no_default',
      'unreachable',
    ]
    const bad: string[] = []
    for (const [locale, messages] of locales) {
      const validate = designerNode(messages, 'validate')
      for (const code of codes) {
        if (typeof validate[code] !== 'string') {
          bad.push(`${locale}: validate.${code}`)
        }
      }
    }

    expect(bad).toEqual([])
  })
})
