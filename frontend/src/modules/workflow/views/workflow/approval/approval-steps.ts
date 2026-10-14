/**
 * 审批流程步骤条的步骤模型（纯函数，抽屉与测试共用）。
 * 把审批轨迹归并成步骤：每次办理、转办、加签、自动处理各占一步；同一节点的待办人合成「当前步」，
 * 同一节点连续的抄送合成一步；流程还没结束时末尾补一个「结束」占位步，结束后末步写明结论。
 */
import type { Tone } from '@xihan-ui/core'
import type { WorkflowApprovalTimelineItemDto } from '../../../api'
import { WorkflowApprovalTimelineKind, WorkflowInstanceStatus } from '../../../api'
import { instanceStatusLabelKey, outcomeLabelKey } from './approval-labels'

type Translate = (key: string, named?: Record<string, unknown>) => string

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

function outcomeText(t: Translate, outcome?: string | null) {
  if (!outcome)
    return ''
  const key = outcomeLabelKey(outcome)
  return key ? t(key) : outcome
}

function userOf(t: Translate, item: WorkflowApprovalTimelineItemDto) {
  return item.userName || item.userId || t('workflow.approval.tl_system')
}

function withComment(line: string, comment?: string | null) {
  const text = comment?.trim()
  return text ? [line, text] : [line]
}

function nodeTitle(t: Translate, item: WorkflowApprovalTimelineItemDto) {
  return item.nodeName || item.nodeId || t('workflow.approval.step_node')
}

function outcomeTone(outcome?: string | null): Tone | undefined {
  if (outcome === 'approved')
    return 'success'
  if (outcome === 'rejected')
    return 'danger'
  return undefined
}

function finishedStep(t: Translate, item: WorkflowApprovalTimelineItemDto, key: string): DraftStep {
  const status = item.instanceStatus ?? WorkflowInstanceStatus.Terminated
  if (status === WorkflowInstanceStatus.Completed) {
    return {
      key,
      title: t('workflow.approval.step_end'),
      lines: [outcomeText(t, item.outcome) || t(instanceStatusLabelKey(status))],
      time: item.time,
      tone: item.outcome === 'rejected' ? 'danger' : 'success',
    }
  }
  return {
    key,
    title: t('workflow.approval.step_end'),
    lines: [t(instanceStatusLabelKey(status))],
    time: item.time,
    tone: status === WorkflowInstanceStatus.Faulted ? 'danger' : 'neutral',
  }
}

/** 审批轨迹 → 步骤条 */
export function buildApprovalSteps(t: Translate, timeline: readonly WorkflowApprovalTimelineItemDto[]): ApprovalSteps {
  const steps: DraftStep[] = []
  let current = -1
  let finished = false

  for (const [index, item] of timeline.entries()) {
    const user = userOf(t, item)
    const targets = item.targetUserNames.join('、')
    switch (item.kind) {
      case WorkflowApprovalTimelineKind.Started:
        steps.push({ key: `started-${index}`, title: t('workflow.approval.step_started'), lines: withComment(user, item.comment), time: item.time })
        break
      case WorkflowApprovalTimelineKind.Completed:
        steps.push({
          key: `completed-${index}`,
          title: nodeTitle(t, item),
          lines: withComment(t('workflow.approval.tl_completed', { user, outcome: outcomeText(t, item.outcome) }), item.comment),
          time: item.time,
          tone: outcomeTone(item.outcome),
        })
        break
      case WorkflowApprovalTimelineKind.Transferred:
        steps.push({ key: `transferred-${index}`, title: nodeTitle(t, item), lines: withComment(t('workflow.approval.tl_transferred', { user, targets }), item.comment), time: item.time })
        break
      case WorkflowApprovalTimelineKind.AddedAssignees:
        steps.push({ key: `added-${index}`, title: nodeTitle(t, item), lines: withComment(t('workflow.approval.tl_added', { user, targets }), item.comment), time: item.time })
        break
      case WorkflowApprovalTimelineKind.AutoDecided:
        steps.push({ key: `auto-${index}`, title: nodeTitle(t, item), lines: [t('workflow.approval.tl_auto', { outcome: outcomeText(t, item.outcome) })], time: item.time, tone: 'warning' })
        break
      case WorkflowApprovalTimelineKind.CarbonCopied: {
        // 同一节点连续抄送给多人：合成一步，抄送对象逐个列出
        const group = `cc-${item.nodeId ?? ''}`
        const last = steps.at(-1)
        if (last?.group === group && last.users) {
          last.users.push(user)
          last.lines = [t('workflow.approval.tl_cc', { user: last.users.join('、') })]
          break
        }
        steps.push({ key: `${group}-${index}`, group, users: [user], title: nodeTitle(t, item), lines: [t('workflow.approval.tl_cc', { user })], time: item.time, tone: 'neutral' })
        break
      }
      case WorkflowApprovalTimelineKind.Pending: {
        // 同一节点的多位待办人（会签、或签、依次）合成当前这一步
        const group = `pending-${item.nodeId ?? ''}`
        const existing = steps.find(step => step.group === group)
        if (existing?.users) {
          existing.users.push(user)
          existing.lines = [t('workflow.approval.tl_pending', { user: existing.users.join('、') })]
          break
        }
        if (current < 0)
          current = steps.length
        steps.push({ key: group, group, users: [user], title: nodeTitle(t, item), lines: [t('workflow.approval.tl_pending', { user })], time: item.time })
        break
      }
      default:
        finished = true
        steps.push(finishedStep(t, item, `finished-${index}`))
    }
  }

  if (!finished) {
    // 流程还在进行：末尾补「结束」占位；没有待办（自动推进中、故障待处理）时当前步落在它上面
    steps.push({ key: 'end', title: t('workflow.approval.step_end'), lines: [] })
    if (current < 0)
      current = steps.length - 1
  }

  return {
    steps: steps.map(({ group: _group, users: _users, ...step }) => step),
    value: finished ? steps.length : current,
  }
}
