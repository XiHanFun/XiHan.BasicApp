<script setup lang="ts">
import type { PageResult, WorkflowTodoListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDynamicTags,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NSpace,
  useMessage,
} from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
  workflowTodoApi,
} from '@/api'
import { SchemaPage } from '~/components'

defineOptions({ name: 'WorkflowTodoPage' })

const { t } = useI18n()
const message = useMessage()

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
  scrollX: 1300,
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
    message.success(completeOutcome.value === 'approved' ? t('workflow.todo.msg_approved') : t('workflow.todo.msg_rejected'))
    completeVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.todo.err_complete'))
  }
  finally {
    completeLoading.value = false
  }
}

// ── 转办 ───────────────────────────────────────────────────────
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
    message.warning(t('workflow.todo.transfer_target_required'))
    return
  }
  transferLoading.value = true
  try {
    await workflowTodoApi.transfer({
      taskId: transferTarget.value.taskId,
      targetAssigneeId: transferTargetUser.value.trim(),
      comment: transferComment.value.trim() || undefined,
    })
    message.success(t('workflow.todo.msg_transferred'))
    transferVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.todo.err_transfer'))
  }
  finally {
    transferLoading.value = false
  }
}

// ── 加签 ───────────────────────────────────────────────────────
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
    message.warning(t('workflow.todo.add_sign_required'))
    return
  }
  addSignLoading.value = true
  try {
    await workflowTodoApi.addAssignees({
      taskId: addSignTarget.value.taskId,
      assigneeIds: addSignUsers.value,
      comment: addSignComment.value.trim() || undefined,
    })
    message.success(t('workflow.todo.msg_add_signed'))
    addSignVisible.value = false
    reload()
  }
  catch {
    message.error(t('workflow.todo.err_add_sign'))
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
    <NModal v-model:show="completeVisible" preset="card" :title="completeOutcome === 'approved' ? t('workflow.todo.approve_title') : t('workflow.todo.reject_title')" style="width: 480px">
      <NSpace vertical>
        <NInput
          v-model:value="completeComment"
          type="textarea"
          :autosize="{ minRows: 2, maxRows: 5 }"
          :placeholder="t('workflow.todo.comment_placeholder')"
        />
        <NButton block :type="completeOutcome === 'approved' ? 'success' : 'error'" :loading="completeLoading" @click="handleComplete">
          {{ completeOutcome === 'approved' ? t('workflow.todo.btn_approve') : t('workflow.todo.btn_reject') }}
        </NButton>
      </NSpace>
    </NModal>

    <!-- 转办 -->
    <NModal v-model:show="transferVisible" preset="card" :title="t('workflow.todo.transfer_title')" style="width: 480px">
      <NForm label-placement="left" :label-width="100">
        <NFormItem :label="t('workflow.todo.transfer_target')">
          <NInput v-model:value="transferTargetUser" :placeholder="t('workflow.todo.transfer_target_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('workflow.todo.comment')">
          <NInput v-model:value="transferComment" type="textarea" :autosize="{ minRows: 2, maxRows: 5 }" />
        </NFormItem>
      </NForm>
      <NButton block type="primary" :loading="transferLoading" @click="handleTransfer">
        {{ t('workflow.todo.btn_transfer') }}
      </NButton>
    </NModal>

    <!-- 加签 -->
    <NModal v-model:show="addSignVisible" preset="card" :title="t('workflow.todo.add_sign_title')" style="width: 480px">
      <NForm label-placement="left" :label-width="100">
        <NFormItem :label="t('workflow.todo.add_sign_users')">
          <NDynamicTags v-model:value="addSignUsers" />
        </NFormItem>
        <NFormItem :label="t('workflow.todo.comment')">
          <NInput v-model:value="addSignComment" type="textarea" :autosize="{ minRows: 2, maxRows: 5 }" />
        </NFormItem>
      </NForm>
      <NButton block type="primary" :loading="addSignLoading" @click="handleAddSign">
        {{ t('workflow.todo.btn_add_sign') }}
      </NButton>
    </NModal>
  </SchemaPage>
</template>
