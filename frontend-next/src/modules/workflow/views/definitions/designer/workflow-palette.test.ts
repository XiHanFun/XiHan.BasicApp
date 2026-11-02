/**
 * 设计器物料收窄（workflow-palette.ts）的单元测试。
 *
 * 锁定：框架 5 没有的 Script 恒不放；后端活动类型目录到了只留目录里有的；预设别的部分原样。
 */
import { describe, expect, it } from 'vitest'
import { createXihanWorkflow } from '~/designers/flow'
import { paletteCategoryNames, restrictActivityTypes } from './workflow-palette'

const preset = createXihanWorkflow({ legacyAssignees: false })
const typesOf = (extension: ReturnType<typeof createXihanWorkflow>) => extension.nodeTypes?.map(nodeType => nodeType.type) ?? []

describe('restrictActivityTypes', () => {
  it('没有目录时只去掉 Script，别的节点类型与预设的其余部分原样', () => {
    const restricted = restrictActivityTypes(preset)

    expect(typesOf(restricted)).not.toContain('Script')
    expect(typesOf(restricted)).toEqual(typesOf(preset).filter(type => type !== 'Script'))
    expect(restricted.id).toBe(preset.id)
    expect(restricted.rules).toBe(preset.rules)
    expect(restricted.documentProperties).toBe(preset.documentProperties)
  })

  it('有目录时只留目录里有的；目录里就算有 Script 也不放', () => {
    const restricted = restrictActivityTypes(preset, new Set(['Start', 'End', 'UserTask', 'Script', 'EventGateway']))

    expect(typesOf(restricted)).toEqual(['Start', 'End', 'UserTask'])
  })
})

describe('paletteCategoryNames', () => {
  it('分类标识小写后对上预设的分组，没有展示名的不给', () => {
    expect(paletteCategoryNames([
      { category: 'Control', categoryName: '流程控制' },
      { category: 'Human', categoryName: '人工' },
      { category: 'Control', categoryName: '流程控制' },
      { category: 'Data', categoryName: null },
      { category: null, categoryName: '无分类' },
    ])).toEqual({ control: '流程控制', human: '人工' })
  })
})
