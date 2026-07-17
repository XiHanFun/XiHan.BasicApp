<script setup lang="ts">
import type { SelectOption } from 'naive-ui'
import type {
  PositionCreateDto,
  PositionDetailDto,
  PositionListItemDto,
  PositionUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  positionApi,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XEditModal } from '~/components'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'IdentityPositionPage' })

interface PositionFormModel {
  basicId?: string
  positionCode: string
  positionName: string
  remark?: string | null
  sort: number
  status: EnableStatus
}

const { t } = useI18n()
const message = useMessage()
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadPage() {
  void schemaPageRef.value?.reload()
}

// ── 字段单一事实源（列 + searchable；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.position.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.position.keyword_placeholder'), width: 250, order: 0 },
  { key: 'positionName', title: t('identity.position.position_name'), dataType: 'string', sortable: true, importable: true, required: true, minWidth: 180, order: 1 },
  { key: 'positionCode', title: t('identity.position.position_code'), dataType: 'string', sortable: true, importable: true, required: true, minWidth: 160, order: 2 },
  // enum + options 由框架自动渲染为 NTag，无需自定义 render
  { key: 'status', title: t('identity.position.status'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, importable: true, dictionaryCode: 'EnableStatus', options: statusOptions.value, searchPlaceholder: t('identity.position.status_placeholder'), width: 100, order: 3 },
  { key: 'sort', title: t('identity.position.sort'), dataType: 'number', sortable: true, importable: true, width: 90, order: 4 },
  { key: 'createdTime', title: t('identity.position.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 5 },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──
const schema = computed<PageSchema>(() => ({
  pageCode: 'identity.position',
  exportPermission: 'saas:position:export',
  pageName: t('identity.position.page_name'),
  batchRemovable: true,
  removePermission: 'saas:position:delete',
  statusPermission: 'saas:position:status',
  rowKey: 'basicId',
  scrollX: 980,
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword } = params.filters
      return positionApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 状态多选等通用过滤统一走 conditions（多选经 filters In 下发）
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    remove: id => positionApi.delete(id),
    updateStatus: (id, enabled) => positionApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('identity.position.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('identity.position.view'), scope: 'row' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row' },
    { key: 'toggle', title: t('identity.position.toggle'), scope: 'row' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row' },
  ],
}))

// ── 行/页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as PositionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ── 弹窗/表单 ──
const modalVisible = ref(false)
const submitLoading = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<PositionDetailDto | null>(null)
const positionForm = ref<PositionFormModel>(createDefaultForm())

const modalTitle = computed(() => (positionForm.value.basicId ? t('identity.position.edit_title') : t('identity.position.add_title')))

function createDefaultForm(): PositionFormModel {
  return {
    positionCode: '',
    positionName: '',
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function toStr(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions.value, value)
}

function handleAdd() {
  positionForm.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: PositionListItemDto) {
  // 列表项不含备注，编辑前取详情回填，避免保存时把备注置空
  let detail: PositionDetailDto | null = null
  try {
    detail = await positionApi.detail(row.basicId)
  }
  catch {
    message.error(t('identity.position.load_detail_failed'))
  }
  positionForm.value = {
    basicId: row.basicId,
    positionCode: row.positionCode,
    positionName: row.positionName,
    remark: detail?.remark ?? null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleView(row: PositionListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await positionApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('identity.position.detail_not_found'))
    }
  }
  catch {
    message.error(t('identity.position.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function handleDelete(row: PositionListItemDto) {
  await positionApi.delete(row.basicId)
  message.success(t('common.messages.delete_success'))
  reloadPage()
}

async function handleToggleStatus(row: PositionListItemDto) {
  const nextStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await positionApi.updateStatus({ basicId: row.basicId, status: nextStatus })
    message.success(t('common.messages.status_updated'))
    reloadPage()
  }
  catch {
    message.error(t('common.messages.status_failed'))
  }
}

function validateForm() {
  if (!positionForm.value.positionName.trim()) {
    message.warning(t('identity.position.validate_position_name'))
    return false
  }
  if (!positionForm.value.basicId && !positionForm.value.positionCode.trim()) {
    message.warning(t('identity.position.validate_position_code'))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (positionForm.value.basicId) {
      const updateInput: PositionUpdateDto = {
        basicId: positionForm.value.basicId,
        positionName: positionForm.value.positionName.trim(),
        remark: toStr(positionForm.value.remark),
        sort: positionForm.value.sort,
      }
      await positionApi.update(updateInput)
    }
    else {
      const createInput: PositionCreateDto = {
        positionCode: positionForm.value.positionCode.trim(),
        positionName: positionForm.value.positionName.trim(),
        remark: toStr(positionForm.value.remark),
        sort: positionForm.value.sort,
        status: positionForm.value.status,
      }
      await positionApi.create(createInput)
    }

    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadPage()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NModal
      v-model:show="detailVisible"
      class="xh-mgmt-detail-modal"
      preset="card"
      :bordered="false"
      :mask-closable="true"
      style="width: 640px; max-width: calc(100vw - 32px);"
    >
      <template v-if="currentDetail" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="lucide:briefcase" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ currentDetail.positionName }}
            </div>
            <div class="det-hd-sub">
              {{ currentDetail.positionCode }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        {{ t('common.statuses.loading') }}
      </div>
      <NDescriptions v-else-if="currentDetail" :column="2" bordered size="small">
        <NDescriptionsItem :label="t('identity.position.position_code')">
          {{ currentDetail.positionCode }}
        </NDescriptionsItem>
        <NDescriptionsItem :label="t('identity.position.status')">
          <NTag size="small" :type="currentDetail.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
            {{ formatStatus(currentDetail.status) }}
          </NTag>
        </NDescriptionsItem>
        <NDescriptionsItem :label="t('identity.position.sort')">
          {{ currentDetail.sort }}
        </NDescriptionsItem>
        <NDescriptionsItem :label="t('identity.position.created_time')">
          {{ formatNullableDate(currentDetail.createdTime) }}
        </NDescriptionsItem>
        <NDescriptionsItem :label="t('identity.position.remark')" :span="2">
          {{ formatNullable(currentDetail.remark) }}
        </NDescriptionsItem>
      </NDescriptions>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            {{ t('common.actions.close') }}
          </NButton>
          <NButton
            v-if="currentDetail"
            size="small"
            type="primary"
            @click="detailVisible = false; handleEdit(currentDetail as PositionListItemDto)"
          >
            {{ t('common.actions.edit') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      @save="handleSubmit"
    >
      <NForm :model="positionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('identity.position.position_name')" path="positionName">
          <NInput v-model:value="positionForm.positionName" clearable :placeholder="t('identity.position.ph_position_name')" />
        </NFormItem>
        <NFormItem :label="t('identity.position.position_code')" path="positionCode">
          <NInput
            v-model:value="positionForm.positionCode"
            :disabled="Boolean(positionForm.basicId)"
            clearable
            :placeholder="t('identity.position.ph_position_code')"
          />
        </NFormItem>
        <NFormItem :label="t('identity.position.sort')" path="sort">
          <NInputNumber v-model:value="positionForm.sort" :min="0" />
        </NFormItem>
        <NFormItem v-if="!positionForm.basicId" :label="t('identity.position.status')" path="status">
          <NSelect v-model:value="positionForm.status" :options="(statusOptions as SelectOption[])" />
        </NFormItem>
        <NFormItem :label="t('identity.position.remark')" path="remark" class="xh-span-2">
          <NInput v-model:value="positionForm.remark" clearable :rows="3" type="textarea" :placeholder="t('identity.position.ph_remark')" />
        </NFormItem>
      </NForm>
    </XEditModal>
  </SchemaPage>
</template>
