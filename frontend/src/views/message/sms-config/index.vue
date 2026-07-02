<script setup lang="ts">
import type { SmsConfigListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NConfigProvider,
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
import { createPageRequest, querySortsFromSchema, smsConfigApi, SmsProviderType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'MessageSmsConfigPage' })

interface SmsConfigFormModel {
  accessKeyId: string
  accessKeySecret: string | null
  basicId?: string
  configCode: string
  configName: string
  isDefault: boolean
  isEnabled: boolean
  provider: SmsProviderType
  region: string | null
  remark: string | null
  sdkAppId: string | null
  signName: string
  sort: number
  templateMap: string | null
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const providerOptions = computed(() => [
  { label: t('message.sms_config.provider.aliyun'), value: SmsProviderType.Aliyun },
  { label: t('message.sms_config.provider.tencent_cloud'), value: SmsProviderType.TencentCloud },
])

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = computed(() => [
  { label: t('message.sms_config.default.is_default'), value: 1 },
  { label: t('message.sms_config.default.not_default'), value: 0 },
])
const enabledOptions = computed(() => [
  { label: t('message.sms_config.enabled.enabled'), value: 1 },
  { label: t('message.sms_config.enabled.disabled'), value: 0 },
])

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.sms_config.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.sms_config.columns.keyword_placeholder'), order: 0 },
  { key: 'configCode', title: t('message.sms_config.columns.config_code'), dataType: 'string', sortable: true, minWidth: 140, order: 1 },
  { key: 'configName', title: t('message.sms_config.columns.config_name'), dataType: 'string', sortable: true, minWidth: 140, order: 2 },
  {
    key: 'provider',
    title: t('message.sms_config.columns.provider'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: providerOptions.value,
    searchPlaceholder: t('message.sms_config.columns.provider_placeholder'),
    width: 110,
    order: 3,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(providerOptions.value, (row as unknown as SmsConfigListItemDto).provider),
    ),
  },
  { key: 'signName', title: t('message.sms_config.columns.sign_name'), dataType: 'string', sortable: true, minWidth: 120, order: 4 },
  {
    key: 'isDefault',
    title: t('message.sms_config.columns.is_default'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: defaultOptions.value,
    searchPlaceholder: t('message.sms_config.columns.is_default_placeholder'),
    width: 90,
    order: 5,
    render: (row) => {
      const isDefault = (row as unknown as SmsConfigListItemDto).isDefault
      return isDefault
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'warning' }, () => t('message.sms_config.tag.default'))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('message.sms_config.columns.status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('message.sms_config.columns.status_placeholder'),
    width: 90,
    order: 6,
    render: (row) => {
      const enabled = (row as unknown as SmsConfigListItemDto).isEnabled
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: enabled ? 'success' : 'error' },
        () => enabled ? t('message.sms_config.tag.enabled') : t('message.sms_config.tag.disabled'),
      )
    },
  },
  { key: 'sort', title: t('message.sms_config.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 7 },
  { key: 'createdTime', title: t('message.sms_config.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.sms-config',
  exportPermission: 'saas:sms-config:export',
  pageName: t('message.sms_config.page_name'),
  statusPermission: 'saas:sms-config:status',
  rowKey: 'basicId',
  scrollX: 1200,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return smsConfigApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(provider) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isDefault: pickBoolean(f.isDefault),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        // provider 为多选，经 conditions.filters In 下发（不走 DTO 顶层 provider 单值字段）
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => smsConfigApi.updateStatus({ basicId: id, isEnabled: enabled }),
  },
  actions: [
    { key: 'create', title: t('message.sms_config.actions.create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:sms-config:create' },
    { key: 'edit', title: t('message.sms_config.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'saas:sms-config:update' },
    { key: 'toggle', title: t('message.sms_config.actions.toggle'), scope: 'row', icon: 'lucide:power', permission: 'saas:sms-config:status' },
    {
      key: 'setDefault',
      title: t('message.sms_config.actions.set_default'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'saas:sms-config:update',
      visible: row => !(row as unknown as SmsConfigListItemDto).isDefault,
    },
    {
      key: 'delete',
      title: t('message.sms_config.actions.delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      type: 'error',
      permission: 'saas:sms-config:delete',
      visible: row => !(row as unknown as SmsConfigListItemDto).isDefault,
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as SmsConfigListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        handleToggleStatus(row)
      }
      break
    case 'setDefault':
      if (row) {
        handleSetDefault(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

// ── 弹窗/表单 ──
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingHasSecret = ref(false)
const form = ref<SmsConfigFormModel>(createDefaultForm())

const modalTitle = computed(() => (form.value.basicId ? t('message.sms_config.form.edit_title') : t('message.sms_config.form.add_title')))
const isTencentCloud = computed(() => form.value.provider === SmsProviderType.TencentCloud)
const secretPlaceholder = computed(() =>
  form.value.basicId && editingHasSecret.value ? t('message.sms_config.form.secret_configured') : t('message.sms_config.form.secret_placeholder'),
)

function createDefaultForm(): SmsConfigFormModel {
  return {
    accessKeyId: '',
    accessKeySecret: null,
    configCode: '',
    configName: '',
    isDefault: false,
    isEnabled: true,
    provider: SmsProviderType.Aliyun,
    region: null,
    remark: null,
    sdkAppId: null,
    signName: '',
    sort: 100,
    templateMap: null,
  }
}

function handleAdd() {
  editingHasSecret.value = false
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: SmsConfigListItemDto) {
  try {
    const detail = await smsConfigApi.detail(row.basicId)
    if (!detail) {
      message.warning(t('message.sms_config.message.detail_not_found'))
      return
    }

    editingHasSecret.value = detail.hasAccessKeySecret
    form.value = {
      accessKeyId: detail.accessKeyId,
      accessKeySecret: null,
      basicId: detail.basicId,
      configCode: detail.configCode,
      configName: detail.configName,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      provider: detail.provider,
      region: detail.region ?? null,
      remark: detail.remark ?? null,
      sdkAppId: detail.sdkAppId ?? null,
      signName: detail.signName,
      sort: detail.sort,
      templateMap: detail.templateMap ?? null,
    }
    modalVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('message.sms_config.message.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    message.warning(t('message.sms_config.message.input_config_code'))
    return false
  }

  if (!form.value.configName.trim()) {
    message.warning(t('message.sms_config.message.input_config_name'))
    return false
  }

  if (!form.value.accessKeyId.trim()) {
    message.warning(t('message.sms_config.message.input_access_key_id'))
    return false
  }

  if (!form.value.basicId && !form.value.accessKeySecret?.trim()) {
    message.warning(t('message.sms_config.message.input_access_key_secret'))
    return false
  }

  if (!form.value.signName.trim()) {
    message.warning(t('message.sms_config.message.input_sign_name'))
    return false
  }

  if (isTencentCloud.value) {
    if (!form.value.sdkAppId?.trim()) {
      message.warning(t('message.sms_config.message.input_sdk_app_id'))
      return false
    }

    if (!form.value.region?.trim()) {
      message.warning(t('message.sms_config.message.input_region'))
      return false
    }
  }

  const templateMap = form.value.templateMap?.trim()
  if (templateMap) {
    try {
      const parsed: unknown = JSON.parse(templateMap)
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
        message.warning(t('message.sms_config.message.template_map_invalid'))
        return false
      }
    }
    catch {
      message.warning(t('message.sms_config.message.template_map_invalid'))
      return false
    }
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
      await smsConfigApi.update({
        accessKeyId: form.value.accessKeyId.trim(),
        accessKeySecret: form.value.accessKeySecret?.trim() || null,
        basicId: form.value.basicId,
        configName: form.value.configName.trim(),
        provider: form.value.provider,
        region: form.value.region,
        remark: form.value.remark,
        sdkAppId: form.value.sdkAppId,
        signName: form.value.signName.trim(),
        sort: form.value.sort,
        templateMap: form.value.templateMap?.trim() || null,
      })
    }
    else {
      await smsConfigApi.create({
        accessKeyId: form.value.accessKeyId.trim(),
        accessKeySecret: form.value.accessKeySecret?.trim() ?? '',
        configCode: form.value.configCode.trim(),
        configName: form.value.configName.trim(),
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        provider: form.value.provider,
        region: form.value.region,
        remark: form.value.remark,
        sdkAppId: form.value.sdkAppId,
        signName: form.value.signName.trim(),
        sort: form.value.sort,
        templateMap: form.value.templateMap?.trim() || null,
      })
    }

    message.success(t('message.sms_config.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    message.error((e as Error).message || t('message.sms_config.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: SmsConfigListItemDto) {
  const next = !row.isEnabled
  dialog.warning({
    title: next ? t('message.sms_config.message.enable_title') : t('message.sms_config.message.disable_title'),
    content: next
      ? t('message.sms_config.message.enable_content', { name: row.configName })
      : t('message.sms_config.message.disable_content', { name: row.configName }),
    positiveText: next ? t('message.sms_config.message.enable') : t('message.sms_config.message.disable'),
    negativeText: t('message.sms_config.form.cancel'),
    onPositiveClick: async () => {
      try {
        await smsConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        message.success(t('message.sms_config.message.status_updated'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('message.sms_config.message.status_update_failed'))
      }
    },
  })
}

function handleSetDefault(row: SmsConfigListItemDto) {
  dialog.info({
    title: t('message.sms_config.message.set_default_title'),
    content: t('message.sms_config.message.set_default_content', { name: row.configName }),
    positiveText: t('message.sms_config.message.set_default'),
    negativeText: t('message.sms_config.form.cancel'),
    onPositiveClick: async () => {
      try {
        await smsConfigApi.setDefault({ basicId: row.basicId })
        message.success(t('message.sms_config.message.set_default_success'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('message.sms_config.message.set_default_failed'))
      }
    },
  })
}

function handleDelete(row: SmsConfigListItemDto) {
  dialog.warning({
    title: t('message.sms_config.message.delete_title'),
    content: t('message.sms_config.message.delete_content', { name: row.configName }),
    positiveText: t('message.sms_config.message.delete'),
    negativeText: t('message.sms_config.form.cancel'),
    onPositiveClick: async () => {
      try {
        await smsConfigApi.delete(row.basicId)
        message.success(t('message.sms_config.message.delete_success'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('message.sms_config.message.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="form" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem :label="t('message.sms_config.form.config_code')" path="configCode">
            <NInput
              v-model:value="form.configCode"
              clearable size="small"
              :disabled="Boolean(form.basicId)"
              :placeholder="t('message.sms_config.form.config_code_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.config_name')" path="configName">
            <NInput v-model:value="form.configName" clearable size="small" :placeholder="t('message.sms_config.form.config_name_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.provider')" path="provider">
            <NSelect v-model:value="form.provider" :options="providerOptions" />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.sign_name')" path="signName">
            <NInput v-model:value="form.signName" clearable size="small" :placeholder="t('message.sms_config.form.sign_name_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.access_key_id')" path="accessKeyId">
            <NInput v-model:value="form.accessKeyId" clearable size="small" :placeholder="t('message.sms_config.form.access_key_id_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.access_key_secret')" path="accessKeySecret">
            <NInput
              v-model:value="form.accessKeySecret"
              size="small"
              type="password"
              show-password-on="click"
              :placeholder="secretPlaceholder"
            />
          </NFormItem>

          <template v-if="isTencentCloud">
            <NFormItem :label="t('message.sms_config.form.sdk_app_id')" path="sdkAppId">
              <NInput v-model:value="form.sdkAppId" clearable size="small" :placeholder="t('message.sms_config.form.sdk_app_id_placeholder')" />
            </NFormItem>
            <NFormItem :label="t('message.sms_config.form.region')" path="region">
              <NInput v-model:value="form.region" clearable size="small" :placeholder="t('message.sms_config.form.region_placeholder')" />
            </NFormItem>
          </template>

          <NFormItem :label="t('message.sms_config.form.template_map')" path="templateMap" style="grid-column: span 2">
            <NInput
              v-model:value="form.templateMap"
              size="small"
              type="textarea"
              :rows="4"
              :placeholder="t('message.sms_config.form.template_map_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('message.sms_config.form.sort')" path="sort">
            <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
          </NFormItem>

          <template v-if="!form.basicId">
            <NFormItem :label="t('message.sms_config.form.is_enabled')" path="isEnabled">
              <NSwitch v-model:value="form.isEnabled" />
            </NFormItem>
            <NFormItem :label="t('message.sms_config.form.is_default')" path="isDefault">
              <NSwitch v-model:value="form.isDefault" :disabled="!form.isEnabled" />
            </NFormItem>
          </template>

          <NFormItem :label="t('message.sms_config.form.remark')" path="remark" style="grid-column: span 2">
            <NInput v-model:value="form.remark" clearable size="small" :placeholder="t('message.sms_config.form.remark_placeholder')" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            {{ t('message.sms_config.form.cancel') }}
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('message.sms_config.form.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>
