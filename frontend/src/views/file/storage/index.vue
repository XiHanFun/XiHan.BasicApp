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

const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

const storageTypeOptions = [
  { label: '本地存储', value: StorageConfigType.Local },
  { label: 'AWS S3', value: StorageConfigType.S3 },
  { label: '阿里云OSS', value: StorageConfigType.OSS },
  { label: '腾讯云COS', value: StorageConfigType.COS },
  { label: 'MinIO', value: StorageConfigType.MinIO },
]

// SchemaSelectOption.value 仅支持 string | number；布尔搜索项用 1/0，page() 里转回 boolean
const defaultOptions = [
  { label: '默认', value: 1 },
  { label: '非默认', value: 0 },
]
const enabledOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
]

function pickEnum<T>(value: unknown): T | undefined {
  return value === undefined || value === null || value === '' ? undefined : (value as T)
}

function pickBoolean(value: unknown): boolean | undefined {
  return value === undefined || value === null || value === '' ? undefined : Boolean(Number(value))
}

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序） ──
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索配置编码/名称/备注', order: 0 },
  { key: 'configCode', title: '配置编码', dataType: 'string', minWidth: 150, order: 1 },
  { key: 'configName', title: '配置名称', dataType: 'string', minWidth: 150, order: 2 },
  {
    key: 'storageType',
    title: '提供者类型',
    dataType: 'enum',
    searchable: true,
    options: storageTypeOptions,
    searchPlaceholder: '提供者类型',
    width: 110,
    order: 3,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(storageTypeOptions, (row as unknown as StorageConfigListItemDto).storageType),
    ),
  },
  {
    key: 'bucketName',
    title: 'Bucket / 根路径',
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
    title: '默认',
    dataType: 'boolean',
    searchable: true,
    options: defaultOptions,
    searchPlaceholder: '默认',
    width: 90,
    order: 5,
    render: (row) => {
      const isDefault = (row as unknown as StorageConfigListItemDto).isDefault
      return isDefault
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'warning' }, () => '默认')
        : h('span', { style: 'opacity:.45' }, '—')
    },
  },
  {
    key: 'isEnabled',
    title: '状态',
    dataType: 'boolean',
    searchable: true,
    options: enabledOptions,
    searchPlaceholder: '状态',
    width: 90,
    order: 6,
    render: (row) => {
      const enabled = (row as unknown as StorageConfigListItemDto).isEnabled
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: enabled ? 'success' : 'error' },
        () => enabled ? '启用' : '停用',
      )
    },
  },
  { key: 'sort', title: '排序', dataType: 'number', width: 80, order: 7 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 8 },
]

const schema: PageSchema = {
  pageCode: 'file.storage',
  exportPermission: 'saas:storage-config:export',
  pageName: '存储配置',
  rowKey: 'basicId',
  scrollX: 1200,
  fields,
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
  },
  actions: [
    { key: 'create', title: '新增配置', scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:storage-config:create' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pencil', permission: 'saas:storage-config:update' },
    { key: 'toggle', title: '启用/停用', scope: 'row', icon: 'lucide:power', permission: 'saas:storage-config:status' },
    {
      key: 'setDefault',
      title: '设为默认',
      scope: 'row',
      icon: 'lucide:star',
      permission: 'saas:storage-config:update',
      visible: row => !(row as unknown as StorageConfigListItemDto).isDefault,
    },
    {
      key: 'delete',
      title: '删除',
      scope: 'row',
      icon: 'lucide:trash-2',
      type: 'error',
      permission: 'saas:storage-config:delete',
      visible: row => !(row as unknown as StorageConfigListItemDto).isDefault,
    },
  ],
}

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

const modalTitle = computed(() => (form.value.basicId ? '编辑存储配置' : '新增存储配置'))
const isObjectStorage = computed(() => form.value.storageType !== StorageConfigType.Local)
const secretPlaceholder = computed(() =>
  form.value.basicId && editingHasSecret.value ? '已配置，留空则不修改' : '请输入访问密钥',
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
      message.warning('未查询到存储配置详情')
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
    message.error((e as Error).message || '加载存储配置详情失败')
  }
}

function validateForm() {
  if (!form.value.basicId && !form.value.configCode.trim()) {
    message.warning('请输入配置编码')
    return false
  }

  if (!form.value.configName.trim()) {
    message.warning('请输入配置名称')
    return false
  }

  if (isObjectStorage.value) {
    if (!form.value.bucketName?.trim()) {
      message.warning('请输入存储桶名称')
      return false
    }

    if (!form.value.accessKeyId?.trim()) {
      message.warning('请输入访问密钥ID')
      return false
    }

    if (!form.value.basicId && !form.value.secretAccessKey?.trim()) {
      message.warning('请输入访问密钥')
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

    message.success('保存成功')
    modalVisible.value = false
    reloadList()
  }
  catch (e) {
    message.error((e as Error).message || '保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

function handleToggleStatus(row: StorageConfigListItemDto) {
  const next = !row.isEnabled
  dialog.warning({
    title: next ? '启用存储配置' : '停用存储配置',
    content: next
      ? `确定启用存储配置「${row.configName}」吗？`
      : `确定停用存储配置「${row.configName}」吗？停用后该存储不可用。`,
    positiveText: next ? '启用' : '停用',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await storageConfigApi.updateStatus({ basicId: row.basicId, isEnabled: next })
        message.success('状态已更新')
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || '状态更新失败')
      }
    },
  })
}

function handleSetDefault(row: StorageConfigListItemDto) {
  dialog.info({
    title: '设为默认存储',
    content: `确定将「${row.configName}」设为默认存储吗？原默认存储配置将被取消默认。`,
    positiveText: '设为默认',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await storageConfigApi.setDefault({ basicId: row.basicId })
        message.success('已设为默认存储')
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || '设置默认存储失败')
      }
    },
  })
}

function handleDelete(row: StorageConfigListItemDto) {
  dialog.warning({
    title: '删除存储配置',
    content: `确定删除存储配置「${row.configName}」吗？已被文件存储记录引用的配置无法删除。`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await storageConfigApi.delete(row.basicId)
        message.success('删除成功')
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || '删除失败')
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
          <NFormItem label="配置编码" path="configCode">
            <NInput
              v-model:value="form.configCode"
              clearable size="small"
              :disabled="Boolean(form.basicId)"
              placeholder="如: oss-main（租户内唯一）"
            />
          </NFormItem>
          <NFormItem label="配置名称" path="configName">
            <NInput v-model:value="form.configName" clearable size="small" placeholder="请输入配置名称" />
          </NFormItem>
          <NFormItem label="提供者类型" path="storageType">
            <NSelect v-model:value="form.storageType" :options="storageTypeOptions" />
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
          </NFormItem>

          <template v-if="isObjectStorage">
            <NFormItem label="端点URL" path="endpoint">
              <NInput v-model:value="form.endpoint" clearable size="small" placeholder="S3 兼容接口地址，如 https://oss-cn-hangzhou.aliyuncs.com" />
            </NFormItem>
            <NFormItem label="区域" path="region">
              <NInput v-model:value="form.region" clearable size="small" placeholder="如 cn-hangzhou / us-east-1" />
            </NFormItem>
            <NFormItem label="存储桶名称" path="bucketName">
              <NInput v-model:value="form.bucketName" clearable size="small" placeholder="Bucket / Container 名称" />
            </NFormItem>
            <NFormItem label="访问密钥ID" path="accessKeyId">
              <NInput v-model:value="form.accessKeyId" clearable size="small" placeholder="AccessKeyId" />
            </NFormItem>
            <NFormItem label="访问密钥" path="secretAccessKey" style="grid-column: span 2">
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
            <NFormItem label="根路径" path="bucketName" style="grid-column: span 2">
              <NInput v-model:value="form.bucketName" clearable size="small" placeholder="本地存储根路径，留空使用系统默认" />
            </NFormItem>
          </template>

          <template v-if="!form.basicId">
            <NFormItem label="是否启用" path="isEnabled">
              <NSwitch v-model:value="form.isEnabled" />
            </NFormItem>
            <NFormItem label="设为默认" path="isDefault">
              <NSwitch v-model:value="form.isDefault" :disabled="!form.isEnabled" />
            </NFormItem>
          </template>

          <NFormItem label="备注" path="remark" style="grid-column: span 2">
            <NInput v-model:value="form.remark" clearable size="small" placeholder="请输入备注" />
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
