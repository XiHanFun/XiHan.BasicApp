/**
 * 审批中心的深链意图：选中的页签、要打开详情的流程与要办的待办
 *
 * 深链参数用完就从地址里去掉（`useTabs().consumeQuery`）；内容区按完整地址给页面做键，换了地址页面就重建，
 * 新实例读不到已经去掉的参数。意图先记在这里，由重建后的实例取走，只取一次
 */
export interface ApprovalIntent {
  /** 选中的页签，没给即不切换 */
  tab?: string
  /** 打开详情的流程实例 */
  instanceId?: string
  /** 详情里要办的待办 */
  taskId?: string | null
}

let pending: ApprovalIntent | null = null

/**
 * 记下意图，交给重建后的实例
 * @param intent 深链意图
 */
export function stashApprovalIntent(intent: ApprovalIntent): void {
  pending = intent
}

/**
 * 取走记下的意图（取过即清空）
 * @returns 记下的意图，没有为 null
 */
export function takeApprovalIntent(): ApprovalIntent | null {
  const intent = pending
  pending = null
  return intent
}
