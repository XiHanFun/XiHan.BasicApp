<script setup lang="ts">
import type {
  ApiId,
  PageResult,
  TenantEditionCreateDto,
  TenantEditionListItemDto,
  TenantEditionPermissionListItemDto,
  TenantEditionUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaSelectOption } from '~/components'
import {
  NButton,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  permissionApi,
  tenantEditionApi,
  tenantEditionPermissionApi,
  ValidityStatus,
} from '@/api'
import { SchemaPage } from '~/components'
import { PERMISSION_TYPE_OPTIONS, STATUS_OPTIONS, VALIDITY_STATUS_OPTIONS } from '~/constants'
import { usePermission } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'TenantEditionPage' })

interface EditionFormModel extends TenantEditionCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()

const statusOptions = STATUS_OPTIONS
const validityStatusOptions = VALIDITY_STATUS_OPTIONS
const permissionTypeOptions = PERMISSION_TYPE_OPTIONS

const boolOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索版本编码/名称', width: 240, order: 0 },
  { key: 'editionCode', title: '版本编码', dataType: 'string', minWidth: 140, order: 1 },
  {
    key: 'editionName',
    title: '版本名称',
    dataType: 'string',
    minWidth: 150,
    order: 2,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('div', { style: 'display:flex;flex-direction:column;line-height:1.35' }, [
        h('span', { style: 'font-weight:500' }, r.editionName),
        r.description ? h('span', { style: 'font-size:12px;opacity:.65' }, r.description) : null,
      ])
    },
  },
  {
    key: 'price',
    title: '价格',
    dataType: 'money',
    width: 110,
    order: 3,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      if (r.isFree) {
        return h(NTag, { size: 'small', round: true, bordered: false, type: 'success' }, () => '免费')
      }
      return h('span', r.price == null ? '-' : `¥ ${r.price}`)
    },
  },
  {
    key: 'billingPeriodMonths',
    title: '计费周期',
    dataType: 'number',
    width: 100,
    order: 4,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.billingPeriodMonths == null ? '-' : `${r.billingPeriodMonths} 个月`)
    },
  },
  {
    key: 'userLimit',
    title: '用户上限',
    dataType: 'number',
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.userLimit == null ? '不限' : String(r.userLimit))
    },
  },
  {
    key: 'storageLimit',
    title: '存储上限',
    dataType: 'number',
    width: 110,
    order: 6,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h('span', r.storageLimit == null ? '不限' : `${r.storageLimit} MB`)
    },
  },
  {
    key: 'isFree',
    title: '免费',
    dataType: 'boolean',
    searchable: true,
    options: boolOptions,
    searchPlaceholder: '是否免费',
    width: 86,
    order: 7,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isFree ? 'success' : 'default' }, () => (r.isFree ? '是' : '否'))
    },
  },
  {
    key: 'isDefault',
    title: '默认',
    dataType: 'boolean',
    searchable: true,
    options: boolOptions,
    searchPlaceholder: '是否默认',
    width: 86,
    order: 8,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return r.isDefault
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'warning' }, () => '默认')
        : h('span', { style: 'opacity:.45' }, '-')
    },
  },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 90,
    order: 9,
    render: (row) => {
      const r = row as unknown as TenantEditionListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' },
        () => getOptionLabel(statusOptions, r.status),
      )
    },
  },
  { key: 'sort', title: '排序', dataType: 'number', sortable: true, width: 80, order: 10 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
]

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

function toBool(v: unknown): boolean | undefined {
  if (v === undefined || v === null || v === '') {
    return undefined
  }
  return Boolean(v)
}

const schema: PageSchema = {
  pageCode: 'tenant.edition',
  exportPermission: 'saas:tenant-edition:export',
  pageName: '版本套餐',
  rowKey: 'basicId',
  scrollX: 1500,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return tenantEditionApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword) ?? null,
        status: (f.status as EnableStatus | undefined) ?? undefined,
        isFree: toBool(f.isFree) ?? null,
        isDefault: toBool(f.isDefault) ?? null,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    { key: 'create', title: '新增版本', scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:tenant-edition:create' },
    { key: 'edit', title: '编辑', scope: 'row', icon: 'lucide:pencil', permission: 'saas:tenant-edition:update' },
    {
      key: 'enable',
      title: '启用',
      scope: 'row',
      type: 'success',
      icon: 'lucide:play',
      permission: 'saas:tenant-edition:status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Disabled,
    },
    {
      key: 'disable',
      title: '停用',
      scope: 'row',
      type: 'warning',
      icon: 'lucide:pause',
      permission: 'saas:tenant-edition:status',
      visible: row => (row as unknown as TenantEditionListItemDto).status === EnableStatus.Enabled,
    },
    {
      key: 'set-default',
      title: '设为默认',
      scope: 'row',
      icon: 'lucide:star',
      permission: 'saas:tenant-edition:default',
      visible: row => !(row as unknown as TenantEditionListItemDto).isDefault,
    },
    { key: 'permissions', title: '版本权限', scope: 'row', icon: 'lucide:shield-check', permission: 'saas:tenant-edition-permission:read' },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as TenantEditionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'enable':
      if (row) {
        confirmToggleStatus(row, EnableStatus.Enabled)
      }
      break
    case 'disable':
      if (row) {
        confirmToggleStatus(row, EnableStatus.Disabled)
      }
      break
    case 'set-default':
      if (row) {
        confirmSetDefault(row)
      }
      break
    case 'permissions':
      if (row) {
        openPermissionDrawer(row)
      }
      break
  }
}

// ── 新增/编辑 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editionForm = ref<EditionFormModel>(createDefaultForm())
const modalTitle = computed(() => (editionForm.value.basicId ? '编辑版本' : '新增版本'))

function createDefaultForm(): EditionFormModel {
  return {
    billingPeriodMonths: null,
    description: null,
    editionCode: '',
    editionName: '',
    isDefault: false,
    isFree: false,
    price: null,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
    storageLimit: null,
    userLimit: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function handleAdd() {
  editionForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: TenantEditionListItemDto) {
  editionForm.value = {
    basicId: row.basicId,
    billingPeriodMonths: row.billingPeriodMonths ?? null,
    description: row.description ?? null,
    editionCode: row.editionCode,
    editionName: row.editionName,
    isDefault: row.isDefault,
    isFree: row.isFree,
    price: row.price ?? null,
    remark: null,
    sort: row.sort,
    status: row.status,
    storageLimit: row.storageLimit ?? null,
    userLimit: row.userLimit ?? null,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!editionForm.value.basicId && !editionForm.value.editionCode.trim()) {
    message.warning('请输入版本编码')
    return false
  }
  if (!editionForm.value.editionName.trim()) {
    message.warning('请输入版本名称')
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
    if (editionForm.value.basicId) {
      const updateInput: TenantEditionUpdateDto = {
        basicId: editionForm.value.basicId,
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }
      await tenantEditionApi.update(updateInput)
    }
    else {
      const createInput: TenantEditionCreateDto = {
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionCode: editionForm.value.editionCode.trim(),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        status: editionForm.value.status,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }
      await tenantEditionApi.create(createInput)
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

// ── 启停/设为默认 ───────────────────────────────────────────────
function confirmToggleStatus(row: TenantEditionListItemDto, next: EnableStatus) {
  const enabling = next === EnableStatus.Enabled
  dialog.warning({
    title: enabling ? '启用版本' : '停用版本',
    content: `确定${enabling ? '启用' : '停用'}版本「${row.editionName}」吗？${enabling ? '' : '停用后新租户将无法选择该版本。'}`,
    positiveText: enabling ? '启用' : '停用',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await tenantEditionApi.updateStatus({ basicId: row.basicId, status: next })
        message.success(enabling ? '已启用' : '已停用')
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || '状态更新失败')
      }
    },
  })
}

function confirmSetDefault(row: TenantEditionListItemDto) {
  dialog.warning({
    title: '设为默认版本',
    content: `确定将「${row.editionName}」设为默认版本吗？原默认版本将被取消默认标记。`,
    positiveText: '设为默认',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await tenantEditionApi.updateDefault({ basicId: row.basicId })
        message.success('已设为默认版本')
        reloadList()
      }
      catch (e) {
        message.error((e as Error).message || '设置默认版本失败')
      }
    },
  })
}

// ── 版本权限抽屉 ────────────────────────────────────────────────
const canGrantPermission = computed(() => hasPermission('saas:tenant-edition-permission:grant'))
const canRevokePermission = computed(() => hasPermission('saas:tenant-edition-permission:revoke'))
const canUpdateMapping = computed(() => hasPermission('saas:tenant-edition-permission:update'))

const permDrawerVisible = ref(false)
const permLoading = ref(false)
const permError = ref(false)
const permEdition = ref<TenantEditionListItemDto | null>(null)
const permList = ref<TenantEditionPermissionListItemDto[]>([])
const permActingId = ref<ApiId | null>(null)

const grantPermissionId = ref<ApiId | null>(null)
const grantOptionsLoading = ref(false)
const grantSubmitLoading = ref(false)
const grantOptions = ref<SchemaSelectOption<string>[]>([])

const grantedPermissionIds = computed(() => new Set(permList.value.map(item => String(item.permissionId))))
const grantSelectOptions = computed(() =>
  grantOptions.value.filter(option => !grantedPermissionIds.value.has(String(option.value))),
)

function openPermissionDrawer(row: TenantEditionListItemDto) {
  permEdition.value = row
  permList.value = []
  grantPermissionId.value = null
  permDrawerVisible.value = true
  void loadPermissionList()
  if (canGrantPermission.value) {
    void loadGrantOptions('')
  }
}

async function loadPermissionList() {
  if (!permEdition.value) {
    return
  }
  permLoading.value = true
  permError.value = false
  try {
    permList.value = await tenantEditionPermissionApi.list(permEdition.value.basicId)
  }
  catch {
    permError.value = true
    permList.value = []
    message.error('加载版本权限失败')
  }
  finally {
    permLoading.value = false
  }
}

async function loadGrantOptions(keyword: string) {
  grantOptionsLoading.value = true
  try {
    const items = await permissionApi.availableGlobal({ limit: 50, keyword: keyword.trim() || null })
    grantOptions.value = items.map(item => ({
      label: `${item.permissionName} (${item.permissionCode})`,
      value: String(item.basicId),
    }))
  }
  catch (e) {
    message.error((e as Error).message || '加载权限候选失败')
  }
  finally {
    grantOptionsLoading.value = false
  }
}

async function handleGrant() {
  if (!permEdition.value || !grantPermissionId.value) {
    return
  }
  grantSubmitLoading.value = true
  try {
    await tenantEditionPermissionApi.grant({
      editionId: permEdition.value.basicId,
      permissionId: grantPermissionId.value,
    })
    message.success('授予成功')
    grantPermissionId.value = null
    await loadPermissionList()
  }
  catch (e) {
    message.error((e as Error).message || '授予失败')
  }
  finally {
    grantSubmitLoading.value = false
  }
}

async function handleToggleMappingStatus(item: TenantEditionPermissionListItemDto) {
  const next = item.status === ValidityStatus.Valid ? ValidityStatus.Invalid : ValidityStatus.Valid
  permActingId.value = item.basicId
  try {
    await tenantEditionPermissionApi.updateStatus({ basicId: item.basicId, status: next })
    message.success(next === ValidityStatus.Valid ? '已启用' : '已停用')
    await loadPermissionList()
  }
  catch (e) {
    message.error((e as Error).message || '状态更新失败')
  }
  finally {
    permActingId.value = null
  }
}

function confirmRevoke(item: TenantEditionPermissionListItemDto) {
  dialog.warning({
    title: '撤销权限',
    content: `确定从版本「${permEdition.value?.editionName ?? ''}」撤销权限「${item.permissionName ?? item.permissionCode ?? item.permissionId}」吗？`,
    positiveText: '撤销',
    negativeText: '取消',
    onPositiveClick: async () => {
      permActingId.value = item.basicId
      try {
        await tenantEditionPermissionApi.revoke(item.basicId)
        message.success('已撤销')
        await loadPermissionList()
      }
      catch (e) {
        message.error((e as Error).message || '撤销失败')
      }
      finally {
        permActingId.value = null
      }
    },
  })
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
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
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="editionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="版本编码" path="editionCode">
          <NInput
            v-model:value="editionForm.editionCode"
            :disabled="Boolean(editionForm.basicId)"
            clearable
            placeholder="如: pro"
          />
        </NFormItem>
        <NFormItem label="版本名称" path="editionName">
          <NInput v-model:value="editionForm.editionName" clearable placeholder="请输入版本名称" />
        </NFormItem>
        <NFormItem label="价格" path="price">
          <NInputNumber
            v-model:value="editionForm.price"
            :disabled="editionForm.isFree"
            :min="0"
            :precision="2"
            clearable
            placeholder="留空表示未定价"
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="计费周期(月)" path="billingPeriodMonths">
          <NInputNumber
            v-model:value="editionForm.billingPeriodMonths"
            :min="1"
            :precision="0"
            clearable
            placeholder="如: 12"
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="用户上限" path="userLimit">
          <NInputNumber
            v-model:value="editionForm.userLimit"
            :min="0"
            :precision="0"
            clearable
            placeholder="留空不限"
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="存储上限(MB)" path="storageLimit">
          <NInputNumber
            v-model:value="editionForm.storageLimit"
            :min="0"
            :precision="0"
            clearable
            placeholder="留空不限"
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="是否免费" path="isFree">
          <NSwitch v-model:value="editionForm.isFree" />
        </NFormItem>
        <NFormItem label="设为默认版本" path="isDefault">
          <NSwitch v-model:value="editionForm.isDefault" />
        </NFormItem>
        <NFormItem v-if="!editionForm.basicId" label="状态" path="status">
          <NSelect v-model:value="editionForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="editionForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="editionForm.description"
            :rows="2"
            clearable
            placeholder="请输入版本描述"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="editionForm.remark"
            :rows="2"
            clearable
            placeholder="请输入备注"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NDrawer v-model:show="permDrawerVisible" :width="760">
      <NDrawerContent :title="`版本权限 — ${permEdition?.editionName ?? ''}`" closable>
        <div v-if="canGrantPermission" class="perm-grant">
          <NSelect
            v-model:value="grantPermissionId"
            :loading="grantOptionsLoading"
            :options="grantSelectOptions"
            clearable
            filterable
            placeholder="搜索全局权限（名称/编码）"
            remote
            @search="loadGrantOptions"
          />
          <NButton
            :disabled="!grantPermissionId"
            :loading="grantSubmitLoading"
            type="primary"
            @click="handleGrant"
          >
            授予
          </NButton>
        </div>

        <NSpin :show="permLoading">
          <div v-if="permError" class="xh-detail-empty">
            <NEmpty description="加载版本权限失败">
              <template #extra>
                <NButton size="small" @click="loadPermissionList">
                  重试
                </NButton>
              </template>
            </NEmpty>
          </div>
          <NEmpty
            v-else-if="!permLoading && permList.length === 0"
            class="xh-detail-empty"
            description="该版本暂无权限白名单"
          />
          <table v-else class="xh-detail-table">
            <thead>
              <tr>
                <th>权限</th>
                <th>模块</th>
                <th>类型</th>
                <th>映射状态</th>
                <th>授予时间</th>
                <th v-if="canUpdateMapping || canRevokePermission">
                  操作
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in permList" :key="item.basicId">
                <td>
                  <div class="perm-cell">
                    <span class="perm-cell__name">{{ item.permissionName ?? '-' }}</span>
                    <span class="perm-cell__code">{{ formatNullable(item.permissionCode) }}</span>
                  </div>
                </td>
                <td>{{ formatNullable(item.moduleCode) }}</td>
                <td>
                  <NTag v-if="item.permissionType" :bordered="false" round size="small" type="info">
                    {{ getOptionLabel(permissionTypeOptions, item.permissionType) }}
                  </NTag>
                  <span v-else>-</span>
                </td>
                <td>
                  <NTag :type="item.status === ValidityStatus.Valid ? 'success' : 'error'" round size="small">
                    {{ getOptionLabel(validityStatusOptions, item.status) }}
                  </NTag>
                </td>
                <td>{{ formatDate(item.createdTime) }}</td>
                <td v-if="canUpdateMapping || canRevokePermission">
                  <NSpace size="small">
                    <NButton
                      v-if="canUpdateMapping"
                      :loading="permActingId === item.basicId"
                      size="tiny"
                      type="warning"
                      @click="handleToggleMappingStatus(item)"
                    >
                      {{ item.status === ValidityStatus.Valid ? '停用' : '启用' }}
                    </NButton>
                    <NButton
                      v-if="canRevokePermission"
                      :loading="permActingId === item.basicId"
                      size="tiny"
                      type="error"
                      @click="confirmRevoke(item)"
                    >
                      撤销
                    </NButton>
                  </NSpace>
                </td>
              </tr>
            </tbody>
          </table>
        </NSpin>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.perm-grant {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

.perm-grant :deep(.n-select) {
  flex: 1;
}

.perm-cell {
  display: flex;
  flex-direction: column;
  line-height: 1.35;
}

.perm-cell__name {
  font-weight: 500;
}

.perm-cell__code {
  font-size: 12px;
  opacity: 0.65;
}

.xh-detail-empty {
  padding: 48px 0;
}

.xh-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.xh-detail-table th,
.xh-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.xh-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}
</style>
