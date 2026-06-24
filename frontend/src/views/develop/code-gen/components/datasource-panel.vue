<script setup lang="ts">
import type {
  CodeGenDataSourceCreateDto,
  CodeGenDataSourceListItemDto,
  CodeGenDataSourceUpdateDto,
  DatabaseType,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { PageResult } from '~/types/contracts'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenDataSourceApi,
  createPageRequest,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  EnableStatus,
  querySortsFromSchema,
} from '@/api'
import { SchemaPage } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'CodeGenDatasourcePanel' })

interface DatasourceFormModel {
  basicId?: string
  sourceName: string
  sourceDescription?: string | null
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  userName: string
  password?: string | null
  connectionString?: string | null
  extraParams?: string | null
  connectionTimeout: number
  isDefault: boolean
  sort: number
  status: EnableStatus
  remark?: string | null
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const testingId = ref<string | null>(null)

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('develop.code_gen.datasource.col_source_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.code_gen.datasource.search_placeholder'), order: 0 },
  {
    key: 'sourceName',
    title: t('develop.code_gen.datasource.col_source_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
    render: (row) => {
      const r = row as unknown as CodeGenDataSourceListItemDto
      return h('div', { class: 'ds-name' }, [
        h('span', { class: 'ds-name__text' }, r.sourceName),
        r.isDefault
          ? h(NTag, { size: 'tiny', type: 'info', round: true, bordered: false }, () => t('common.statuses.default_tag'))
          : null,
      ])
    },
  },
  {
    key: 'databaseType',
    title: t('develop.code_gen.datasource.col_database'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: DATABASE_TYPE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.datasource.filter_database_type'),
    width: 110,
    order: 2,
    render: row => getOptionLabel(DATABASE_TYPE_OPTIONS, (row as unknown as CodeGenDataSourceListItemDto).databaseType),
  },
  {
    key: 'host',
    title: t('develop.code_gen.datasource.col_host'),
    dataType: 'string',
    minWidth: 140,
    sortable: true,
    order: 3,
    render: (row) => {
      const r = row as unknown as CodeGenDataSourceListItemDto
      return `${r.host}:${r.port}`
    },
  },
  { key: 'databaseName', title: t('develop.code_gen.datasource.col_database_name'), dataType: 'string', minWidth: 120, sortable: true, order: 4 },
  {
    key: 'lastTestResult',
    title: t('develop.code_gen.datasource.col_connection'),
    dataType: 'enum',
    width: 90,
    sortable: true,
    order: 5,
    render: (row) => {
      const r = row as unknown as CodeGenDataSourceListItemDto
      return r.lastTestTime
        ? h(NTag, { size: 'small', round: true, bordered: false, type: r.lastTestResult ? 'success' : 'error' }, () => (r.lastTestResult ? t('develop.code_gen.datasource.tag_normal') : t('develop.code_gen.datasource.tag_failed')))
        : h(NTag, { size: 'small', round: true, bordered: false, type: 'default' }, () => t('develop.code_gen.datasource.tag_untested'))
    },
  },
  {
    key: 'status',
    title: t('common.fields.status'),
    dataType: 'enum',
    searchable: true,
    sortable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: t('common.fields.status'),
    width: 90,
    order: 6,
    render: (row) => {
      const r = row as unknown as CodeGenDataSourceListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(STATUS_OPTIONS, r.status))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.datasource',
  pageName: t('develop.code_gen.tabs.datasource'),
  rowKey: 'basicId',
  scrollX: 1100,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenDataSourceApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts) },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        databaseType: (f.databaseType as DatabaseType | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => codeGenDataSourceApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.code_gen.datasource.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'test', title: t('develop.code_gen.datasource.action_test'), scope: 'row', type: 'info', icon: 'lucide:plug' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as CodeGenDataSourceListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'test':
      if (row) {
        void handleTest(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

async function handleTest(row: CodeGenDataSourceListItemDto) {
  testingId.value = row.basicId
  try {
    const result = await codeGenDataSourceApi.testConnection(row.basicId)
    if (result.success) {
      message.success(t('develop.code_gen.datasource.test_success', { ms: result.elapsedMilliseconds }))
    }
    else {
      message.error(result.message || t('develop.code_gen.datasource.test_failed'))
    }
    reload()
  }
  catch {
    message.error(t('develop.code_gen.datasource.test_error'))
  }
  finally {
    testingId.value = null
  }
}

function handleDelete(row: CodeGenDataSourceListItemDto) {
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.code_gen.datasource.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await codeGenDataSourceApi.delete(row.basicId)
        message.success(t('common.messages.delete_success'))
        reload()
      }
      catch {
        message.error(t('common.messages.delete_failed'))
      }
    },
  })
}

// ── 表单/弹窗 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<DatasourceFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.code_gen.datasource.modal_edit_title') : t('develop.code_gen.datasource.modal_add_title')))

function createDefaultForm(): DatasourceFormModel {
  return {
    sourceName: '',
    sourceDescription: null,
    databaseType: DatabaseTypeEnum.MySql,
    host: '',
    port: 3306,
    databaseName: '',
    userName: '',
    password: null,
    connectionString: null,
    extraParams: null,
    connectionTimeout: 30,
    isDefault: false,
    sort: 100,
    status: EnableStatus.Enabled,
    remark: null,
  }
}

function handleAdd() {
  editingStatus.value = null
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: CodeGenDataSourceListItemDto) {
  try {
    const detail = await codeGenDataSourceApi.detail(row.basicId)
    if (!detail) {
      message.error(t('develop.code_gen.datasource.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      sourceName: detail.sourceName,
      sourceDescription: detail.sourceDescription ?? null,
      databaseType: detail.databaseType,
      host: detail.host,
      port: detail.port,
      databaseName: detail.databaseName,
      userName: detail.userName,
      password: null,
      connectionString: detail.connectionString ?? null,
      extraParams: detail.extraParams ?? null,
      connectionTimeout: detail.connectionTimeout,
      isDefault: detail.isDefault,
      sort: detail.sort,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch {
    message.error(t('develop.code_gen.datasource.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.sourceName.trim()) {
    message.warning(t('develop.code_gen.datasource.validate_source_name'))
    return false
  }
  if (!form.value.host.trim()) {
    message.warning(t('develop.code_gen.datasource.validate_host'))
    return false
  }
  if (!form.value.databaseName.trim()) {
    message.warning(t('develop.code_gen.datasource.validate_database_name'))
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
    if (form.value.basicId) {
      const updateInput: CodeGenDataSourceUpdateDto = {
        basicId: form.value.basicId,
        sourceName: form.value.sourceName.trim(),
        sourceDescription: form.value.sourceDescription,
        databaseType: form.value.databaseType,
        host: form.value.host.trim(),
        port: form.value.port,
        databaseName: form.value.databaseName.trim(),
        userName: form.value.userName.trim(),
        password: form.value.password?.trim() || null,
        connectionString: form.value.connectionString,
        extraParams: form.value.extraParams,
        connectionTimeout: form.value.connectionTimeout,
        isDefault: form.value.isDefault,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await codeGenDataSourceApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await codeGenDataSourceApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.code_gen.datasource.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: CodeGenDataSourceCreateDto = {
        sourceName: form.value.sourceName.trim(),
        sourceDescription: form.value.sourceDescription,
        databaseType: form.value.databaseType,
        host: form.value.host.trim(),
        port: form.value.port,
        databaseName: form.value.databaseName.trim(),
        userName: form.value.userName.trim(),
        password: form.value.password,
        connectionString: form.value.connectionString,
        extraParams: form.value.extraParams,
        connectionTimeout: form.value.connectionTimeout,
        isDefault: form.value.isDefault,
        status: form.value.status,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await codeGenDataSourceApi.create(createInput)
    }
    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
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
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.code_gen.datasource.form_source_name')" path="sourceName">
          <NInput v-model:value="form.sourceName" clearable :placeholder="t('develop.code_gen.datasource.form_source_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_database_type')" path="databaseType">
          <NSelect v-model:value="form.databaseType" :options="DATABASE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_host')" path="host">
          <NInput v-model:value="form.host" clearable :placeholder="t('develop.code_gen.datasource.form_host_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_port')" path="port">
          <NInputNumber v-model:value="form.port" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_database_name')" path="databaseName">
          <NInput v-model:value="form.databaseName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_user_name')" path="userName">
          <NInput v-model:value="form.userName" clearable />
        </NFormItem>
        <NFormItem :label="form.basicId ? t('develop.code_gen.datasource.form_password_edit') : t('develop.code_gen.datasource.form_password')" path="password">
          <NInput v-model:value="form.password" clearable show-password-on="click" type="password" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_connection_timeout')" path="connectionTimeout">
          <NInputNumber v-model:value="form.connectionTimeout" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_sort')" path="sort">
          <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.datasource.form_is_default')" path="isDefault">
          <NSwitch v-model:value="form.isDefault" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('common.fields.status')" path="status">
          <NSelect v-model:value="form.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.datasource.form_connection_string')" path="connectionString">
          <NInput
            v-model:value="form.connectionString"
            clearable
            :placeholder="t('develop.code_gen.datasource.form_connection_string_placeholder')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.datasource.form_description')" path="sourceDescription">
          <NInput v-model:value="form.sourceDescription" clearable :rows="2" type="textarea" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.ds-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.ds-name__text {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
