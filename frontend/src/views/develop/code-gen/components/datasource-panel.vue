<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  CodeGenDataSourceCreateDto,
  CodeGenDataSourceListItemDto,
  CodeGenDataSourceUpdateDto,
  DatabaseType,
} from '@/api'
import {
  NButton,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenDataSourceApi,
  createPageRequest,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  EnableStatus,
} from '@/api'
import { Icon } from '~/components'
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

const loading = ref(false)
const list = ref<CodeGenDataSourceListItemDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const queryParams = reactive({
  keyword: '',
  databaseType: null as DatabaseType | null,
  status: null as EnableStatus | null,
})

const testingId = ref<string | null>(null)

async function fetchData() {
  loading.value = true
  try {
    const result = await codeGenDataSourceApi.page({
      ...createPageRequest({ page: { pageIndex: page.value, pageSize: pageSize.value } }),
      databaseType: queryParams.databaseType ?? undefined,
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status ?? undefined,
    })
    list.value = result.items
    total.value = result.page.totalCount
  }
  catch {
    message.error(t('develop.code_gen.datasource.query_failed'))
    list.value = []
    total.value = 0
  }
  finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  fetchData()
}

function handlePageChange(value: number) {
  page.value = value
  fetchData()
}

function handlePageSizeChange(value: number) {
  pageSize.value = value
  page.value = 1
  fetchData()
}

const columns = computed<DataTableColumns<CodeGenDataSourceListItemDto>>(() => [
  {
    key: 'sourceName',
    title: t('develop.code_gen.datasource.col_source_name'),
    minWidth: 160,
    ellipsis: { tooltip: true },
    render: (row: CodeGenDataSourceListItemDto) =>
      h('div', { class: 'ds-name' }, [
        h('span', { class: 'ds-name__text' }, row.sourceName),
        row.isDefault
          ? h(NTag, { size: 'tiny', type: 'info', round: true, bordered: false }, () => t('common.statuses.default_tag'))
          : null,
      ]),
  },
  {
    key: 'databaseType',
    title: t('develop.code_gen.datasource.col_database'),
    width: 110,
    render: (row: CodeGenDataSourceListItemDto) => getOptionLabel(DATABASE_TYPE_OPTIONS, row.databaseType),
  },
  {
    key: 'host',
    title: t('develop.code_gen.datasource.col_host'),
    minWidth: 140,
    ellipsis: { tooltip: true },
    render: (row: CodeGenDataSourceListItemDto) => `${row.host}:${row.port}`,
  },
  {
    key: 'databaseName',
    title: t('develop.code_gen.datasource.col_database_name'),
    minWidth: 120,
    ellipsis: { tooltip: true },
  },
  {
    key: 'lastTestResult',
    title: t('develop.code_gen.datasource.col_connection'),
    width: 90,
    align: 'center',
    render: (row: CodeGenDataSourceListItemDto) =>
      row.lastTestTime
        ? h(NTag, {
            size: 'small',
            round: true,
            bordered: false,
            type: row.lastTestResult ? 'success' : 'error',
          }, () => (row.lastTestResult ? t('develop.code_gen.datasource.tag_normal') : t('develop.code_gen.datasource.tag_failed')))
        : h(NTag, { size: 'small', round: true, bordered: false, type: 'default' }, () => t('develop.code_gen.datasource.tag_untested')),
  },
  {
    key: 'status',
    title: t('common.fields.status'),
    width: 72,
    align: 'center',
    render: (row: CodeGenDataSourceListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: row.status === EnableStatus.Enabled ? 'success' : 'error',
      }, () => getOptionLabel(STATUS_OPTIONS, row.status)),
  },
  {
    key: 'sort',
    title: t('common.fields.sort'),
    width: 70,
    align: 'center',
  },
  {
    key: 'actions',
    title: t('common.fields.actions'),
    width: 150,
    align: 'center',
    render: (row: CodeGenDataSourceListItemDto) =>
      h(NSpace, { size: 4, justify: 'center', wrap: false }, () => [
        h(NButton, {
          ariaLabel: t('develop.code_gen.datasource.action_test'),
          circle: true,
          quaternary: true,
          size: 'small',
          type: 'info',
          loading: testingId.value === row.basicId,
          onClick: () => handleTest(row),
        }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:plug' })) }),
        h(NButton, {
          ariaLabel: t('common.actions.edit'),
          circle: true,
          quaternary: true,
          size: 'small',
          type: 'primary',
          onClick: () => handleEdit(row),
        }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () => h(NButton, {
            ariaLabel: t('common.actions.delete'),
            circle: true,
            quaternary: true,
            size: 'small',
            type: 'error',
          }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => t('develop.code_gen.datasource.confirm_delete'),
        }),
      ]),
  },
])

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
    fetchData()
  }
  catch {
    message.error(t('develop.code_gen.datasource.test_error'))
  }
  finally {
    testingId.value = null
  }
}

async function handleDelete(row: CodeGenDataSourceListItemDto) {
  try {
    await codeGenDataSourceApi.delete(row.basicId)
    message.success(t('common.messages.delete_success'))
    fetchData()
  }
  catch {
    message.error(t('common.messages.delete_failed'))
  }
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
    fetchData()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <div class="panel">
    <div class="panel__toolbar">
      <NInput
        v-model:value="queryParams.keyword"
        class="panel__kw"
        clearable
        :placeholder="t('develop.code_gen.datasource.search_placeholder')"
        size="small"
        @clear="handleSearch"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.databaseType"
        class="panel__filter"
        clearable
        :options="DATABASE_TYPE_OPTIONS"
        :placeholder="t('develop.code_gen.datasource.filter_database_type')"
        size="small"
        @update:value="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.status"
        class="panel__filter"
        clearable
        :options="STATUS_OPTIONS"
        :placeholder="t('common.fields.status')"
        size="small"
        @update:value="handleSearch"
      />
      <NButton size="small" type="primary" @click="handleSearch">
        {{ t('common.actions.search') }}
      </NButton>
      <NButton class="panel__add" size="small" type="primary" @click="handleAdd">
        <template #icon>
          <NIcon><Icon icon="lucide:plus" /></NIcon>
        </template>
        {{ t('develop.code_gen.datasource.add') }}
      </NButton>
    </div>

    <div class="panel__body">
      <NDataTable
        class="panel__table"
        flex-height
        :columns="columns"
        :data="list"
        :loading="loading"
        :row-key="(row: CodeGenDataSourceListItemDto) => row.basicId"
        :scroll-x="1100"
        size="small"
      />
    </div>

    <div class="panel__foot">
      <NPagination
        v-model:page="page"
        v-model:page-size="pageSize"
        :item-count="total"
        :page-sizes="[10, 20, 50, 100]"
        show-size-picker
        @update:page="handlePageChange"
        @update:page-size="handlePageSizeChange"
      />
    </div>

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
  </div>
</template>

<style scoped>
.panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.panel__toolbar {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding-bottom: 10px;
}

.panel__kw {
  width: 220px;
}

.panel__filter {
  width: 130px;
  flex-shrink: 0;
}

.panel__add {
  margin-left: auto;
}

.panel__body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.panel__table {
  flex: 1;
  min-height: 0;
}

.panel__foot {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
  padding-top: 10px;
}

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
