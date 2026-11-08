import { describe, expect, it } from 'vitest'
import { stashApprovalIntent, takeApprovalIntent } from '@/modules/workflow/views/approvals/approval-intent'

describe('审批中心的深链意图', () => {
  it('记下的意图交给重建后的实例，只取一次', () => {
    expect(takeApprovalIntent()).toBeNull()
    stashApprovalIntent({ tab: 'todo', instanceId: 'i-1', taskId: 't-1' })
    expect(takeApprovalIntent()).toEqual({ tab: 'todo', instanceId: 'i-1', taskId: 't-1' })
    expect(takeApprovalIntent()).toBeNull()
  })

  it('后记下的覆盖先记下的', () => {
    stashApprovalIntent({ tab: 'done' })
    stashApprovalIntent({ tab: 'started' })
    expect(takeApprovalIntent()).toEqual({ tab: 'started' })
  })
})
