<script setup lang="ts">
import type {
  WorkflowTodoListItemDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage, XEditModal, XInput, XTagsInput } from '~/components'
import { toast } from '~/composables'
import {
  workflowTodoApi,
} from '../../../api'

defineOptions({ name: 'WorkflowTodoPage' })

const { t } = useI18n()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('workflow.todo.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('workflow.todo.keyword_placeholder'), width: 240, order: 0 },
  { key: 'title', title: t('workflow.todo.title'), dataType: 'string', minWidth: 220, order: 10 },
  { key: 'instanceName', title: t('workflow.todo.instance_name'), dataType: 'string', minWidth: 180, order: 11 },
  { key: 'definitionCode', title: t('workflow.todo.definition_code'), dataType: 'string', minWidth: 140, order: 12 },
  { key: 'nodeId', title: t('workflow.todo.node'), dataType: 'string', width: 130, order: 13 },
  { key: 'correlationId', title: t('workflow.todo.correlation_id'), dataType: 'string', minWidth: 140, order: 14 },
  { key: 'creationTime', title: t('workflow.todo.creation_time'), dataType: 'datetime', minWidth: 170, order: 15 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'workflow.todo',
  pageName: t('workflow.todo.page_name'),
  rowKey: 'taskId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return workflowTodoApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'approve', title: t('workflow.todo.action_approve'), scope: 'row', type: 'success', icon: 'lucide:check' },
    { key: 'reject', title: t('workflow.todo.action_reject'), scope: 'row', type: 'error', icon: 'lucide:x' },
    { key: 'transfer', title: t('workflow.todo.action_transfer'), scope: 'row' },
    { key: 'addSign', title: t('workflow.todo.action_add_sign'), scope: 'row' },
  ],
}))

// ── 办理（同意/拒绝） ──────────────────────────────────────────
/** 弹窗底部的确认钮靠这个 id 关联到表单，点它走表单提交 */
const completeFormId = useId()
const completeVisible = ref(false)
const completeLoading = ref(false)
const completeOutcome = ref<'approved' | 'rejected'>('approved')
const completeComment = ref('')
const completeTarget = ref<WorkflowTodoListItemDto | null>(null)

function openComplete(outcome: 'approved' | 'rejected', row: WorkflowTodoListItemDto) {
  completeOutcome.value = outcome
  completeComment.value = ''
  completeTarget.value = row
  completeVisible.value = true
}

async function handleComplete() {
  if (!completeTarget.value)
    return
  completeLoading.value = true
  try {
    await workflowTodoApi.complete({
      taskId: completeTarget.value.taskId,
      outcome: completeOutcome.value,
      comment: completeComment.value.trim() || undefined,
    })
    toast.success(completeOutcome.value === 'approved' ? t('workflow.todo.msg_approved') : t('workflow.todo.msg_rejected'))
    completeVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.todo.err_complete'))
  }
  finally {
    completeLoading.value = false
  }
}

// ── 转办 ───────────────────────────────────────────────────────
/** 弹窗底部的确认钮靠这个 id 关联到表单，点它走表单提交 */
const transferFormId = useId()
const transferVisible = ref(false)
const transferLoading = ref(false)
const transferTargetUser = ref('')
const transferComment = ref('')
const transferTarget = ref<WorkflowTodoListItemDto | null>(null)

function openTransfer(row: WorkflowTodoListItemDto) {
  transferTargetUser.value = ''
  transferComment.value = ''
  transferTarget.value = row
  transferVisible.value = true
}

async function handleTransfer() {
  if (!transferTarget.value)
    return
  if (!transferTargetUser.value.trim()) {
    toast.warning(t('workflow.todo.transfer_target_required'))
    return
  }
  transferLoading.value = true
  try {
    await workflowTodoApi.transfer({
      taskId: transferTarget.value.taskId,
      targetAssigneeId: transferTargetUser.value.trim(),
      comment: transferComment.value.trim() || undefined,
    })
    toast.success(t('workflow.todo.msg_transferred'))
    transferVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.todo.err_transfer'))
  }
  finally {
    transferLoading.value = false
  }
}

// ── 加签 ───────────────────────────────────────────────────────
/** 弹窗底部的确认钮靠这个 id 关联到表单，点它走表单提交 */
const addSignFormId = useId()
const addSignVisible = ref(false)
const addSignLoading = ref(false)
const addSignUsers = ref<string[]>([])
const addSignComment = ref('')
const addSignTarget = ref<WorkflowTodoListItemDto | null>(null)

function openAddSign(row: WorkflowTodoListItemDto) {
  addSignUsers.value = []
  addSignComment.value = ''
  addSignTarget.value = row
  addSignVisible.value = true
}

async function handleAddSign() {
  if (!addSignTarget.value)
    return
  if (addSignUsers.value.length === 0) {
    toast.warning(t('workflow.todo.add_sign_required'))
    return
  }
  addSignLoading.value = true
  try {
    await workflowTodoApi.addAssignees({
      taskId: addSignTarget.value.taskId,
      assigneeIds: addSignUsers.value,
      comment: addSignComment.value.trim() || undefined,
    })
    toast.success(t('workflow.todo.msg_add_signed'))
    addSignVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('workflow.todo.err_add_sign'))
  }
  finally {
    addSignLoading.value = false
  }
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as WorkflowTodoListItemDto | undefined
  if (!row)
    return
  switch (payload.key) {
    case 'approve':
      openComplete('approved', row)
      break
    case 'reject':
      openComplete('rejected', row)
      break
    case 'transfer':
      openTransfer(row)
      break
    case 'addSign':
      openAddSign(row)
      break
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 办理（同意/拒绝） -->
    <XEditModal
      v-model:show="completeVisible"
      :title="completeOutcome === 'approved' ? t('workflow.todo.approve_title') : t('workflow.todo.reject_title')"
      :width="480"
      :loading="completeLoading"
      :save-text="completeOutcome === 'approved' ? t('workflow.todo.btn_approve') : t('workflow.todo.btn_reject')"
      :save-tone="completeOutcome === 'approved' ? 'success' : 'danger'"
      :form-id="completeFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="completeFormId"
        class="xh-edit-form-grid"
        @submit="handleComplete"
      >
        <XhFormFieldGroup value="comment" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.todo.comment') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="completeComment"
                type="textarea"
                :autosize="{ minRows: 2, maxRows: 5 }"
                :placeholder="t('workflow.todo.comment_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <!-- 转办 -->
    <XEditModal
      v-model:show="transferVisible"
      :title="t('workflow.todo.transfer_title')"
      :width="480"
      :loading="transferLoading"
      :save-text="t('workflow.todo.btn_transfer')"
      :form-id="transferFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="transferFormId"
        class="xh-edit-form-grid"
        @submit="handleTransfer"
      >
        <XhFormFieldGroup value="targetAssigneeId" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.todo.transfer_target') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="transferTargetUser" :placeholder="t('workflow.todo.transfer_target_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="comment" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.todo.comment') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="transferComment" type="textarea" :autosize="{ minRows: 2, maxRows: 5 }" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>

    <!-- 加签 -->
    <XEditModal
      v-model:show="addSignVisible"
      :title="t('workflow.todo.add_sign_title')"
      :width="480"
      :loading="addSignLoading"
      :save-text="t('workflow.todo.btn_add_sign')"
      :form-id="addSignFormId"
    >
      <!-- 必填由提交处理器判定；这里不配 rules，错误文本槽位留着备用 -->
      <XhFormRoot
        :id="addSignFormId"
        class="xh-edit-form-grid"
        @submit="handleAddSign"
      >
        <XhFormFieldGroup value="assigneeIds" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.todo.add_sign_users') }}</XhFieldLabel>
            <XhFieldControl>
              <XTagsInput v-model:value="addSignUsers" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="comment" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('workflow.todo.comment') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="addSignComment" type="textarea" :autosize="{ minRows: 2, maxRows: 5 }" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
