<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { OAuthAppDetailDto, OAuthAppListItemDto, OAuthAppSecretDto } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { appManagementApi, createPageRequest, EnableStatus } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformAppPage' })

interface OAuthAppGridResult {
  items: OAuthAppListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<OAuthAppListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppDetailDto | null>(null)
const actionLoading = ref(false)
const secretVisible = ref(false)
const currentSecret = ref<OAuthAppSecretDto | null>(null)

const queryParams = reactive({
  appType: undefined as OAuthAppListItemDto['appType'] | undefined,
  keyword: '',
  skipConsent: undefined as number | undefined,
  status: undefined as EnableStatus | undefined,
})

const consentOptions = [
  { label: '跳过确认', value: 1 },
  { label: '需要确认', value: 0 },
]

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
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

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<OAuthAppGridResult> {
  return appManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      appType: queryParams.appType,
      keyword: queryParams.keyword.trim() || null,
      skipConsent: toOptionalBoolean(queryParams.skipConsent),
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询 OAuth 应用失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<OAuthAppListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'appName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '应用名称' },
      { field: 'clientId', minWidth: 220, showOverflow: 'tooltip', title: 'Client ID' },
      {
        field: 'appType',
        formatter: ({ cellValue }) => getOptionLabel(OAUTH_APP_TYPE_OPTIONS, cellValue),
        minWidth: 110,
        title: '应用类型',
      },
      { field: 'grantTypes', minWidth: 180, showOverflow: 'tooltip', title: '授权类型' },
      { field: 'scopes', minWidth: 160, showOverflow: 'tooltip', title: '权限范围' },
      {
        field: 'accessTokenLifetime',
        formatter: ({ cellValue }) => formatSeconds(Number(cellValue || 0)),
        minWidth: 130,
        title: '访问令牌',
      },
      {
        field: 'skipConsent',
        slots: { default: 'col_skip_consent' },
        title: '授权确认',
        width: 110,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 82,
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 170,
      },
    ],
    id: 'sys_oauth_app',
    name: 'OAuth 应用',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function reload() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reload()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.appType = undefined
  queryParams.skipConsent = undefined
  queryParams.status = undefined
  reload()
}

async function handleDetail(row: OAuthAppListItemDto) {
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

async function handleRegenerateSecret() {
  if (!currentDetail.value) return
  actionLoading.value = true
  try {
    currentSecret.value = await appManagementApi.regenerateSecret(currentDetail.value.basicId)
    secretVisible.value = true
    message.success('密钥已重新生成')
  }
  catch {
    message.error('重新生成密钥失败')
  }
  finally {
    actionLoading.value = false
  }
}

function copySecret() {
  if (!currentSecret.value?.clientSecret) return
  navigator.clipboard.writeText(currentSecret.value.clientSecret).then(() => {
    message.success('密钥已复制到剪贴板')
  }).catch(() => {
    message.error('复制失败')
  })
}

async function handleToggleStatus(row: OAuthAppListItemDto) {
  const newStatus = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  actionLoading.value = true
  try {
    await appManagementApi.updateStatus({
      basicId: row.basicId,
      status: newStatus,
    })
    message.success(newStatus === EnableStatus.Enabled ? '应用已启用' : '应用已停用')
    reload()
  }
  catch {
    message.error('更新状态失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDelete(row: OAuthAppListItemDto) {
  actionLoading.value = true
  try {
    await appManagementApi.delete(row.basicId)
    message.success('应用已删除')
    reload()
  }
  catch {
    message.error('删除应用失败')
  }
  finally {
    actionLoading.value = false
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索应用名称/Client ID"
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.appType"
          :options="OAUTH_APP_TYPE_OPTIONS"
          clearable
          placeholder="应用类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.skipConsent"
          :options="consentOptions"
          clearable
          placeholder="授权确认"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          clearable
          placeholder="状态"
          style="width: 110px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons />
        <template #empty>
          <div class="py-12 text-center text-gray-400">
            暂无 OAuth 应用数据
          </div>
        </template>

        <template #col_skip_consent="{ row }">
          <NTag :type="row.skipConsent ? 'warning' : 'default'" round size="small">
            {{ row.skipConsent ? '跳过' : '确认' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '启用' : '禁用' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace :size="4">
            <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>
            <NButton
              aria-label="启停"
              circle
              quaternary
              size="small"
              :type="row.status === EnableStatus.Enabled ? 'warning' : 'success'"
              @click="handleToggleStatus(row)"
            >
              <template #icon>
                <NIcon :icon="row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" />
              </template>
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton
                  aria-label="删除"
                  circle
                  quaternary
                  size="small"
                  type="error"
                  :loading="actionLoading"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确定删除该 OAuth 应用？删除后不可恢复。
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="560">
      <NDrawerContent closable title="OAuth 应用详情">
        <NSpace v-if="detailLoading" justify="center">
          加载中...
        </NSpace>

        <NDescriptions v-else-if="currentDetail" :column="1" bordered size="small">
          <NDescriptionsItem label="应用名称">
            {{ currentDetail.appName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="应用描述">
            {{ currentDetail.appDescription || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="Client ID">
            {{ currentDetail.clientId }}
          </NDescriptionsItem>
          <NDescriptionsItem label="应用类型">
            {{ getOptionLabel(OAUTH_APP_TYPE_OPTIONS, currentDetail.appType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="授权类型">
            {{ currentDetail.grantTypes || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="权限范围">
            {{ currentDetail.scopes || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="回调地址">
            {{ currentDetail.redirectUris || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="主页">
            {{ currentDetail.homepage || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label=" Logo">
            {{ currentDetail.logo || '-' }}
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
            {{ currentDetail.status === EnableStatus.Enabled ? '启用' : '禁用' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ currentDetail.remark || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(currentDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ currentDetail.modifiedTime ? formatDate(currentDetail.modifiedTime) : '-' }}
          </NDescriptionsItem>
        </NDescriptions>

        <div v-else class="py-8 text-center text-gray-400">
          暂无应用详情
        </div>

        <template v-if="currentDetail && !detailLoading" #footer>
          <NSpace justify="end">
            <NButton
              type="primary"
              :loading="actionLoading"
              @click="handleRegenerateSecret"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:key-round" /></NIcon>
              </template>
              重新生成密钥
            </NButton>
            <NButton
              :type="currentDetail.status === EnableStatus.Enabled ? 'warning' : 'success'"
              :loading="actionLoading"
              @click="handleToggleStatus(currentDetail)"
            >
              <template #icon>
                <NIcon :icon="currentDetail.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play'" />
              </template>
              {{ currentDetail.status === EnableStatus.Enabled ? '停用' : '启用' }}
            </NButton>
            <NPopconfirm @positive-click="handleDelete(currentDetail); detailVisible = false">
              <template #trigger>
                <NButton type="error" :loading="actionLoading">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确定删除该 OAuth 应用？
            </NPopconfirm>
          </NSpace>
        </template>
      </NDrawerContent>
    </NDrawer>

    <NDrawer v-model:show="secretVisible" :width="420">
      <NDrawerContent closable title="客户端密钥">
        <NSpace v-if="currentSecret" vertical>
          <NDescriptions :column="1" bordered size="small">
            <NDescriptionsItem label="Client ID">
              {{ currentSecret.clientId }}
            </NDescriptionsItem>
            <NDescriptionsItem label="Client Secret">
              <div class="relative">
                <div class="break-all font-mono text-sm bg-gray-50 dark:bg-gray-800 p-3 rounded">
                  {{ currentSecret.clientSecret }}
                </div>
              </div>
            </NDescriptionsItem>
          </NDescriptions>
          <div class="text-xs text-gray-400 mt-2">
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
  </div>
</template>
