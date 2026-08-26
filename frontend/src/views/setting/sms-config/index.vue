<script setup lang="ts">
import type { SmsConfigListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, smsConfigApi, SmsProviderType } from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SettingSmsConfigPage' })

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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
    render: row => h(XhTagRoot, { variant: 'solid', tone: 'info' }, () => h(XhTagLabel, () => getOptionLabel(providerOptions.value, (row as unknown as SmsConfigListItemDto).provider))),
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
        ? h(XhTagRoot, { variant: 'solid', tone: 'warning' }, () => h(XhTagLabel, () => t('message.sms_config.tag.default')))
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
      return h(XhTagRoot, { variant: 'solid', tone: enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => enabled ? t('message.sms_config.tag.enabled') : t('message.sms_config.tag.disabled')))
    },
  },
  { key: 'sort', title: t('message.sms_config.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 7 },
  { key: 'createdTime', title: t('message.sms_config.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.sms-config',
  exportPermission: 'saas:sms-config:export',
  pageName: t('message.sms_config.page_name'),
  statusPermission: 'saas:sms-config:status',
  rowKey: 'basicId',
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
      toast.warning(t('message.sms_config.message.detail_not_found'))
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
    toast.error((e as Error).message || t('message.sms_config.message.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    toast.warning(t('message.sms_config.message.input_config_code'))
    return false
  }

  if (!form.value.configName.trim()) {
    toast.warning(t('message.sms_config.message.input_config_name'))
    return false
  }

  if (!form.value.accessKeyId.trim()) {
    toast.warning(t('message.sms_config.message.input_access_key_id'))
    return false
  }

  if (!form.value.basicId && !form.value.accessKeySecret?.trim()) {
    toast.warning(t('message.sms_config.message.input_access_key_secret'))
    return false
  }

  if (!form.value.signName.trim()) {
    toast.warning(t('message.sms_config.message.input_sign_name'))
    return false
  }

  if (isTencentCloud.value) {
    if (!form.value.sdkAppId?.trim()) {
      toast.warning(t('message.sms_config.message.input_sdk_app_id'))
      return false
    }

    if (!form.value.region?.trim()) {
      toast.warning(t('message.sms_config.message.input_region'))
      return false
    }
  }

  const templateMap = form.value.templateMap?.trim()
  if (templateMap) {
    try {
      const parsed: unknown = JSON.parse(templateMap)
      if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
        toast.warning(t('message.sms_config.message.template_map_invalid'))
        return false
      }
    }
    catch {
      toast.warning(t('message.sms_config.message.template_map_invalid'))
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

    toast.success(t('message.sms_config.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.error((e as Error).message || t('message.sms_config.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: SmsConfigListItemDto) {
  const next = !row.isEnabled
  void dialog.confirm({
    badge: 'warning',
    title: next ? t('message.sms_config.message.enable_title') : t('message.sms_config.message.disable_title'),
    content: next
      ? t('message.sms_config.message.enable_content', { name: row.configName })
      : t('message.sms_config.message.disable_content', { name: row.configName }),
    okText: next ? t('message.sms_config.message.enable') : t('message.sms_config.message.disable'),
    cancelText: t('message.sms_config.form.cancel'),
    onOk: async () => {
      try {
        await smsConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        toast.success(t('message.sms_config.message.status_updated'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.sms_config.message.status_update_failed'))
      }
    },
  })
}

function handleSetDefault(row: SmsConfigListItemDto) {
  void dialog.confirm({
    badge: 'info',
    title: t('message.sms_config.message.set_default_title'),
    content: t('message.sms_config.message.set_default_content', { name: row.configName }),
    okText: t('message.sms_config.message.set_default'),
    cancelText: t('message.sms_config.form.cancel'),
    onOk: async () => {
      try {
        await smsConfigApi.setDefault({ basicId: row.basicId })
        toast.success(t('message.sms_config.message.set_default_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.sms_config.message.set_default_failed'))
      }
    },
  })
}

function handleDelete(row: SmsConfigListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.sms_config.message.delete_title'),
    content: t('message.sms_config.message.delete_content', { name: row.configName }),
    okText: t('message.sms_config.message.delete'),
    cancelText: t('message.sms_config.form.cancel'),
    onOk: async () => {
      try {
        await smsConfigApi.delete(row.basicId)
        toast.success(t('message.sms_config.message.delete_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('message.sms_config.message.delete_failed'))
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
        <XhFormFieldGroup value="configCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.config_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.configCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('message.sms_config.form.config_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.configName" clearable :placeholder="t('message.sms_config.form.config_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="provider">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.provider') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.provider" :options="providerOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="signName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.sign_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.signName" clearable :placeholder="t('message.sms_config.form.sign_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="accessKeyId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.access_key_id') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.accessKeyId" clearable :placeholder="t('message.sms_config.form.access_key_id_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="accessKeySecret">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.access_key_secret') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.accessKeySecret"
                type="password"
                autocomplete="new-password"
                :placeholder="secretPlaceholder"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="isTencentCloud">
          <XhFormFieldGroup value="sdkAppId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.sms_config.form.sdk_app_id') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.sdkAppId" clearable :placeholder="t('message.sms_config.form.sdk_app_id_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="region">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.sms_config.form.region') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.region" clearable :placeholder="t('message.sms_config.form.region_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="templateMap" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.template_map') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.templateMap"
                type="textarea"
                :rows="4"
                :placeholder="t('message.sms_config.form.template_map_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="!form.basicId">
          <XhFormFieldGroup value="isEnabled">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.sms_config.form.is_enabled') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="isDefault">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('message.sms_config.form.is_default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isDefault" :disabled="!form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('message.sms_config.form.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :placeholder="t('message.sms_config.form.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
