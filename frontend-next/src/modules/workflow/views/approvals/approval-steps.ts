/**
 * 审批流程步骤条的步骤模型（纯函数，抽屉与测试共用；照 4.x 的 approval-steps）。
 * 把审批轨迹归并成步骤：每次办理、转办、加签、自动处理各占一步；同一节点的待办人合成「当前步」，
 * 同一节点连续的抄送合成一步；流程还没结束时末尾补一个「结束」占位步，结束后末步写明结论。
 */
import type { Tone } from '@xihan-ui/core'
import type { Translate } from './approval-labels'
import type { ResponseOf } from '~/contracts'
import { instanceStatusLabel, outcomeLabel } from './approval-labels'

/** 审批轨迹的一项（approvals.get 的 timeline） */
export type ApprovalTimelineItem = ResponseOf<'approvals.get'>['timeline'][number]

export interface ApprovalStep {
  key: string
  title: string
  /** 说明行（办理人与结果、意见等，逐行展示） */
  lines: string[]
  time?: string | null
  /** 语气：通过绿、驳回与故障红、自动处理黄、抄送与撤回中性；未给时跟随步骤条的品牌色 */
  tone?: Tone
}

export interface ApprovalSteps {
  steps: ApprovalStep[]
  /** 当前步下标：有待办时是待办那一步；流程结束时等于步数（全部走完）；否则落在末尾的「结束」占位步 */
  value: number
}

/** 归并过程中的步骤：group 标出可合并的同类步，users 收集合并进来的人 */
interface DraftStep extends ApprovalStep {
  group?: string
  users?: string[]
}

function userOf(t: Translate, item: ApprovalTimelineItem) {
  return item.userName || item.userId || t('workflow.approvals.tl_system')
}

function withComment(line: string, comment?: string | null) {
  const text = comment?.trim()
  return text ? [line, text] : [line]
}

function nodeTitle(t: Translate, item: ApprovalTimelineItem) {
  return item.nodeName || item.nodeId || t('workflow.approvals.step_node')
}

function outcomeTone(outcome?: string | null): Tone | undefined {
  if (outcome === 'approved')
    return 'success'
  if (outcome === 'rejected')
    return 'danger'
  return undefined
}

function finishedStep(t: Translate, item: ApprovalTimelineItem, key: string): DraftStep {
  const status = item.instanceStatus ?? 'Terminated'
  if (status === 'Completed') {
    return {
      key,
      title: t('workflow.approvals.step_end'),
      lines: [outcomeLabel(t, item.outcome) || instanceStatusLabel(t, status)],
      time: item.time,
      tone: item.outcome === 'rejected' ? 'danger' : 'success',
    }
  }
  return {
    key,
    title: t('workflow.approvals.step_end'),
    lines: [instanceStatusLabel(t, status)],
    time: item.time,
    tone: status === 'Faulted' ? 'danger' : 'neutral',
  }
}

/**
 * 审批轨迹 → 步骤条。
 * @param t 文案函数。
 * @param timeline 审批轨迹（按时间）。
 */
export function buildApprovalSteps(t: Translate, timeline: readonly ApprovalTimelineItem[]): ApprovalSteps {
  const steps: DraftStep[] = []
  let current = -1
  let finished = false

  for (const [index, item] of timeline.entries()) {
    const user = userOf(t, item)
    const targets = item.targetUserNames.join('、')
    switch (item.kind) {
      case 'Started':
        steps.push({ key: `started-${index}`, title: t('workflow.approvals.step_started'), lines: withComment(user, item.comment), time: item.time })
        break
      case 'Completed':
        steps.push({
          key: `completed-${index}`,
          title: nodeTitle(t, item),
          lines: withComment(t('workflow.approvals.tl_completed', { user, outcome: outcomeLabel(t, item.outcome) }), item.comment),
          time: item.time,
          tone: outcomeTone(item.outcome),
        })
        break
      case 'Transferred':
        steps.push({ key: `transferred-${index}`, title: nodeTitle(t, item), lines: withComment(t('workflow.approvals.tl_transferred', { user, targets }), item.comment), time: item.time })
        break
      case 'AddedAssignees':
        steps.push({ key: `added-${index}`, title: nodeTitle(t, item), lines: withComment(t('workflow.approvals.tl_added', { user, targets }), item.comment), time: item.time })
        break
      case 'AutoDecided':
        steps.push({ key: `auto-${index}`, title: nodeTitle(t, item), lines: [t('workflow.approvals.tl_auto', { outcome: outcomeLabel(t, item.outcome) })], time: item.time, tone: 'warning' })
        break
      case 'CarbonCopied': {
        // 同一节点连续抄送给多人：合成一步，抄送对象逐个列出
        const group = `cc-${item.nodeId ?? ''}`
        const last = steps.at(-1)
        if (last?.group === group && last.users) {
          last.users.push(user)
          last.lines = [t('workflow.approvals.tl_cc', { user: last.users.join('、') })]
          break
        }
        steps.push({ key: `${group}-${index}`, group, users: [user], title: nodeTitle(t, item), lines: [t('workflow.approvals.tl_cc', { user })], time: item.time, tone: 'neutral' })
        break
      }
      case 'Pending': {
        // 同一节点的多位待办人（会签、或签、依次）合成当前这一步
        const group = `pending-${item.nodeId ?? ''}`
        const existing = steps.find(step => step.group === group)
        if (existing?.users) {
          existing.users.push(user)
          existing.lines = [t('workflow.approvals.tl_pending', { user: existing.users.join('、') })]
          break
        }
        if (current < 0)
          current = steps.length
        steps.push({ key: group, group, users: [user], title: nodeTitle(t, item), lines: [t('workflow.approvals.tl_pending', { user })], time: item.time })
        break
      }
      default:
        finished = true
        steps.push(finishedStep(t, item, `finished-${index}`))
    }
  }

  if (!finished) {
    // 流程还在进行：末尾补「结束」占位；没有待办（自动推进中、故障待处理）时当前步落在它上面
    steps.push({ key: 'end', title: t('workflow.approvals.step_end'), lines: [] })
    if (current < 0)
      current = steps.length - 1
  }

  return {
    steps: steps.map(({ group: _group, users: _users, ...step }) => step),
    value: finished ? steps.length : current,
  }
}
