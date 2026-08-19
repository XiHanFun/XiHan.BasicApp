<script setup lang="ts">
import type {
  CodeGenDataSourceCreateDto,
  CodeGenDataSourceListItemDto,
  CodeGenDataSourceUpdateDto,
  DatabaseType,
} from '../../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  codeGenDataSourceApi,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  EnableStatus,
} from '../../../../api'

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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

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
          ? h(XhBadge, { variant: 'subtle', size: 'sm', tone: 'info' }, () => t('common.statuses.default_tag'))
          : null,
      ])
    },
  },
  {
    key: 'databaseType',
    title: t('develop.code_gen.datasource.col_database'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
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
        ? h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.lastTestResult ? 'success' : 'danger' }, () => (r.lastTestResult ? t('develop.code_gen.datasource.tag_normal') : t('develop.code_gen.datasource.tag_failed')))
        : h(XhBadge, { variant: 'subtle', size: 'sm', tone: 'neutral' }, () => t('develop.code_gen.datasource.tag_untested'))
    },
  },
  {
    key: 'status',
    title: t('common.fields.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: STATUS_OPTIONS,
    searchPlaceholder: t('common.fields.status'),
    width: 90,
    order: 6,
    render: (row) => {
      const r = row as unknown as CodeGenDataSourceListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => getOptionLabel(statusEnumOptions.value, r.status))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.datasource',
  pageName: t('develop.code_gen.tabs.datasource'),
  rowKey: 'basicId',
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenDataSourceApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(databaseType/status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        // databaseType/status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
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
      toast.success(t('develop.code_gen.datasource.test_success', { ms: result.elapsedMilliseconds }))
    }
    else {
      toast.error(result.message || t('develop.code_gen.datasource.test_failed'))
    }
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.datasource.test_error'))
  }
  finally {
    testingId.value = null
  }
}

function handleDelete(row: CodeGenDataSourceListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.code_gen.datasource.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await codeGenDataSourceApi.delete(row.basicId)
        toast.success(t('common.messages.delete_success'))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('common.messages.delete_failed'))
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
      toast.error(t('develop.code_gen.datasource.not_found'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.datasource.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.sourceName.trim()) {
    toast.warning(t('develop.code_gen.datasource.validate_source_name'))
    return false
  }
  if (!form.value.host.trim()) {
    toast.warning(t('develop.code_gen.datasource.validate_host'))
    return false
  }
  if (!form.value.databaseName.trim()) {
    toast.warning(t('develop.code_gen.datasource.validate_database_name'))
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
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="form"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="sourceName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_source_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.sourceName" clearable :placeholder="t('develop.code_gen.datasource.form_source_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="databaseType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_database_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.databaseType" :options="DATABASE_TYPE_OPTIONS" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="host">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_host') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.host" clearable :placeholder="t('develop.code_gen.datasource.form_host_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="port">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_port') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.port" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="databaseName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_database_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.databaseName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="userName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_user_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.userName" clearable autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="password">
          <XhFieldRoot>
            <XhFieldLabel>{{ form.basicId ? t('develop.code_gen.datasource.form_password_edit') : t('develop.code_gen.datasource.form_password') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.password" clearable type="password" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="connectionTimeout">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_connection_timeout') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.connectionTimeout" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isDefault">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_is_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isDefault" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!form.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('common.fields.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="connectionString" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_connection_string') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.connectionString"
                clearable
                :placeholder="t('develop.code_gen.datasource.form_connection_string_placeholder')"
                :rows="2"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sourceDescription" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.code_gen.datasource.form_description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.sourceDescription" clearable :rows="2" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
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
