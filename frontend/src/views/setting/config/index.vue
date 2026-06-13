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

const message = useMessage()
const statusOptions = STATUS_OPTIONS
const configTypeOptions = CONFIG_TYPE_OPTIONS
const dataTypeOptions = CONFIG_DATA_TYPE_OPTIONS

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const globalOptions = [
  { label: '全局', value: 1 },
  { label: '非全局', value: 0 },
]

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
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索配置名称/键/分组', width: 250, order: 0 },
  { key: 'configName', title: '配置名称', dataType: 'string', sortable: true, importable: true, required: true, minWidth: 160, order: 1 },
  { key: 'configKey', title: '配置键', dataType: 'string', importable: true, required: true, minWidth: 180, order: 2 },
  { key: 'configGroup', title: '分组', dataType: 'string', importable: true, minWidth: 100, order: 3 },
  // enum/boolean + options 由框架自动渲染为 NTag，无需自定义 render
  { key: 'configType', title: '配置类型', dataType: 'enum', searchable: true, importable: true, options: configTypeOptions, searchPlaceholder: '配置类型', width: 100, order: 4 },
  { key: 'dataType', title: '数据类型', dataType: 'enum', advancedSearch: true, importable: true, options: dataTypeOptions, searchPlaceholder: '数据类型', width: 100, order: 5 },
  // 仅导入字段：配置值不在列表 DTO 中，visible:false 不进表格/列设置
  { key: 'configValue', title: '配置值', dataType: 'text', visible: false, importable: true, order: 5.5 },
  { key: 'isGlobal', title: '全局', dataType: 'boolean', searchable: true, importable: true, options: globalOptions, searchPlaceholder: '全局', width: 80, order: 6 },
  { key: 'isBuiltIn', title: '内置', dataType: 'boolean', width: 80, order: 7 },
  { key: 'isEncrypted', title: '加密', dataType: 'boolean', width: 80, order: 8 },
  { key: 'status', title: '状态', dataType: 'enum', searchable: true, importable: true, options: statusOptions, searchPlaceholder: '状态', width: 90, order: 9 },
  { key: 'sort', title: '排序', dataType: 'number', sortable: true, importable: true, width: 80, order: 10 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
]

// ── 资源适配器：归一化查询参数 → 后端 API（仅放后端支持的搜索字段） ──
const schema: PageSchema = {
  pageCode: 'platform.config',
  exportPermission: 'saas:config:export',
  importPermission: 'saas:config:import',
  pageName: '参数配置',
  batchRemovable: true,
  removePermission: 'saas:config:delete',
  statusPermission: 'saas:config:status',
  rowKey: 'basicId',
  scrollX: 1500,
  fields,
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
    updateStatus: (id, enabled) => configManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? '批量启用配置' : '批量停用配置' }),
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
    { key: 'create', title: '新增配置', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
    { key: 'toggle', title: '启用/停用', scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
    { key: 'delete', title: '删除', scope: 'row', visible: row => canMaintainConfig(row as unknown as ConfigListItemDto) },
  ],
}

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

const modalTitle = computed(() => (configForm.value.basicId ? '编辑配置' : '新增配置'))

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
  return value ? '是' : '否'
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
      message.warning('未查询到配置详情')
    }
  }
  catch {
    message.error('加载配置详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

function validateConfigForm() {
  if (!configForm.value.configName.trim()) {
    message.warning('请输入配置名称')
    return false
  }

  if (!configForm.value.basicId && !configForm.value.configKey.trim()) {
    message.warning('请输入配置键')
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

    message.success('保存成功')
    modalVisible.value = false
    reloadConfig()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: ConfigListItemDto) {
  await configManagementApi.delete(row.basicId)
  message.success('删除成功')
  reloadConfig()
}

async function handleToggleStatus(row: ConfigListItemDto) {
  await configManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用配置' : '前端启用配置',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
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
        加载中…
      </div>
      <NTabs v-else-if="currentDetail" type="line" animated size="small">
        <NTabPane name="overview" tab="概览">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem label="配置分组">
              {{ formatNullable(currentDetail.configGroup) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="配置类型">
              {{ getOptionLabel(configTypeOptions, currentDetail.configType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="数据类型">
              {{ getOptionLabel(dataTypeOptions, currentDetail.dataType) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="状态">
              <NTag size="small" :type="currentDetail.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem label="是否全局">
              {{ formatBoolean(currentDetail.isGlobal) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否内置">
              {{ formatBoolean(currentDetail.isBuiltIn) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="是否加密">
              {{ formatBoolean(currentDetail.isEncrypted) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="排序">
              {{ currentDetail.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem label="描述" :span="2">
              {{ formatNullable(currentDetail.configDescription) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建时间">
              {{ formatNullableDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="修改时间">
              {{ formatNullableDate(currentDetail.modifiedTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="创建人">
              {{ formatNullable(currentDetail.createdBy) }}
            </NDescriptionsItem>
            <NDescriptionsItem label="修改人">
              {{ formatNullable(currentDetail.modifiedBy) }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="values" tab="配置值">
          <NDescriptions :column="1" bordered size="small">
            <NDescriptionsItem label="当前值">
              <NSpace align="center" size="small">
                <NTag v-if="currentDetail.hasCurrentValue" size="small" type="success" :bordered="false">
                  已配置
                </NTag>
                <NTag v-else size="small" :bordered="false">
                  未配置
                </NTag>
                <span v-if="currentDetail.isEncrypted" style="font-size:12px;color:var(--n-text-color-3)">敏感项已加密，请通过编辑查看或修改</span>
              </NSpace>
            </NDescriptionsItem>
            <NDescriptionsItem label="默认值">
              <NSpace align="center" size="small">
                <NTag v-if="currentDetail.hasFallbackValue" size="small" type="info" :bordered="false">
                  已设置
                </NTag>
                <NTag v-else size="small" :bordered="false">
                  未设置
                </NTag>
              </NSpace>
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.hasNote" label="说明">
              含附加备注，编辑时可维护
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            关闭
          </NButton>
          <NButton
            v-if="currentDetail"
            size="small"
            type="primary"
            @click="detailVisible = false; handleEdit(currentDetail as ConfigListItemDto)"
          >
            编辑
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
          <NFormItem label="配置名称" path="configName">
            <NInput v-model:value="configForm.configName" clearable size="small" placeholder="请输入配置名称" />
          </NFormItem>
          <NFormItem label="配置键" path="configKey">
            <NInput
              v-model:value="configForm.configKey"
              clearable size="small"
              :disabled="Boolean(configForm.basicId)"
              placeholder="如: app.site_name"
            />
          </NFormItem>
          <NFormItem label="配置分组" path="configGroup">
            <NInput v-model:value="configForm.configGroup" clearable size="small" placeholder="请输入配置分组" />
          </NFormItem>
          <NFormItem label="配置类型" path="configType">
            <NSelect v-model:value="configForm.configType" :options="configTypeOptions" />
          </NFormItem>
          <NFormItem label="数据类型" path="dataType">
            <NSelect v-model:value="configForm.dataType" :options="dataTypeOptions" />
          </NFormItem>
          <NFormItem label="配置值" path="configValue" style="grid-column: span 2">
            <NInput
              v-model:value="configForm.configValue"
              :rows="5"
              clearable size="small"
              placeholder="请输入配置值"
              type="textarea"
            />
          </NFormItem>
          <NFormItem label="默认值" path="defaultValue">
            <NInput v-model:value="configForm.defaultValue" clearable size="small" placeholder="请输入默认值" />
          </NFormItem>
          <NFormItem label="是否全局" path="isGlobal">
            <NSwitch v-model:value="configForm.isGlobal" />
          </NFormItem>
          <NFormItem label="是否内置" path="isBuiltIn">
            <NSwitch :value="configForm.isBuiltIn" disabled />
          </NFormItem>
          <NFormItem label="是否加密" path="isEncrypted">
            <NSwitch v-model:value="configForm.isEncrypted" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="configForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem label="备注" path="remark">
            <NInput v-model:value="configForm.remark" clearable size="small" placeholder="请输入备注" />
          </NFormItem>
          <NFormItem v-if="!configForm.basicId" label="状态" path="status">
            <NSelect v-model:value="configForm.status" :options="statusOptions" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            取消
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
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
