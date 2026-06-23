<script setup lang="ts">
import type { StorageConfigListItemDto } from '@/api'
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
import { createPageRequest, storageConfigApi, StorageConfigType } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'FileStoragePage' })

interface StorageConfigFormModel {
  accessKeyId: string | null
  basicId?: string
  bucketName: string | null
  configCode: string
  configName: string
  endpoint: string | null
  isDefault: boolean
  isEnabled: boolean
  region: string | null
  remark: string | null
  secretAccessKey: string | null
  sort: number
  storageType: StorageConfigType
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const storageTypeOptions = computed(() => [
  { label: t('file.storage.storageType.local'), value: StorageConfigType.Local },
  { label: t('file.storage.storageType.s3'), value: StorageConfigType.S3 },
  { label: t('file.storage.storageType.oss'), value: StorageConfigType.OSS },
  { label: t('file.storage.storageType.cos'), value: StorageConfigType.COS },
  { label: t('file.storage.storageType.minio'), value: StorageConfigType.MinIO },
])

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = computed(() => [
  { label: t('file.storage.default.isDefault'), value: 1 },
  { label: t('file.storage.default.notDefault'), value: 0 },
])
const enabledOptions = computed(() => [
  { label: t('file.storage.enabled.enabled'), value: 1 },
  { label: t('file.storage.enabled.disabled'), value: 0 },
])

function pickEnum<T>(value: unknown): T | undefined {
  return value === undefined || value === null || value === '' ? undefined : (value as T)
}

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('file.storage.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('file.storage.columns.keywordPlaceholder'), order: 0 },
  { key: 'configCode', title: t('file.storage.columns.configCode'), dataType: 'string', minWidth: 150, order: 1 },
  { key: 'configName', title: t('file.storage.columns.configName'), dataType: 'string', minWidth: 150, order: 2 },
  {
    key: 'storageType',
    title: t('file.storage.columns.storageType'),
    dataType: 'enum',
    searchable: true,
    options: storageTypeOptions.value,
    searchPlaceholder: t('file.storage.columns.storageTypePlaceholder'),
    width: 110,
    order: 3,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(storageTypeOptions.value, (row as unknown as StorageConfigListItemDto).storageType),
    ),
  },
  {
    key: 'bucketName',
    title: t('file.storage.columns.bucketName'),
    dataType: 'string',
    minWidth: 160,
    order: 4,
    render: (row) => {
      const r = row as unknown as StorageConfigListItemDto
      return r.bucketName
        ? h('span', r.bucketName)
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isDefault',
    title: t('file.storage.columns.isDefault'),
    dataType: 'boolean',
    searchable: true,
    options: defaultOptions.value,
    searchPlaceholder: t('file.storage.columns.isDefaultPlaceholder'),
    width: 90,
    order: 5,
    render: (row) => {
      const isDefault = (row as unknown as StorageConfigListItemDto).isDefault
      return isDefault
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'warning' }, () => t('file.storage.tag.default'))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('file.storage.columns.status'),
    dataType: 'boolean',
    searchable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('file.storage.columns.statusPlaceholder'),
    width: 90,
    order: 6,
    render: (row) => {
      const enabled = (row as unknown as StorageConfigListItemDto).isEnabled
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: enabled ? 'success' : 'error' },
        () => enabled ? t('file.storage.tag.enabled') : t('file.storage.tag.disabled'),
      )
    },
  },
  { key: 'sort', title: t('file.storage.columns.sort'), dataType: 'number', width: 80, order: 7 },
  { key: 'createdTime', title: t('file.storage.columns.createdTime'), dataType: 'datetime', minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'file.storage',
  exportPermission: 'saas:storage-config:export',
  pageName: t('file.storage.pageName'),
  statusPermission: 'saas:storage-config:status',
  rowKey: 'basicId',
  scrollX: 1200,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return storageConfigApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        isDefault: pickBoolean(f.isDefault),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        storageType: pickEnum<StorageConfigType>(f.storageType),
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => storageConfigApi.updateStatus({ basicId: id, isEnabled: enabled }),
  },
  actions: [
    { key: 'create', title: t('file.storage.actions.create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:storage-config:create' },
    { key: 'edit', title: t('file.storage.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'saas:storage-config:update' },
    { key: 'toggle', title: t('file.storage.actions.toggle'), scope: 'row', icon: 'lucide:power', permission: 'saas:storage-config:status' },
    {
      key: 'setDefault',
      title: t('file.storage.actions.setDefault'),
      scope: 'row',
      icon: 'lucide:star',
      permission: 'saas:storage-config:update',
      visible: row => !(row as unknown as StorageConfigListItemDto).isDefault,
    },
    {
      key: 'delete',
      title: t('file.storage.actions.delete'),
      scope: 'row',
      icon: 'lucide:trash-2',
      type: 'error',
      permission: 'saas:storage-config:delete',
      visible: row => !(row as unknown as StorageConfigListItemDto).isDefault,
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as StorageConfigListItemDto | undefined
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
const form = ref<StorageConfigFormModel>(createDefaultForm())

const modalTitle = computed(() => (form.value.basicId ? t('file.storage.form.editTitle') : t('file.storage.form.addTitle')))
const isObjectStorage = computed(() => form.value.storageType !== StorageConfigType.Local)
const secretPlaceholder = computed(() =>
  form.value.basicId && editingHasSecret.value ? t('file.storage.form.secretConfigured') : t('file.storage.form.secretPlaceholder'),
)

function createDefaultForm(): StorageConfigFormModel {
  return {
    accessKeyId: null,
    bucketName: null,
    configCode: '',
    configName: '',
    endpoint: null,
    isDefault: false,
    isEnabled: true,
    region: null,
    remark: null,
    secretAccessKey: null,
    sort: 100,
    storageType: StorageConfigType.Local,
  }
}

function handleAdd() {
  editingHasSecret.value = false
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: StorageConfigListItemDto) {
  try {
    const detail = await storageConfigApi.detail(row.basicId)
    if (!detail) {
      message.warning(t('file.storage.message.detailNotFound'))
      return
    }

    editingHasSecret.value = detail.hasSecretAccessKey
    form.value = {
      accessKeyId: detail.accessKeyId ?? null,
      basicId: detail.basicId,
      bucketName: detail.bucketName ?? null,
      configCode: detail.configCode,
      configName: detail.configName,
      endpoint: detail.endpoint ?? null,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      region: detail.region ?? null,
      remark: detail.remark ?? null,
      secretAccessKey: null,
      sort: detail.sort,
      storageType: detail.storageType,
    }
    modalVisible.value = true
  }
  catch (e) {
    message.error((e as Error).message || t('file.storage.message.loadDetailFailed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    message.warning(t('file.storage.message.inputConfigCode'))
    return false
  }

  if (!form.value.configName.trim()) {
    message.warning(t('file.storage.message.inputConfigName'))
    return false
  }

  if (isObjectStorage.value) {
    if (!form.value.bucketName?.trim()) {
      message.warning(t('file.storage.message.inputBucketName'))
      return false
    }

    if (!form.value.accessKeyId?.trim()) {
      message.warning(t('file.storage.message.inputAccessKeyId'))
      return false
    }

    if (!form.value.basicId && !form.value.secretAccessKey?.trim()) {
      message.warning(t('file.storage.message.inputSecretAccessKey'))
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
      await storageConfigApi.update({
        accessKeyId: form.value.accessKeyId,
        basicId: form.value.basicId,
        bucketName: form.value.bucketName,
        configName: form.value.configName.trim(),
        endpoint: form.value.endpoint,
        region: form.value.region,
        remark: form.value.remark,
        secretAccessKey: form.value.secretAccessKey?.trim() || null,
        sort: form.value.sort,
        storageType: form.value.storageType,
      })
    }
    else {
      await storageConfigApi.create({
        accessKeyId: form.value.accessKeyId,
        bucketName: form.value.bucketName,
        configCode: form.value.configCode.trim(),
        configName: form.value.configName.trim(),
        endpoint: form.value.endpoint,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        region: form.value.region,
        remark: form.value.remark,
        secretAccessKey: form.value.secretAccessKey?.trim() || null,
        sort: form.value.sort,
        storageType: form.value.storageType,
      })
    }

    message.success(t('file.storage.message.saveSuccess'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    message.error((e as Error).message || t('file.storage.message.saveFailed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: StorageConfigListItemDto) {
  const next = !row.isEnabled
  dialog.warning({
    title: next ? t('file.storage.message.enableTitle') : t('file.storage.message.disableTitle'),
    content: next
      ? t('file.storage.message.enableContent', { name: row.configName })
      : t('file.storage.message.disableContent', { name: row.configName }),
    positiveText: next ? t('file.storage.message.enable') : t('file.storage.message.disable'),
    negativeText: t('file.storage.form.cancel'),
    onPositiveClick: async () => {
      try {
        await storageConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        message.success(t('file.storage.message.statusUpdated'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('file.storage.message.statusUpdateFailed'))
      }
    },
  })
}

function handleSetDefault(row: StorageConfigListItemDto) {
  dialog.info({
    title: t('file.storage.message.setDefaultTitle'),
    content: t('file.storage.message.setDefaultContent', { name: row.configName }),
    positiveText: t('file.storage.message.setDefault'),
    negativeText: t('file.storage.form.cancel'),
    onPositiveClick: async () => {
      try {
        await storageConfigApi.setDefault({ basicId: row.basicId })
        message.success(t('file.storage.message.setDefaultSuccess'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('file.storage.message.setDefaultFailed'))
      }
    },
  })
}

function handleDelete(row: StorageConfigListItemDto) {
  dialog.warning({
    title: t('file.storage.message.deleteTitle'),
    content: t('file.storage.message.deleteContent', { name: row.configName }),
    positiveText: t('file.storage.message.delete'),
    negativeText: t('file.storage.form.cancel'),
    onPositiveClick: async () => {
      try {
        await storageConfigApi.delete(row.basicId)
        message.success(t('file.storage.message.deleteSuccess'))
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || t('file.storage.message.deleteFailed'))
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
          <NFormItem :label="t('file.storage.form.configCode')" path="configCode">
            <NInput
              v-model:value="form.configCode"
              clearable size="small"
              :disabled="Boolean(form.basicId)"
              :placeholder="t('file.storage.form.configCodePlaceholder')"
            />
          </NFormItem>
          <NFormItem :label="t('file.storage.form.configName')" path="configName">
            <NInput v-model:value="form.configName" clearable size="small" :placeholder="t('file.storage.form.configNamePlaceholder')" />
          </NFormItem>
          <NFormItem :label="t('file.storage.form.storageType')" path="storageType">
            <NSelect v-model:value="form.storageType" :options="storageTypeOptions" />
          </NFormItem>
          <NFormItem :label="t('file.storage.form.sort')" path="sort">
            <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
          </NFormItem>

          <template v-if="isObjectStorage">
            <NFormItem :label="t('file.storage.form.endpoint')" path="endpoint">
              <NInput v-model:value="form.endpoint" clearable size="small" :placeholder="t('file.storage.form.endpointPlaceholder')" />
            </NFormItem>
            <NFormItem :label="t('file.storage.form.region')" path="region">
              <NInput v-model:value="form.region" clearable size="small" :placeholder="t('file.storage.form.regionPlaceholder')" />
            </NFormItem>
            <NFormItem :label="t('file.storage.form.bucketName')" path="bucketName">
              <NInput v-model:value="form.bucketName" clearable size="small" :placeholder="t('file.storage.form.bucketNamePlaceholder')" />
            </NFormItem>
            <NFormItem :label="t('file.storage.form.accessKeyId')" path="accessKeyId">
              <NInput v-model:value="form.accessKeyId" clearable size="small" placeholder="AccessKeyId" />
            </NFormItem>
            <NFormItem :label="t('file.storage.form.secretAccessKey')" path="secretAccessKey" style="grid-column: span 2">
              <NInput
                v-model:value="form.secretAccessKey"
                size="small"
                type="password"
                show-password-on="click"
                :placeholder="secretPlaceholder"
              />
            </NFormItem>
          </template>
          <template v-else>
            <NFormItem :label="t('file.storage.form.rootPath')" path="bucketName" style="grid-column: span 2">
              <NInput v-model:value="form.bucketName" clearable size="small" :placeholder="t('file.storage.form.rootPathPlaceholder')" />
            </NFormItem>
          </template>

          <template v-if="!form.basicId">
            <NFormItem :label="t('file.storage.form.isEnabled')" path="isEnabled">
              <NSwitch v-model:value="form.isEnabled" />
            </NFormItem>
            <NFormItem :label="t('file.storage.form.isDefault')" path="isDefault">
              <NSwitch v-model:value="form.isDefault" :disabled="!form.isEnabled" />
            </NFormItem>
          </template>

          <NFormItem :label="t('file.storage.form.remark')" path="remark" style="grid-column: span 2">
            <NInput v-model:value="form.remark" clearable size="small" :placeholder="t('file.storage.form.remarkPlaceholder')" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            {{ t('file.storage.form.cancel') }}
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('file.storage.form.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>
