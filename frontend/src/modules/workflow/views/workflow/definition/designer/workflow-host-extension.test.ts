/**
 * 流程定义设计器挂进菜单栏的宿主操作（workflow-host-extension.ts）的单元测试。
 *
 * 锁定：引擎定义 JSON 与返回列表进「文件」菜单，返回列表也上工具栏；保存中不许返回列表；执行时交给宿主。
 */
import { describe, expect, it, vi } from 'vitest'
import { createWorkflowHostExtension, WORKFLOW_HOST_COMMANDS } from './workflow-host-extension'

function setup(saving = false) {
  const actions = { openEngineJson: vi.fn(), backToList: vi.fn() }
  const extension = createWorkflowHostExtension({ t: key => key, saving: () => saving, ...actions })
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
  it('引擎定义 JSON 与返回列表进文件菜单，返回列表也上工具栏', () => {
    const { extension } = setup()

    expect(extension.commands?.map(item => [item.id, item.placement])).toEqual([
      [WORKFLOW_HOST_COMMANDS.engineJson, ['menubar:file']],
      [WORKFLOW_HOST_COMMANDS.backToList, ['menubar:file', 'toolbar']],
    ])
  })

  it('命令 id 以扩展 id 开头，文案取流程设计器的文案键，每条都带图标', () => {
    const { extension, label } = setup()

    expect(extension.commands?.every(item => item.id.startsWith(`${extension.id}.`) && item.icon !== undefined)).toBe(true)
    expect(label(WORKFLOW_HOST_COMMANDS.engineJson)).toBe('workflow.designer.engine_json')
    expect(label(WORKFLOW_HOST_COMMANDS.backToList)).toBe('workflow.designer.back_to_list')
  })
})

describe('可用与执行', () => {
  it('保存中不许返回列表，引擎定义 JSON 照常可用', () => {
    const { command } = setup(true)

    expect(command(WORKFLOW_HOST_COMMANDS.backToList).enabled?.({} as never)).toBe(false)
    expect(command(WORKFLOW_HOST_COMMANDS.engineJson).enabled?.({} as never) ?? true).toBe(true)
  })

  it('执行时交给宿主', () => {
    const { command, actions } = setup()

    command(WORKFLOW_HOST_COMMANDS.engineJson).run?.({} as never)
    command(WORKFLOW_HOST_COMMANDS.backToList).run?.({} as never)

    expect(actions.openEngineJson).toHaveBeenCalledTimes(1)
    expect(actions.backToList).toHaveBeenCalledTimes(1)
  })
})
