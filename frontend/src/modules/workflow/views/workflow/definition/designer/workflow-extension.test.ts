/**
 * 流程设计器上 BasicApp 的设计器扩展（workflow-extension.ts）的单元测试。
 *
 * 锁定：流程的编码、名称、分类、描述在设计器里只读（在流程定义列表的「编辑」里改），属性面板只显示当前值、不给改；
 * 编码一行另带关联审批业务的提示。
 */
import type { VNode } from 'vue'
import type { WorkflowBusinessTypeDto } from '../../../../api'
import { describe, expect, it, vi } from 'vitest'
import { createWorkflowExtension } from './workflow-extension'

vi.mock('~/designers', () => ({
  createVuePropertyEditor: (_appContext: unknown, render: (context: unknown) => VNode) => ({ render }),
}))
vi.mock('~/designers/flow', () => ({ XIHAN_WORKFLOW_ASSIGNEE_RULES: 'xihan-workflow:assignee-rules' }))

const leave = { code: 'leave-request', name: '请假审批', variables: [], outcomes: ['approved', 'rejected'] } as unknown as WorkflowBusinessTypeDto

function setup() {
  const extension = createWorkflowExtension({
    appContext: null,
    businessTypes: () => [leave],
    document: () => undefined,
    t: key => key,
  })
  const document = extension.editors?.byTarget?.document as Record<string, { render: (context: unknown) => VNode }> | undefined
  const render = (path: string, value: unknown) => document?.[path]?.render({ value, descriptor: { label: path }, readonly: false, setValue: vi.fn(), commit: vi.fn() })
  return { document, render }
}

describe('流程元数据在设计器里只读', () => {
  it('编码、名称、分类、描述都由扩展接管，只显示当前值', () => {
    const { document, render } = setup()

    expect(Object.keys(document ?? {})).toEqual(expect.arrayContaining(['data.code', 'data.name', 'data.category', 'data.description']))
    for (const [path, value] of [['data.code', 'leave-request'], ['data.name', '请假审批'], ['data.category', '人事'], ['data.description', '年假']] as const) {
      const props = render(path, value)?.props ?? {}
      expect(props.value).toBe(value)
      expect(Object.keys(props).some(key => key.startsWith('onUpdate'))).toBe(false)
    }
  })

  it('描述按多行显示；编码一行带关联业务与去列表「编辑」里改的提示', () => {
    const { render } = setup()

    expect(render('data.description', '年假')?.props?.multiline).toBe(true)
    expect(render('data.code', 'leave-request')?.props?.hints).toEqual([
      'workflow.designer.flow_business_tip',
      'workflow.designer.metadata_readonly_tip',
    ])
    expect(render('data.code', 'custom')?.props?.hints).toEqual(['workflow.designer.metadata_readonly_tip'])
  })
})
