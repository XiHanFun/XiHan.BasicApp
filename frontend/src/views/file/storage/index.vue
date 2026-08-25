<script setup lang="ts">
import type { StorageConfigListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, storageConfigApi, StorageConfigType } from '@/api'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const storageTypeOptions = computed(() => [
  { label: t('file.storage.storage_type.local'), value: StorageConfigType.Local },
  { label: t('file.storage.storage_type.s3'), value: StorageConfigType.S3 },
  { label: t('file.storage.storage_type.oss'), value: StorageConfigType.OSS },
  { label: t('file.storage.storage_type.cos'), value: StorageConfigType.COS },
  { label: t('file.storage.storage_type.minio'), value: StorageConfigType.MinIO },
])

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = computed(() => [
  { label: t('file.storage.default.is_default'), value: 1 },
  { label: t('file.storage.default.not_default'), value: 0 },
])
const enabledOptions = computed(() => [
  { label: t('file.storage.enabled.enabled'), value: 1 },
  { label: t('file.storage.enabled.disabled'), value: 0 },
])

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('file.storage.columns.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('file.storage.columns.keyword_placeholder'), order: 0 },
  { key: 'configCode', title: t('file.storage.columns.config_code'), dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'configName', title: t('file.storage.columns.config_name'), dataType: 'string', sortable: true, minWidth: 150, order: 2 },
  {
    key: 'storageType',
    title: t('file.storage.columns.storage_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: storageTypeOptions.value,
    searchPlaceholder: t('file.storage.columns.storage_type_placeholder'),
    width: 110,
    order: 3,
    render: row => h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'info' }, () => h(XhTagLabel, () => getOptionLabel(storageTypeOptions.value, (row as unknown as StorageConfigListItemDto).storageType))),
  },
  {
    key: 'bucketName',
    title: t('file.storage.columns.bucket_name'),
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
    title: t('file.storage.columns.is_default'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: defaultOptions.value,
    searchPlaceholder: t('file.storage.columns.is_default_placeholder'),
    width: 90,
    order: 5,
    render: (row) => {
      const isDefault = (row as unknown as StorageConfigListItemDto).isDefault
      return isDefault
        ? h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: 'warning' }, () => h(XhTagLabel, () => t('file.storage.tag.default')))
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: t('file.storage.columns.status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: enabledOptions.value,
    searchPlaceholder: t('file.storage.columns.status_placeholder'),
    width: 90,
    order: 6,
    render: (row) => {
      const enabled = (row as unknown as StorageConfigListItemDto).isEnabled
      return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => enabled ? t('file.storage.tag.enabled') : t('file.storage.tag.disabled')))
    },
  },
  { key: 'sort', title: t('file.storage.columns.sort'), dataType: 'number', sortable: true, width: 80, order: 7 },
  { key: 'createdTime', title: t('file.storage.columns.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'file.storage',
  exportPermission: 'saas:storage-config:export',
  pageName: t('file.storage.page_name'),
  statusPermission: 'saas:storage-config:status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return storageConfigApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(storageType) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        isDefault: pickBoolean(f.isDefault),
        isEnabled: pickBoolean(f.isEnabled),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        // storageType 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层 storageType 单值字段）
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
      title: t('file.storage.actions.set_default'),
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

const modalTitle = computed(() => (form.value.basicId ? t('file.storage.form.edit_title') : t('file.storage.form.add_title')))
const isObjectStorage = computed(() => form.value.storageType !== StorageConfigType.Local)
const secretPlaceholder = computed(() =>
  form.value.basicId && editingHasSecret.value ? t('file.storage.form.secret_configured') : t('file.storage.form.secret_placeholder'),
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
      toast.warning(t('file.storage.message.detail_not_found'))
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
    toast.error((e as Error).message || t('file.storage.message.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    toast.warning(t('file.storage.message.input_config_code'))
    return false
  }

  if (!form.value.configName.trim()) {
    toast.warning(t('file.storage.message.input_config_name'))
    return false
  }

  if (isObjectStorage.value) {
    if (!form.value.bucketName?.trim()) {
      toast.warning(t('file.storage.message.input_bucket_name'))
      return false
    }

    if (!form.value.accessKeyId?.trim()) {
      toast.warning(t('file.storage.message.input_access_key_id'))
      return false
    }

    if (!form.value.basicId && !form.value.secretAccessKey?.trim()) {
      toast.warning(t('file.storage.message.input_secret_access_key'))
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

    toast.success(t('file.storage.message.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    toast.error((e as Error).message || t('file.storage.message.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: StorageConfigListItemDto) {
  const next = !row.isEnabled
  void dialog.confirm({
    badge: 'warning',
    title: next ? t('file.storage.message.enable_title') : t('file.storage.message.disable_title'),
    content: next
      ? t('file.storage.message.enable_content', { name: row.configName })
      : t('file.storage.message.disable_content', { name: row.configName }),
    okText: next ? t('file.storage.message.enable') : t('file.storage.message.disable'),
    cancelText: t('file.storage.form.cancel'),
    onOk: async () => {
      try {
        await storageConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        toast.success(t('file.storage.message.status_updated'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('file.storage.message.status_update_failed'))
      }
    },
  })
}

function handleSetDefault(row: StorageConfigListItemDto) {
  void dialog.confirm({
    badge: 'info',
    title: t('file.storage.message.set_default_title'),
    content: t('file.storage.message.set_default_content', { name: row.configName }),
    okText: t('file.storage.message.set_default'),
    cancelText: t('file.storage.form.cancel'),
    onOk: async () => {
      try {
        await storageConfigApi.setDefault({ basicId: row.basicId })
        toast.success(t('file.storage.message.set_default_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('file.storage.message.set_default_failed'))
      }
    },
  })
}

function handleDelete(row: StorageConfigListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('file.storage.message.delete_title'),
    content: t('file.storage.message.delete_content', { name: row.configName }),
    okText: t('file.storage.message.delete'),
    cancelText: t('file.storage.form.cancel'),
    onOk: async () => {
      try {
        await storageConfigApi.delete(row.basicId)
        toast.success(t('file.storage.message.delete_success'))
        reloadList()
      }
      catch (e) {
        toast.error((e as Error).message || t('file.storage.message.delete_failed'))
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
            <XhFieldLabel>{{ t('file.storage.form.config_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.configCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('file.storage.form.config_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('file.storage.form.config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.configName" clearable :placeholder="t('file.storage.form.config_name_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="storageType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('file.storage.form.storage_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.storageType" :options="storageTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('file.storage.form.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>

        <template v-if="isObjectStorage">
          <XhFormFieldGroup value="endpoint">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.endpoint') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.endpoint" clearable :placeholder="t('file.storage.form.endpoint_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="region">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.region') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.region" clearable :placeholder="t('file.storage.form.region_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="bucketName">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.bucket_name') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.bucketName" clearable :placeholder="t('file.storage.form.bucket_name_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="accessKeyId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.access_key_id') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.accessKeyId" clearable placeholder="AccessKeyId" autocomplete="off" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="secretAccessKey" class="xh-span-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.secret_access_key') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="form.secretAccessKey"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="secretPlaceholder"
                />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>
        <template v-else>
          <XhFormFieldGroup value="bucketName" class="xh-span-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.root_path') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.bucketName" clearable :placeholder="t('file.storage.form.root_path_placeholder')" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <template v-if="!form.basicId">
          <XhFormFieldGroup value="isEnabled">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.is_enabled') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup value="isDefault">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('file.storage.form.is_default') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch v-model:checked="form.isDefault" :disabled="!form.isEnabled" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>

        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('file.storage.form.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :placeholder="t('file.storage.form.remark_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
