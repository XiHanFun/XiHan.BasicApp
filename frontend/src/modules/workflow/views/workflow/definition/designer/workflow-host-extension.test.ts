/**
 * 流程定义设计器挂进菜单栏的宿主操作（workflow-host-extension.ts）的单元测试。
 *
 * 锁定：只有引擎定义 JSON 一条，进「文件」菜单，只在开发者模式开着时有，执行时交给宿主；
 * 回列表走设计器自带的关闭钮（closable），不再有「返回列表」宿主命令。
 */
import { describe, expect, it, vi } from 'vitest'
import { createWorkflowHostExtension, WORKFLOW_HOST_COMMANDS } from './workflow-host-extension'

function setup() {
  const actions = { openEngineJson: vi.fn() }
  const extension = createWorkflowHostExtension({ t: key => key, ...actions })
  const command = (id: string) => {
    const found = extension.commands?.find(item => item.id === id)
    if (!found)
      throw new Error(`没有命令 ${id}`)
    return found
  }
  const label = (id: string) => {
    const value = command(id).label
    return typeof value === 'function' ? value({} as never) : value
  }
  return { actions, extension, command, label }
}

describe('落点与文案', () => {
  it('只有引擎定义 JSON，进文件菜单；没有返回列表', () => {
    const { extension } = setup()

    expect(extension.commands?.map(item => [item.id, item.placement])).toEqual([
      [WORKFLOW_HOST_COMMANDS.engineJson, ['menubar:file']],
    ])
  })

  it('命令 id 以扩展 id 开头，文案取流程设计器的文案键，带图标', () => {
    const { extension, label } = setup()

    expect(extension.commands?.every(item => item.id.startsWith(`${extension.id}.`) && item.icon !== undefined)).toBe(true)
    expect(label(WORKFLOW_HOST_COMMANDS.engineJson)).toBe('workflow.designer.engine_json')
  })
})

describe('开发者模式', () => {
  it('关着时没有引擎定义 JSON，与设计器自带的「编辑 JSON」同一口径；开着时才有', () => {
    const { command } = setup()
    const visible = (settings: Record<string, unknown>) => command(WORKFLOW_HOST_COMMANDS.engineJson).visible?.({ settings } as never) ?? true

    expect(visible({})).toBe(false)
    expect(visible({ 'general.developer': false })).toBe(false)
    expect(visible({ 'general.developer': true })).toBe(true)
  })
})

describe('执行', () => {
  it('交给宿主', () => {
    const { command, actions } = setup()

    command(WORKFLOW_HOST_COMMANDS.engineJson).run?.({} as never)

    expect(actions.openEngineJson).toHaveBeenCalledTimes(1)
  })
})
