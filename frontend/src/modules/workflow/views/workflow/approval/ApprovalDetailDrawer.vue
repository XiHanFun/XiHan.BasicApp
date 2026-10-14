<script setup lang="ts">
import type { Tone } from '@xihan-ui/core'
import type { Component } from 'vue'
import type { WorkflowApprovalDetailDto } from '../../../api'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhSeparator, XhStepsDescription, XhStepsIndicator, XhStepsItem, XhStepsList, XhStepsRoot, XhStepsSeparator, XhStepsTitle, XhStepsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, defineAsyncComponent, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XInput, XJsonBlock, XPopconfirm } from '~/components'
import { toast } from '~/composables'
import { resolveView } from '~/router/dynamic'
import { formatDate } from '~/utils'
import { workflowApprovalApi, workflowTodoApi } from '../../../api'
import WorkflowUserPicker from '../shared/WorkflowUserPicker.vue'
import { handleOutcomes, instanceStatusLabelKey, instanceStatusTone, outcomeLabelKey, outcomeTone } from './approval-labels'
import { buildApprovalSteps } from './approval-steps'

/**
 * 审批详情抽屉：业务单据 + 办理区 + 审批轨迹。
 * 单据由审批业务登记的视图组件渲染（props：businessId / data / mode / nodeId）；
 * 组件若暴露 getVariables()，办理时把返回值作为附加变量一并提交（审批人补填的业务字段）。
 */
defineOptions({ name: 'ApprovalDetailDrawer' })

const props = defineProps<{
  instanceId: string | null
  taskId?: string | null
}>()

const emit = defineEmits<{
  /** 办理、转办、加签或撤回之后（列表据此刷新） */
  changed: []
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()

const loading = ref(false)
const detail = ref<WorkflowApprovalDetailDto | null>(null)
const formComponent = shallowRef<Component | null>(null)
const formRef = ref<{ getVariables?: () => Record<string, unknown> } | null>(null)
const formData = ref<unknown>(null)

const comment = ref('')
const submitting = ref(false)
const mode = ref<'none' | 'transfer' | 'addSign'>('none')
const transferTarget = ref<string | null>(null)
const addSignUsers = ref<string[]>([])
const withdrawReason = ref('')

const outcomes = computed(() => handleOutcomes(detail.value?.task?.allowedOutcomes ?? []))
/** 非审批业务流程：节点表单数据原文（空对象不展示） */
const taskFormJson = computed(() => {
  const json = detail.value?.task?.formDataJson?.trim()
  return json && json !== '{}' ? json : null
})

function outcomeText(outcome?: string | null) {
  if (!outcome)
    return ''
  const key = outcomeLabelKey(outcome)
  return key ? t(key) : outcome
}

/** 审批轨迹归并成步骤条：走过的步打勾，当前步是正在等的待办，没结束时末尾留「结束」 */
const approvalSteps = computed(() => buildApprovalSteps(t, detail.value?.timeline ?? []))
const stepTones = computed(() => {
  const tones: Record<number, Tone> = {}
  approvalSteps.value.steps.forEach((step, index) => {
    if (step.tone)
      tones[index] = step.tone
  })
  return tones
})

function resolveForm(path: string): Component | null {
  const loader = resolveView(path)
  return loader ? defineAsyncComponent(loader as () => Promise<Component>) : null
}

async function load() {
  if (!props.instanceId)
    return
  loading.value = true
  detail.value = null
  formComponent.value = null
  formData.value = null
  mode.value = 'none'
  comment.value = ''
  withdrawReason.value = ''
  try {
    const result = await workflowApprovalApi.detail(props.instanceId, props.taskId)
    detail.value = result
    if (result.business) {
      formData.value = result.business.dataJson ? JSON.parse(result.business.dataJson) as unknown : null
      formComponent.value = resolveForm(result.business.formComponent)
    }
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.approval.err_load'))
    open.value = false
  }
  finally {
    loading.value = false
  }
}

watch(() => [open.value, props.instanceId, props.taskId] as const, ([isOpen]) => {
  if (isOpen)
    void load()
}, { immediate: true })

function finish() {
  emit('changed')
  open.value = false
}

async function complete(outcome: string) {
  const task = detail.value?.task
  if (!task)
    return
  submitting.value = true
  try {
    const variables = formRef.value?.getVariables?.()
    const result = await workflowTodoApi.complete({
      taskId: task.taskId,
      outcome,
      comment: comment.value.trim() || null,
      variablesJson: variables && Object.keys(variables).length > 0 ? JSON.stringify(variables) : null,
    })
    if (result.faultMessage)
      toast.warning(t('workflow.approval.warn_faulted', { message: result.faultMessage }))
    else
      toast.success(t('workflow.approval.msg_handled', { outcome: outcomeText(outcome) }))
    finish()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.approval.err_handle'))
  }
  finally {
    submitting.value = false
  }
}

async function transfer() {
  const task = detail.value?.task
  if (!task)
    return
  if (!transferTarget.value) {
    toast.warning(t('workflow.approval.transfer_required'))
    return
  }
  submitting.value = true
  try {
    await workflowTodoApi.transfer({ taskId: task.taskId, targetAssigneeId: transferTarget.value, comment: comment.value.trim() || null })
    toast.success(t('workflow.approval.msg_transferred'))
    finish()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.approval.err_handle'))
  }
  finally {
    submitting.value = false
  }
}

async function addSign() {
  const task = detail.value?.task
  if (!task)
    return
  if (addSignUsers.value.length === 0) {
    toast.warning(t('workflow.approval.add_sign_required'))
    return
  }
  submitting.value = true
  try {
    await workflowTodoApi.addAssignees({ taskId: task.taskId, assigneeIds: addSignUsers.value, comment: comment.value.trim() || null })
    toast.success(t('workflow.approval.msg_add_signed'))
    finish()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.approval.err_handle'))
  }
  finally {
    submitting.value = false
  }
}

async function withdraw() {
  if (!detail.value)
    return
  submitting.value = true
  try {
    await workflowApprovalApi.withdraw({ instanceId: detail.value.instanceId, reason: withdrawReason.value.trim() || null })
    toast.success(t('workflow.approval.msg_withdrawn'))
    finish()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('workflow.approval.err_handle'))
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent class="approval-drawer" style="--xh-drawer-size: 760px">
      <XhDrawerTitle>{{ detail?.instanceName || t('workflow.approval.detail_title') }}</XhDrawerTitle>
      <XhDrawerCloseTrigger />

      <div v-if="loading" class="py-8 text-center text-muted-foreground">
        {{ t('workflow.approval.loading') }}
      </div>

      <div v-else-if="detail" class="flex flex-col gap-4">
        <!-- 概要：描边、标签在上，与各详情页同一种描述列表 -->
        <XhDescriptionsRoot :columns="4" variant="outline" size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('workflow.approval.col_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <span class="inline-flex items-center gap-1">
                <XhTagRoot variant="subtle" :tone="instanceStatusTone(detail.status)" size="sm">
                  <XhTagLabel>{{ t(instanceStatusLabelKey(detail.status)) }}</XhTagLabel>
                </XhTagRoot>
                <XhTagRoot v-if="detail.outcome" variant="subtle" :tone="outcomeTone(detail.outcome)" size="sm">
                  <XhTagLabel>{{ outcomeText(detail.outcome) }}</XhTagLabel>
                </XhTagRoot>
              </span>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('workflow.approval.col_business') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>{{ detail.business?.name || detail.definitionName }}</XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('workflow.approval.col_starter') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>{{ detail.starterName || detail.starterId || '-' }}</XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('workflow.approval.col_started_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>{{ formatDate(detail.startTime ?? detail.creationTime) }}</XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem v-if="detail.faultMessage" :span="4">
            <XhDescriptionsLabel>{{ t('workflow.approval.col_fault') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              <span class="text-destructive">{{ detail.faultMessage }}</span>
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>

        <!-- 业务单据 -->
        <section class="flex flex-col gap-2" :aria-label="t('workflow.approval.section_form')">
          <XhSeparator>{{ t('workflow.approval.section_form') }}</XhSeparator>
          <template v-if="detail.business">
            <component
              :is="formComponent"
              v-if="formComponent && formData !== null"
              ref="formRef"
              :business-id="detail.business.businessId"
              :data="formData"
              :mode="detail.task ? 'approve' : 'view'"
              :node-id="detail.task?.nodeId ?? null"
            />
            <p v-else-if="formData === null" class="text-sm text-muted-foreground">
              {{ t('workflow.approval.form_missing') }}
            </p>
            <template v-else>
              <p class="text-sm text-muted-foreground">
                {{ t('workflow.approval.form_component_missing', { path: detail.business.formComponent }) }}
              </p>
              <XJsonBlock :raw="detail.business.dataJson" :default-expanded-depth="2" max-height="16rem" />
            </template>
          </template>
          <template v-else>
            <p class="text-sm text-muted-foreground">
              {{ t('workflow.approval.no_business', { code: detail.definitionCode }) }}
            </p>
            <XJsonBlock v-if="taskFormJson" :raw="taskFormJson" :default-expanded-depth="2" max-height="16rem" />
          </template>
        </section>

        <!-- 办理 -->
        <section v-if="detail.task" class="flex flex-col gap-2" :aria-label="t('workflow.approval.section_handle')">
          <XhSeparator>{{ t('workflow.approval.section_handle') }} · {{ detail.task.nodeName }}</XhSeparator>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.approval.comment') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="comment"
                type="textarea"
                :autosize="{ minRows: 2, maxRows: 5 }"
                :placeholder="t('workflow.approval.comment_placeholder')"
              />
            </XhFieldControl>
          </XhFieldRoot>

          <div v-if="mode === 'transfer'" class="flex flex-col gap-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('workflow.approval.transfer_target') }}</XhFieldLabel>
              <XhFieldControl>
                <WorkflowUserPicker
                  :value="transferTarget"
                  :placeholder="t('workflow.approval.user_placeholder')"
                  @update:value="(value: string | string[] | null) => transferTarget = Array.isArray(value) ? (value[0] ?? null) : value"
                />
              </XhFieldControl>
            </XhFieldRoot>
            <div class="flex justify-end gap-2">
              <XhButton variant="subtle" size="sm" :disabled="submitting" @click="mode = 'none'">
                {{ t('workflow.approval.btn_back') }}
              </XhButton>
              <XhButton size="sm" :loading="submitting" @click="transfer">
                {{ t('workflow.approval.btn_confirm_transfer') }}
              </XhButton>
            </div>
          </div>

          <div v-else-if="mode === 'addSign'" class="flex flex-col gap-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('workflow.approval.add_sign_users') }}</XhFieldLabel>
              <XhFieldControl>
                <WorkflowUserPicker
                  multiple
                  :value="addSignUsers"
                  :placeholder="t('workflow.approval.user_placeholder')"
                  @update:value="(value: string | string[] | null) => addSignUsers = Array.isArray(value) ? value : (value ? [value] : [])"
                />
              </XhFieldControl>
            </XhFieldRoot>
            <div class="flex justify-end gap-2">
              <XhButton variant="subtle" size="sm" :disabled="submitting" @click="mode = 'none'">
                {{ t('workflow.approval.btn_back') }}
              </XhButton>
              <XhButton size="sm" :loading="submitting" @click="addSign">
                {{ t('workflow.approval.btn_confirm_add_sign') }}
              </XhButton>
            </div>
          </div>

          <div v-else class="flex flex-wrap items-center gap-2">
            <XhButton
              v-for="outcome in outcomes"
              :key="outcome"
              size="sm"
              :tone="outcome === 'approved' ? 'success' : outcome === 'rejected' ? 'danger' : 'brand'"
              :variant="outcome === 'approved' || outcome === 'rejected' ? 'solid' : 'outline'"
              :loading="submitting"
              @click="complete(outcome)"
            >
              {{ outcome === 'approved' ? t('workflow.approval.btn_approve') : outcome === 'rejected' ? t('workflow.approval.btn_reject') : outcome }}
            </XhButton>
            <span class="flex-1" />
            <XhButton variant="subtle" size="sm" :disabled="submitting" @click="mode = 'transfer'">
              {{ t('workflow.approval.btn_transfer') }}
            </XhButton>
            <XhButton variant="subtle" size="sm" :disabled="submitting" @click="mode = 'addSign'">
              {{ t('workflow.approval.btn_add_sign') }}
            </XhButton>
          </div>
        </section>

        <!-- 撤回（发起人、流程进行中） -->
        <section v-if="detail.canWithdraw" class="flex items-center gap-2">
          <XInput
            v-model:value="withdrawReason"
            class="flex-1"
            size="sm"
            :aria-label="t('workflow.approval.withdraw_reason')"
            :placeholder="t('workflow.approval.withdraw_reason')"
          />
          <XPopconfirm :title="t('workflow.approval.withdraw_confirm')" @confirm="withdraw">
            <template #trigger>
              <XhButton tone="danger" variant="outline" size="sm" :loading="submitting">
                {{ t('workflow.approval.btn_withdraw') }}
              </XhButton>
            </template>
          </XPopconfirm>
        </section>

        <!-- 审批流程：只读竖向步骤条；被驳回、出错的步用叉号，不只靠颜色 -->
        <section class="flex flex-col gap-2" :aria-label="t('workflow.approval.section_timeline')">
          <XhSeparator>{{ t('workflow.approval.section_timeline') }}</XhSeparator>
          <XhStepsRoot
            :count="approvalSteps.steps.length"
            :value="approvalSteps.value"
            :tones="stepTones"
            orientation="vertical"
            size="sm"
            read-only
            :translations="{ list: t('workflow.approval.section_timeline') }"
          >
            <XhStepsList>
              <XhStepsItem v-for="(step, index) in approvalSteps.steps" :key="step.key" :value="index">
                <XhStepsTrigger>
                  <XhStepsIndicator>
                    <Icon v-if="step.tone === 'danger'" icon="lucide:x" />
                    <template v-else-if="index >= approvalSteps.value">
                      {{ index + 1 }}
                    </template>
                  </XhStepsIndicator>
                  <XhStepsTitle>{{ step.title }}</XhStepsTitle>
                  <XhStepsDescription v-if="step.lines.length || step.time">
                    <span v-for="(line, lineIndex) in step.lines" :key="lineIndex" class="block whitespace-pre-wrap">{{ line }}</span>
                    <time v-if="step.time" class="block" :datetime="step.time">{{ formatDate(step.time) }}</time>
                  </XhStepsDescription>
                </XhStepsTrigger>
                <XhStepsSeparator />
              </XhStepsItem>
            </XhStepsList>
          </XhStepsRoot>
        </section>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>
