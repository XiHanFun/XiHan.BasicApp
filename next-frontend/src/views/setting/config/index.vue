<script setup lang="ts">
import type {
  ConfigCreateDto,
  ConfigDetailDto,
  ConfigListItemDto,
  ConfigUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { SelectOption } from '~/types'
import { XhBadge, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  ConfigDataType,
  configManagementApi,
  ConfigType,
  createPageRequest,
  EnableStatus,
  querySortsFromSchema,
} from '@/api'
import { CONFIG_DATA_TYPE_OPTIONS, CONFIG_TYPE_OPTIONS, STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const configTypeOptions = useEnumOptions('ConfigType', CONFIG_TYPE_OPTIONS)
const dataTypeOptions = useEnumOptions('ConfigDataType', CONFIG_DATA_TYPE_OPTIONS)

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const globalOptions = computed(() => [
  { label: t('common.statuses.global'), value: 1 },
  { label: t('common.statuses.not_global'), value: 0 },
])

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadConfig() {
  void schemaPageRef.value?.reload()
}

/** 仅删除受内置限制：后端 DeleteConfigAsync 对内置配置直接抛错 */
function canDeleteConfig(row: ConfigListItemDto) {
  return !row.isBuiltIn
}

// ── 字段单一事实源（列 + searchable/advancedSearch；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.config.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.config.keyword_placeholder'), width: 250, order: 0 },
  { key: 'configName', title: t('setting.config.config_name'), dataType: 'string', sortable: true, importable: true, required: true, minWidth: 160, order: 1 },
  { key: 'configKey', title: t('setting.config.config_key'), dataType: 'string', sortable: true, importable: true, required: true, minWidth: 180, order: 2 },
  { key: 'configGroup', title: t('setting.config.config_group'), dataType: 'string', sortable: true, importable: true, minWidth: 100, order: 3 },
  // enum/boolean + options 由框架自动渲染为徽标，无需自定义 render
  { key: 'configType', title: t('setting.config.config_type'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, importable: true, dictionaryCode: 'ConfigType', options: configTypeOptions.value, searchPlaceholder: t('setting.config.config_type_placeholder'), width: 100, order: 4 },
  { key: 'dataType', title: t('setting.config.data_type'), dataType: 'enum', sortable: true, advancedSearch: true, searchMultiple: true, importable: true, dictionaryCode: 'ConfigDataType', options: dataTypeOptions.value, searchPlaceholder: t('setting.config.data_type_placeholder'), width: 100, order: 5 },
  // 仅导入字段：配置值不在列表 DTO 中，visible:false 不进表格/列设置
  { key: 'configValue', title: t('setting.config.config_value'), dataType: 'text', visible: false, importable: true, order: 5.5 },
  // isGlobal 为派生属性（TenantId==0），非实体列，不可服务端排序
  { key: 'isGlobal', title: t('setting.config.is_global'), dataType: 'boolean', searchable: true, importable: true, options: globalOptions.value, searchPlaceholder: t('setting.config.is_global_placeholder'), width: 80, order: 6 },
  { key: 'isBuiltIn', title: t('setting.config.is_builtin'), dataType: 'boolean', sortable: true, width: 80, order: 7 },
  { key: 'isEncrypted', title: t('setting.config.is_encrypted'), dataType: 'boolean', sortable: true, width: 80, order: 8 },
  { key: 'status', title: t('setting.config.status'), dataType: 'enum', sortable: true, searchable: true, searchMultiple: true, importable: true, dictionaryCode: 'EnableStatus', options: statusOptions.value, searchPlaceholder: t('setting.config.status_placeholder'), width: 90, order: 9 },
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
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword, isGlobal } = params.filters
      return configManagementApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(configType/dataType/status)等通用过滤统一走 conditions（多选经 filters In 下发，不再走 DTO 单值字段）
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isGlobal: isGlobal === undefined || isGlobal === null || isGlobal === '' ? undefined : Boolean(Number(isGlobal)),
        keyword: (keyword as string | undefined)?.trim() || undefined,
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
    // 内置配置本就是给运维调值的：后端只禁止删除，不限制改值与启停
    { key: 'edit', title: t('common.actions.edit'), scope: 'row' },
    { key: 'toggle', title: t('setting.job.toggle'), scope: 'row' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', visible: row => canDeleteConfig(row as unknown as ConfigListItemDto) },
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
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function handleAdd() {
  editingStatus.value = null
  configForm.value = createDefaultConfigForm()
  modalVisible.value = true
}

async function handleEdit(row: ConfigListItemDto) {
  editingStatus.value = row.status
  // 列表不回传配置值；编辑前取详情拿原始 configValue/defaultValue/remark，
  // 否则保存时会把值置空（后端更新为整体替换）。加密项详情返回 null，编辑时留空即保留原值（后端约定）。
  let detail: ConfigDetailDto | null = null
  try {
    detail = await configManagementApi.detail(row.basicId)
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.config.load_detail_failed'))
    return
  }
  configForm.value = {
    basicId: row.basicId,
    configDescription: row.configDescription ?? null,
    configGroup: row.configGroup ?? null,
    configKey: row.configKey,
    configName: row.configName,
    configType: row.configType,
    configValue: detail?.configValue ?? null,
    dataType: row.dataType,
    defaultValue: detail?.defaultValue ?? null,
    isBuiltIn: row.isBuiltIn,
    isEncrypted: row.isEncrypted,
    isGlobal: row.isGlobal,
    remark: detail?.remark ?? null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

/**
 * 配置值展示：JSON（数组/对象）美化缩进，其余原样返回。
 */
function formatConfigValue(value?: string | null): string {
  if (value == null || value === '') {
    return ''
  }
  const trimmed = value.trim()
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
    try {
      return JSON.stringify(JSON.parse(trimmed), null, 2)
    }
    catch {
      return value
    }
  }
  return value
}

async function handleView(row: ConfigListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await configManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      toast.warning(t('setting.config.detail_not_found'))
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.config.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

function validateConfigForm() {
  if (!configForm.value.configName.trim()) {
    toast.warning(t('setting.config.validate_config_name'))
    return false
  }

  if (!configForm.value.basicId && !configForm.value.configKey.trim()) {
    toast.warning(t('setting.config.validate_config_key'))
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

    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadConfig()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: ConfigListItemDto) {
  await configManagementApi.delete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  reloadConfig()
}

async function handleToggleStatus(row: ConfigListItemDto) {
  await configManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('setting.config.frontend_disable_remark') : t('setting.config.frontend_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  toast.success(t('common.messages.status_updated'))
  reloadConfig()
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDialogRoot v-model:open="detailVisible">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogTitle v-if="currentDetail">
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
        </XhDialogTitle>
        <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>

        <div v-if="detailLoading" class="modal-loading">
          {{ t('common.statuses.loading') }}
        </div>
        <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
        <XhTabsRoot v-else-if="currentDetail" default-value="overview" variant="line">
          <XhTabsList>
            <XhTabsTrigger value="overview">
              {{ t('setting.config.overview') }}
            </XhTabsTrigger>
            <XhTabsTrigger value="values">
              {{ t('setting.config.values') }}
            </XhTabsTrigger>
          </XhTabsList>
          <XhTabsContent value="overview">
            <XhDescriptionsRoot :columns="2" bordered size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.config_group') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.configGroup) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.config_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ getOptionLabel(configTypeOptions, currentDetail.configType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.data_type_field') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ getOptionLabel(dataTypeOptions, currentDetail.dataType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhBadge variant="subtle" size="sm" :tone="currentDetail.status === EnableStatus.Enabled ? 'success' : 'danger'">
                    {{ getOptionLabel(statusOptions, currentDetail.status) }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.is_global_field') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isGlobal) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.is_builtin_field') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isBuiltIn) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.is_encrypted_field') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isEncrypted) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.sort') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.sort }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(currentDetail.createdTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.modified_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(currentDetail.modifiedTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.created_by') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.createdBy) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.modified_by') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.modifiedBy) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('setting.config.description') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.configDescription) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </XhTabsContent>
          <XhTabsContent value="values">
            <XhDescriptionsRoot :columns="1" bordered size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.current_value') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <span v-if="currentDetail.isEncrypted" style="font-size:12px;color:hsl(var(--muted-foreground))">{{ t('setting.config.encrypted_hint') }}</span>
                  <pre v-else-if="currentDetail.hasCurrentValue" class="config-value-block">{{ formatConfigValue(currentDetail.configValue) }}</pre>
                  <XhBadge v-else variant="subtle" size="sm">
                    {{ t('setting.config.not_configured') }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.config.default_value') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <span v-if="currentDetail.isEncrypted" style="font-size:12px;color:hsl(var(--muted-foreground))">{{ t('setting.config.encrypted_hint') }}</span>
                  <pre v-else-if="currentDetail.hasFallbackValue" class="config-value-block">{{ formatConfigValue(currentDetail.defaultValue) }}</pre>
                  <XhBadge v-else variant="subtle" size="sm">
                    {{ t('setting.config.value_unset') }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="currentDetail.hasNote">
                <XhDescriptionsLabel>{{ t('setting.config.note') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.remark }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </XhTabsContent>
        </XhTabsRoot>

        <div class="xh-dialog-footer">
          <XhFlex justify="end" gap="md">
            <XhButton size="sm" @click="detailVisible = false">
              {{ t('common.actions.close') }}
            </XhButton>
          </XhFlex>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="configForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="configForm.configName" clearable :placeholder="t('setting.config.config_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configKey">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.config_key') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="configForm.configKey"
                clearable
                :disabled="Boolean(configForm.basicId)"
                :placeholder="t('setting.config.config_key_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configGroup">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.config_group') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="configForm.configGroup" clearable :placeholder="t('setting.config.config_group_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.config_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="configForm.configType" :options="(configTypeOptions as SelectOption[])" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="dataType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.data_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="configForm.dataType" :options="(dataTypeOptions as SelectOption[])" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configValue" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.config_value') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="configForm.configValue"
                :rows="5"
                clearable
                :placeholder="configForm.isEncrypted && configForm.basicId ? t('setting.config.config_value_encrypted_placeholder') : t('setting.config.config_value_placeholder')"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="defaultValue" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.default_value') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="configForm.defaultValue"
                :rows="3"
                clearable
                :placeholder="t('setting.config.default_value_placeholder')"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isGlobal">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.is_global_field') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="configForm.isGlobal" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isBuiltIn">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.is_builtin_field') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch :checked="configForm.isBuiltIn" disabled />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isEncrypted">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.is_encrypted_field') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="configForm.isEncrypted" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="configForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="configForm.remark" clearable :placeholder="t('setting.config.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!configForm.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.config.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="configForm.status" :options="(statusOptions as SelectOption[])" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.config-value-block {
  margin: 0;
  max-height: 220px;
  overflow: auto;
  padding: 8px 10px;
  border-radius: 4px;
  background: var(--xh-bg-subtle);
  font-family: var(--font-family-mono, monospace);
  font-size: 12px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
