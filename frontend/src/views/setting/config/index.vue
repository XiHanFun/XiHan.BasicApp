<script setup lang="ts">
import type {
  ConfigCreateDto,
  ConfigDetailDto,
  ConfigListItemDto,
  ConfigUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NConfigProvider,
  NDescriptions,
  NDescriptionsItem,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  ConfigDataType,
  configManagementApi,
  ConfigType,
  createPageRequest,
  EnableStatus,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { CONFIG_DATA_TYPE_OPTIONS, CONFIG_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformConfigPage' })

interface ConfigFormModel {
  basicId?: string
  configDescription?: string | null
  configGroup?: string | null
  configKey: string
  configName: string
  configType: ConfigType
  configValue?: string | null
  dataType: ConfigDataType
  defaultValue?: string | null
  isBuiltIn: boolean
  isEncrypted: boolean
  isGlobal: boolean
  remark?: string | null
  sort: number
  status: EnableStatus
}

const { t } = useI18n()
const message = useMessage()
const statusOptions = STATUS_OPTIONS
const configTypeOptions = CONFIG_TYPE_OPTIONS
const dataTypeOptions = CONFIG_DATA_TYPE_OPTIONS

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const globalOptions = computed(() => [
  { label: t('setting.common.global'), value: 1 },
  { label: t('setting.common.not_global'), value: 0 },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadConfig() {
  void schemaPageRef.value?.reload()
}

function canMaintainConfig(row: ConfigListItemDto) {
  return !row.isBuiltIn
}

// 枚举筛选值（ConfigType/ConfigDataType/EnableStatus 均为后端字符串枚举）
// 直接透传原值，仅把空值归一为 undefined，避免误把字符串枚举转成 NaN。
function pickEnum<T>(value: unknown): T | undefined {
  return value === undefined || value === null || value === '' ? undefined : (value as T)
}

// ── 字段单一事实源（列 + searchable/advancedSearch；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.config.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.config.keyword_placeholder'), width: 250, order: 0 },
  { key: 'configName', title: t('setting.config.config_name'), dataType: 'string', sortable: true, importable: true, required: true, minWidth: 160, order: 1 },
  { key: 'configKey', title: t('setting.config.config_key'), dataType: 'string', importable: true, required: true, minWidth: 180, order: 2 },
  { key: 'configGroup', title: t('setting.config.config_group'), dataType: 'string', importable: true, minWidth: 100, order: 3 },
  // enum/boolean + options 由框架自动渲染为 NTag，无需自定义 render
  { key: 'configType', title: t('setting.config.config_type'), dataType: 'enum', searchable: true, importable: true, options: configTypeOptions, searchPlaceholder: t('setting.config.config_type_placeholder'), width: 100, order: 4 },
  { key: 'dataType', title: t('setting.config.data_type'), dataType: 'enum', advancedSearch: true, importable: true, options: dataTypeOptions, searchPlaceholder: t('setting.config.data_type_placeholder'), width: 100, order: 5 },
  // 仅导入字段：配置值不在列表 DTO 中，visible:false 不进表格/列设置
  { key: 'configValue', title: t('setting.config.config_value'), dataType: 'text', visible: false, importable: true, order: 5.5 },
  { key: 'isGlobal', title: t('setting.config.is_global'), dataType: 'boolean', searchable: true, importable: true, options: globalOptions.value, searchPlaceholder: t('setting.config.is_global_placeholder'), width: 80, order: 6 },
  { key: 'isBuiltIn', title: t('setting.config.is_builtin'), dataType: 'boolean', width: 80, order: 7 },
  { key: 'isEncrypted', title: t('setting.config.is_encrypted'), dataType: 'boolean', width: 80, order: 8 },
  { key: 'status', title: t('setting.config.status'), dataType: 'enum', searchable: true, importable: true, options: statusOptions, searchPlaceholder: t('setting.config.status_placeholder'), width: 90, order: 9 },
  { key: 'sort', title: t('setting.config.sort'), dataType: 'number', sortable: true, importable: true, width: 80, order: 10 },
  { key: 'createdTime', title: t('setting.config.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

// ── 资源适配器：归一化查询参数 → 后端 API（仅放后端支持的搜索字段） ──
const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.config',
  exportPermission: 'saas:config:export',
  importPermission: 'saas:config:import',
  pageName: t('setting.config.page_name'),
  batchRemovable: true,
  removePermission: 'saas:config:delete',
  statusPermission: 'saas:config:status',
  rowKey: 'basicId',
  scrollX: 1500,
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword, configType, dataType, isGlobal, status } = params.filters
      return configManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        configType: pickEnum<ConfigType>(configType),
        dataType: pickEnum<ConfigDataType>(dataType),
        isGlobal: isGlobal === undefined || isGlobal === null || isGlobal === '' ? undefined : Boolean(Number(isGlobal)),
        keyword: (keyword as string | undefined)?.trim() || undefined,
        status: pickEnum<EnableStatus>(status),
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    remove: id => configManagementApi.delete(id),
    updateStatus: (id, enabled) => configManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('setting.config.enable_remark') : t('setting.config.disable_remark') }),
    // 导入适配器：importable 字段记录 → CreateDto（缺省值在此兜底）
    create: (record) => {
      const input: ConfigCreateDto = {
        configDescription: null,
        configGroup: (record.configGroup as string | undefined) ?? null,
        configKey: String(record.configKey ?? '').trim(),
        configName: String(record.configName ?? '').trim(),
        configType: (record.configType as ConfigType | undefined) ?? ConfigType.Application,
        configValue: (record.configValue as string | undefined) ?? null,
        dataType: (record.dataType as ConfigDataType | undefined) ?? ConfigDataType.String,
        defaultValue: null,
        isEncrypted: false,
        isGlobal: Boolean(record.isGlobal ?? false),
        remark: null,
        sort: typeof record.sort === 'number' ? record.sort : 100,
        status: (record.status as EnableStatus | undefined) ?? EnableStatus.Enabled,
      }
      return configManagementApi.create(input)
    },
  },
  actions: [
    { key: 'create', title: t('setting.config.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: t('setting.config.view'), scope: 'row' },
    { key: 'edit', title: t('setting.common.edit'), scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
    { key: 'toggle', title: t('setting.job.toggle'), scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
    { key: 'delete', title: t('setting.common.delete'), scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
  ],
}))

// ── 行/页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ConfigListItemDto | undefined
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
        handleEdit(row)
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

// ── 弹窗/表单（完整保留） ──
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<ConfigDetailDto | null>(null)
const configForm = ref<ConfigFormModel>(createDefaultConfigForm())

const modalTitle = computed(() => (configForm.value.basicId ? t('setting.config.edit_title') : t('setting.config.add_title')))

function createDefaultConfigForm(): ConfigFormModel {
  return {
    configDescription: null,
    configGroup: null,
    configKey: '',
    configName: '',
    configType: ConfigType.Application,
    configValue: null,
    dataType: ConfigDataType.String,
    defaultValue: null,
    isBuiltIn: false,
    isEncrypted: false,
    isGlobal: false,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? t('setting.common.yes') : t('setting.common.no')
}

function handleAdd() {
  editingStatus.value = null
  configForm.value = createDefaultConfigForm()
  modalVisible.value = true
}

function handleEdit(row: ConfigListItemDto) {
  editingStatus.value = row.status
  configForm.value = {
    basicId: row.basicId,
    configDescription: row.configDescription ?? null,
    configGroup: row.configGroup ?? null,
    configKey: row.configKey,
    configName: row.configName,
    configType: row.configType,
    configValue: null,
    dataType: row.dataType,
    defaultValue: null,
    isBuiltIn: row.isBuiltIn,
    isEncrypted: row.isEncrypted,
    isGlobal: row.isGlobal,
    remark: null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleView(row: ConfigListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await configManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('setting.config.detail_not_found'))
    }
  }
  catch {
    message.error(t('setting.config.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function validateConfigForm() {
  if (!configForm.value.configName.trim()) {
    message.warning(t('setting.config.validate_config_name'))
    return false
  }

  if (!configForm.value.basicId && !configForm.value.configKey.trim()) {
    message.warning(t('setting.config.validate_config_key'))
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateConfigForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (configForm.value.basicId) {
      const updateInput: ConfigUpdateDto = {
        basicId: configForm.value.basicId,
        configDescription: configForm.value.configDescription,
        configGroup: configForm.value.configGroup,
        configName: configForm.value.configName.trim(),
        configType: configForm.value.configType,
        configValue: configForm.value.configValue,
        dataType: configForm.value.dataType,
        defaultValue: configForm.value.defaultValue,
        isEncrypted: configForm.value.isEncrypted,
        isGlobal: configForm.value.isGlobal,
        remark: configForm.value.remark,
        sort: configForm.value.sort,
      }

      await configManagementApi.update(updateInput)
      if (editingStatus.value !== configForm.value.status) {
        await configManagementApi.updateStatus({
          basicId: configForm.value.basicId,
          remark: configForm.value.remark,
          status: configForm.value.status,
        })
      }
    }
    else {
      const createInput: ConfigCreateDto = {
        configDescription: configForm.value.configDescription,
        configGroup: configForm.value.configGroup,
        configKey: configForm.value.configKey.trim(),
        configName: configForm.value.configName.trim(),
        configType: configForm.value.configType,
        configValue: configForm.value.configValue,
        dataType: configForm.value.dataType,
        defaultValue: configForm.value.defaultValue,
        isEncrypted: configForm.value.isEncrypted,
        isGlobal: configForm.value.isGlobal,
        remark: configForm.value.remark,
        sort: configForm.value.sort,
        status: configForm.value.status,
      }

      await configManagementApi.create(createInput)
    }

    message.success(t('setting.common.save_success'))
    modalVisible.value = false
    reloadConfig()
  }
  catch {
    message.error(t('setting.common.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: ConfigListItemDto) {
  await configManagementApi.delete(row.basicId)
  message.success(t('setting.common.delete_success'))
  reloadConfig()
}

async function handleToggleStatus(row: ConfigListItemDto) {
  await configManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.config.frontend_disable_remark') : t('setting.config.frontend_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success(t('setting.common.status_updated'))
  reloadConfig()
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
      style="width: 720px; max-width: calc(100vw - 32px);"
    >
      <template v-if="currentDetail" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="tabler:settings" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ currentDetail.configName }}
            </div>
            <div class="det-hd-sub">
              {{ currentDetail.configKey }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        {{ t('setting.common.loading') }}
      </div>
      <NTabs v-else-if="currentDetail" type="line" animated size="small">
        <NTabPane name="overview" :tab="t('setting.config.overview')">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem :label="t('setting.config.config_group')">
              {{ formatNullable(currentDetail.configGroup) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.config_type')">
              {{ getOptionLabel(configTypeOptions, currentDetail.configType) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.data_type_field')">
              {{ getOptionLabel(dataTypeOptions, currentDetail.dataType) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.status')">
              <NTag size="small" :type="currentDetail.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.is_global_field')">
              {{ formatBoolean(currentDetail.isGlobal) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.is_builtin_field')">
              {{ formatBoolean(currentDetail.isBuiltIn) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.is_encrypted_field')">
              {{ formatBoolean(currentDetail.isEncrypted) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.sort')">
              {{ currentDetail.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.description')" :span="2">
              {{ formatNullable(currentDetail.configDescription) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.created_time')">
              {{ formatNullableDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.modified_time')">
              {{ formatNullableDate(currentDetail.modifiedTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.created_by')">
              {{ formatNullable(currentDetail.createdBy) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.modified_by')">
              {{ formatNullable(currentDetail.modifiedBy) }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="values" :tab="t('setting.config.values')">
          <NDescriptions :column="1" bordered size="small">
            <NDescriptionsItem :label="t('setting.config.current_value')">
              <NSpace align="center" size="small">
                <NTag v-if="currentDetail.hasCurrentValue" size="small" type="success" :bordered="false">
                  {{ t('setting.config.configured') }}
                </NTag>
                <NTag v-else size="small" :bordered="false">
                  {{ t('setting.config.not_configured') }}
                </NTag>
                <span v-if="currentDetail.isEncrypted" style="font-size:12px;color:var(--n-text-color-3)">{{ t('setting.config.encrypted_hint') }}</span>
              </NSpace>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.config.default_value')">
              <NSpace align="center" size="small">
                <NTag v-if="currentDetail.hasFallbackValue" size="small" type="info" :bordered="false">
                  {{ t('setting.config.value_set') }}
                </NTag>
                <NTag v-else size="small" :bordered="false">
                  {{ t('setting.config.value_unset') }}
                </NTag>
              </NSpace>
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.hasNote" :label="t('setting.config.note')">
              {{ t('setting.config.note_hint') }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            {{ t('setting.common.close') }}
          </NButton>
          <NButton
            v-if="currentDetail"
            size="small"
            type="primary"
            @click="detailVisible = false; handleEdit(currentDetail as ConfigListItemDto)"
          >
            {{ t('setting.common.edit') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="configForm" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem :label="t('setting.config.config_name')" path="configName">
            <NInput v-model:value="configForm.configName" clearable size="small" :placeholder="t('setting.config.config_name_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.config.config_key')" path="configKey">
            <NInput
              v-model:value="configForm.configKey"
              clearable size="small"
              :disabled="Boolean(configForm.basicId)"
              :placeholder="t('setting.config.config_key_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('setting.config.config_group')" path="configGroup">
            <NInput v-model:value="configForm.configGroup" clearable size="small" :placeholder="t('setting.config.config_group_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.config.config_type')" path="configType">
            <NSelect v-model:value="configForm.configType" :options="configTypeOptions" />
          </NFormItem>
          <NFormItem :label="t('setting.config.data_type')" path="dataType">
            <NSelect v-model:value="configForm.dataType" :options="dataTypeOptions" />
          </NFormItem>
          <NFormItem :label="t('setting.config.config_value')" path="configValue" style="grid-column: span 2">
            <NInput
              v-model:value="configForm.configValue"
              :rows="5"
              clearable size="small"
              :placeholder="t('setting.config.config_value_placeholder')"
              type="textarea"
            />
          </NFormItem>
          <NFormItem :label="t('setting.config.default_value')" path="defaultValue">
            <NInput v-model:value="configForm.defaultValue" clearable size="small" :placeholder="t('setting.config.default_value_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.config.is_global_field')" path="isGlobal">
            <NSwitch v-model:value="configForm.isGlobal" />
          </NFormItem>
          <NFormItem :label="t('setting.config.is_builtin_field')" path="isBuiltIn">
            <NSwitch :value="configForm.isBuiltIn" disabled />
          </NFormItem>
          <NFormItem :label="t('setting.config.is_encrypted_field')" path="isEncrypted">
            <NSwitch v-model:value="configForm.isEncrypted" />
          </NFormItem>
          <NFormItem :label="t('setting.config.sort')" path="sort">
            <NInputNumber v-model:value="configForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem :label="t('setting.config.remark')" path="remark">
            <NInput v-model:value="configForm.remark" clearable size="small" :placeholder="t('setting.config.remark_placeholder')" />
          </NFormItem>
          <NFormItem v-if="!configForm.basicId" :label="t('setting.config.status')" path="status">
            <NSelect v-model:value="configForm.status" :options="statusOptions" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            {{ t('setting.common.cancel') }}
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('setting.common.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
