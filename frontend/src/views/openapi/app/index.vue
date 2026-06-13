<script setup lang="ts">
import type {
  OAuthAppCreateDto,
  OAuthAppDetailDto,
  OAuthAppListItemDto,
  OAuthAppSecretDto,
  OAuthAppUpdateDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { appManagementApi, createPageRequest, EnableStatus, OAuthAppType } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformAppPage' })

interface AppFormModel {
  accessTokenLifetime: number
  appDescription?: string | null
  appName: string
  appType: OAuthAppType
  authorizationCodeLifetime: number
  basicId?: string
  clientId: string
  grantTypes: string
  homepage?: string | null
  logo?: string | null
  redirectUris?: string | null
  refreshTokenLifetime: number
  remark?: string | null
  scopes: string
  skipConsent: boolean
  status: EnableStatus
}

const message = useMessage()
const statusOptions = STATUS_OPTIONS
const appTypeOptions = OAUTH_APP_TYPE_OPTIONS

// 布尔筛选项：SchemaSelectOption.value 仅支持 string|number，用 1/0 表示真假
const consentOptions = [
  { label: '跳过确认', value: 1 },
  { label: '需要确认', value: 0 },
]

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadApp() {
  void schemaPageRef.value?.reload()
}

function formatSeconds(seconds: number) {
  if (seconds < 60) {
    return `${seconds} 秒`
  }
  if (seconds < 3600) {
    return `${Math.floor(seconds / 60)} 分钟`
  }
  if (seconds < 86400) {
    return `${Math.floor(seconds / 3600)} 小时`
  }
  return `${Math.floor(seconds / 86400)} 天`
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

// ── 字段单一事实源 ──────────────────────────────────────────────
const fields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索应用名称/Client ID', width: 250, order: 0 },
  { key: 'appName', title: '应用名称', dataType: 'string', sortable: true, minWidth: 160, order: 1 },
  { key: 'clientId', title: 'Client ID', dataType: 'string', minWidth: 220, order: 2 },
  {
    key: 'appType',
    title: '应用类型',
    dataType: 'enum',
    searchable: true,
    options: appTypeOptions,
    searchPlaceholder: '应用类型',
    minWidth: 110,
    order: 3,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(appTypeOptions, (row as unknown as OAuthAppListItemDto).appType)),
  },
  { key: 'grantTypes', title: '授权类型', dataType: 'string', minWidth: 180, order: 4 },
  { key: 'scopes', title: '权限范围', dataType: 'string', minWidth: 160, order: 5 },
  {
    key: 'accessTokenLifetime',
    title: '访问令牌',
    dataType: 'number',
    minWidth: 130,
    order: 6,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatSeconds(Number((row as unknown as OAuthAppListItemDto).accessTokenLifetime || 0))),
  },
  {
    key: 'skipConsent',
    title: '授权确认',
    dataType: 'boolean',
    searchable: true,
    options: consentOptions,
    searchPlaceholder: '授权确认',
    width: 110,
    order: 7,
    render: row => h(NTag, { size: 'small', round: true, type: (row as unknown as OAuthAppListItemDto).skipConsent ? 'warning' : 'default', bordered: false }, () => (row as unknown as OAuthAppListItemDto).skipConsent ? '跳过' : '确认'),
  },
  {
    key: 'status',
    title: '状态',
    dataType: 'enum',
    searchable: true,
    options: statusOptions,
    searchPlaceholder: '状态',
    width: 90,
    order: 8,
  },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 9 },
]

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema: PageSchema = {
  pageCode: 'platform.app',
  exportPermission: 'saas:oauth-app:export',
  pageName: '应用管理',
  batchRemovable: true,
  removePermission: 'saas:oauth-app:delete',
  rowKey: 'basicId',
  scrollX: 1700,
  fields,
  resource: {
    page: (params) => {
      const { keyword, appType, skipConsent, status } = params.filters
      return appManagementApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        // appType 为字符串枚举（option.value 直接是 OAuthAppType），透传即可
        appType: (appType as OAuthAppType | undefined) ?? null,
        keyword: (keyword as string | undefined)?.trim() || null,
        // skipConsent 用 1/0 数值选项表示布尔，清洗为 boolean
        skipConsent: skipConsent === undefined || skipConsent === null || skipConsent === '' ? null : Boolean(Number(skipConsent)),
        // status 为数值枚举（option.value 直接是 EnableStatus），透传即可
        status: (status as EnableStatus | undefined) ?? null,
      }) as unknown as Promise<import('@/api').PageResult<Record<string, unknown>>>
    },
    remove: id => appManagementApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增应用', scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'view', title: '查看详情', scope: 'row' },
    { key: 'edit', title: '编辑', scope: 'row' },
    { key: 'toggle', title: '启用/停用', scope: 'row' },
    { key: 'secret', title: '重置密钥', scope: 'row' },
    { key: 'delete', title: '删除', scope: 'row' },
  ],
}

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as OAuthAppListItemDto | undefined
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
    case 'secret':
      if (row) {
        void handleRegenerateSecret(row.basicId)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppDetailDto | null>(null)

// ── 密钥抽屉 ────────────────────────────────────────────────────
const secretVisible = ref(false)
const currentSecret = ref<OAuthAppSecretDto | null>(null)

// ── 新增/编辑弹窗 ───────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const appForm = ref<AppFormModel>(createDefaultForm())
const modalTitle = computed(() => (appForm.value.basicId ? '编辑应用' : '新增应用'))

function createDefaultForm(): AppFormModel {
  return {
    accessTokenLifetime: 3600,
    appDescription: null,
    appName: '',
    appType: OAuthAppType.Web,
    authorizationCodeLifetime: 600,
    clientId: '',
    grantTypes: '',
    homepage: null,
    logo: null,
    redirectUris: null,
    refreshTokenLifetime: 2592000,
    remark: null,
    scopes: '',
    skipConsent: false,
    status: EnableStatus.Enabled,
  }
}

async function handleView(row: OAuthAppListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await appManagementApi.detail(row.basicId)
  }
  catch {
    currentDetail.value = null
    message.error('加载 OAuth 应用详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

function handleAdd() {
  appForm.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: OAuthAppListItemDto) {
  try {
    const detail = await appManagementApi.detail(row.basicId)
    if (!detail) {
      message.error('加载应用详情失败')
      return
    }
    appForm.value = {
      accessTokenLifetime: detail.accessTokenLifetime,
      appDescription: detail.appDescription ?? null,
      appName: detail.appName,
      appType: detail.appType,
      authorizationCodeLifetime: detail.authorizationCodeLifetime,
      basicId: detail.basicId,
      clientId: detail.clientId,
      grantTypes: detail.grantTypes,
      homepage: detail.homepage ?? null,
      logo: detail.logo ?? null,
      redirectUris: detail.redirectUris ?? null,
      refreshTokenLifetime: detail.refreshTokenLifetime,
      remark: detail.remark ?? null,
      scopes: detail.scopes ?? '',
      skipConsent: detail.skipConsent,
      status: detail.status,
    }
    modalVisible.value = true
  }
  catch {
    message.error('加载应用详情失败')
  }
}

function validateForm() {
  if (!appForm.value.appName.trim()) {
    message.warning('请输入应用名称')
    return false
  }
  if (!appForm.value.basicId && !appForm.value.clientId.trim()) {
    message.warning('请输入 Client ID')
    return false
  }
  if (!appForm.value.grantTypes.trim()) {
    message.warning('请输入授权类型')
    return false
  }
  if (!appForm.value.scopes.trim()) {
    message.warning('请输入权限范围')
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
    if (appForm.value.basicId) {
      const updateInput: OAuthAppUpdateDto = {
        accessTokenLifetime: appForm.value.accessTokenLifetime,
        appDescription: appForm.value.appDescription,
        appName: appForm.value.appName.trim(),
        appType: appForm.value.appType,
        authorizationCodeLifetime: appForm.value.authorizationCodeLifetime,
        basicId: appForm.value.basicId,
        grantTypes: appForm.value.grantTypes.trim(),
        homepage: appForm.value.homepage,
        logo: appForm.value.logo,
        redirectUris: appForm.value.redirectUris,
        refreshTokenLifetime: appForm.value.refreshTokenLifetime,
        remark: appForm.value.remark,
        scopes: appForm.value.scopes.trim(),
        skipConsent: appForm.value.skipConsent,
      }
      await appManagementApi.update(updateInput)
      message.success('保存成功')
    }
    else {
      const createInput: OAuthAppCreateDto = {
        accessTokenLifetime: appForm.value.accessTokenLifetime,
        appDescription: appForm.value.appDescription,
        appName: appForm.value.appName.trim(),
        appType: appForm.value.appType,
        authorizationCodeLifetime: appForm.value.authorizationCodeLifetime,
        // ClientId 为后端必填、不自动生成（领域服务对空值抛错并做唯一性校验），由前端录入；ClientSecret 留空由后端生成
        clientId: appForm.value.clientId.trim(),
        grantTypes: appForm.value.grantTypes.trim(),
        homepage: appForm.value.homepage,
        logo: appForm.value.logo,
        redirectUris: appForm.value.redirectUris,
        refreshTokenLifetime: appForm.value.refreshTokenLifetime,
        remark: appForm.value.remark,
        scopes: appForm.value.scopes.trim(),
        skipConsent: appForm.value.skipConsent,
        status: appForm.value.status,
      }
      // 新增成功返回客户端密钥，弹出密钥抽屉（仅显示一次）
      const secret = await appManagementApi.create(createInput)
      message.success('保存成功')
      if (secret) {
        currentSecret.value = secret
        secretVisible.value = true
      }
    }
    modalVisible.value = false
    reloadApp()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleRegenerateSecret(id: string) {
  try {
    currentSecret.value = await appManagementApi.regenerateSecret(id)
    secretVisible.value = true
    message.success('密钥已重新生成')
  }
  catch {
    message.error('重新生成密钥失败')
  }
}

function copySecret() {
  if (!currentSecret.value?.clientSecret) {
    return
  }
  navigator.clipboard.writeText(currentSecret.value.clientSecret).then(() => {
    message.success('密钥已复制到剪贴板')
  }).catch(() => {
    message.error('复制失败')
  })
}

async function handleToggleStatus(row: OAuthAppListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await appManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? '应用已启用' : '应用已停用')
    reloadApp()
  }
  catch {
    message.error('更新状态失败')
  }
}

async function handleDelete(row: OAuthAppListItemDto) {
  try {
    await appManagementApi.delete(row.basicId)
    message.success('应用已删除')
    reloadApp()
  }
  catch {
    message.error('删除应用失败')
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情抽屉 -->
    <NDrawer v-model:show="detailVisible" :width="560">
      <NDrawerContent closable title="OAuth 应用详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无应用详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 180px)">
            <NDescriptions :column="1" bordered size="small">
              <NDescriptionsItem label="应用名称">
                {{ currentDetail.appName }}
              </NDescriptionsItem>
              <NDescriptionsItem label="应用描述">
                {{ formatNullable(currentDetail.appDescription) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="Client ID">
                {{ currentDetail.clientId }}
              </NDescriptionsItem>
              <NDescriptionsItem label="应用类型">
                {{ getOptionLabel(appTypeOptions, currentDetail.appType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="授权类型">
                {{ formatNullable(currentDetail.grantTypes) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="权限范围">
                {{ formatNullable(currentDetail.scopes) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="回调地址">
                {{ formatNullable(currentDetail.redirectUris) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="主页">
                {{ formatNullable(currentDetail.homepage) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="Logo">
                {{ formatNullable(currentDetail.logo) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="访问令牌有效期">
                {{ formatSeconds(currentDetail.accessTokenLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="刷新令牌有效期">
                {{ formatSeconds(currentDetail.refreshTokenLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="授权码有效期">
                {{ formatSeconds(currentDetail.authorizationCodeLifetime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="授权确认">
                {{ currentDetail.skipConsent ? '跳过授权确认' : '需要授权确认' }}
              </NDescriptionsItem>
              <NDescriptionsItem label="状态">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="备注">
                {{ formatNullable(currentDetail.remark) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建时间">
                {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="修改时间">
                {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
            </NDescriptions>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增/编辑弹窗 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="appForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="应用名称" path="appName">
          <NInput v-model:value="appForm.appName" clearable placeholder="请输入应用名称" />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" label="Client ID" path="clientId">
          <NInput v-model:value="appForm.clientId" clearable placeholder="请输入 Client ID" />
        </NFormItem>
        <NFormItem v-else label="Client ID" path="clientId">
          <NInput v-model:value="appForm.clientId" disabled />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" label="应用类型" path="appType">
          <NSelect v-model:value="appForm.appType" :options="appTypeOptions" />
        </NFormItem>
        <NFormItem label="授权类型" path="grantTypes">
          <NInput v-model:value="appForm.grantTypes" clearable placeholder="如: authorization_code,refresh_token" />
        </NFormItem>
        <NFormItem label="权限范围" path="scopes">
          <NInput v-model:value="appForm.scopes" clearable placeholder="如: openid,profile,email" />
        </NFormItem>
        <NFormItem label="回调地址" path="redirectUris">
          <NInput v-model:value="appForm.redirectUris" clearable placeholder="多个以逗号分隔" />
        </NFormItem>
        <NFormItem label="主页" path="homepage">
          <NInput v-model:value="appForm.homepage" clearable placeholder="请输入主页地址" />
        </NFormItem>
        <NFormItem label="Logo" path="logo">
          <NInput v-model:value="appForm.logo" clearable placeholder="请输入 Logo 地址" />
        </NFormItem>
        <NFormItem label="访问令牌有效期(秒)" path="accessTokenLifetime">
          <NInputNumber v-model:value="appForm.accessTokenLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="刷新令牌有效期(秒)" path="refreshTokenLifetime">
          <NInputNumber v-model:value="appForm.refreshTokenLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="授权码有效期(秒)" path="authorizationCodeLifetime">
          <NInputNumber v-model:value="appForm.authorizationCodeLifetime" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="跳过授权确认" path="skipConsent">
          <NSwitch v-model:value="appForm.skipConsent" />
        </NFormItem>
        <NFormItem v-if="!appForm.basicId" label="状态" path="status">
          <NSelect v-model:value="appForm.status" :options="statusOptions" />
        </NFormItem>
        <NFormItem label="应用描述" path="appDescription">
          <NInput
            v-model:value="appForm.appDescription"
            clearable
            placeholder="请输入应用描述"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="appForm.remark"
            clearable
            placeholder="请输入备注"
            :rows="2"
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

    <!-- 密钥抽屉 -->
    <NDrawer v-model:show="secretVisible" :width="420">
      <NDrawerContent closable title="客户端密钥">
        <NSpace v-if="currentSecret" vertical>
          <NDescriptions :column="1" bordered size="small">
            <NDescriptionsItem label="Client ID">
              {{ currentSecret.clientId }}
            </NDescriptionsItem>
            <NDescriptionsItem label="Client Secret">
              <div class="p-3 font-mono text-sm break-all bg-gray-50 rounded dark:bg-gray-800">
                {{ currentSecret.clientSecret }}
              </div>
            </NDescriptionsItem>
          </NDescriptions>
          <div class="mt-2 text-xs text-gray-400">
            密钥仅显示一次，请妥善保管。丢失后需重新生成。
          </div>
          <NButton block type="primary" @click="copySecret">
            <template #icon>
              <NIcon><Icon icon="lucide:copy" /></NIcon>
            </template>
            复制密钥
          </NButton>
          <NButton block @click="secretVisible = false">
            关闭
          </NButton>
        </NSpace>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
