<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { OAuthAppDetailDto, OAuthAppListItemDto, OAuthAppSecretDto } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { appManagementApi, createPageRequest, EnableStatus } from '@/api'
import { Icon } from '~/components'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformAppPage' })

const message = useMessage()
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppDetailDto | null>(null)
const actionLoading = ref(false)
const secretVisible = ref(false)
const currentSecret = ref<OAuthAppSecretDto | null>(null)
const tableLoading = ref(false)
const dataList = ref<OAuthAppListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await appManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
        },
      }),
      appType: queryParams.appType,
      keyword: queryParams.keyword.trim() || null,
      skipConsent: toOptionalBoolean(queryParams.skipConsent),
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询 OAuth 应用失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const tableColumns = computed<DataTableColumns<OAuthAppListItemDto>>(() => [
  { key: 'appName', title: '应用名称', minWidth: 160, ellipsis: { tooltip: true }, sorter: true },
  { key: 'clientId', title: 'Client ID', minWidth: 220, ellipsis: { tooltip: true } },
  {
    key: 'appType',
    title: '应用类型',
    minWidth: 110,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(OAUTH_APP_TYPE_OPTIONS, row.appType))
    },
  },
  { key: 'grantTypes', title: '授权类型', minWidth: 180, ellipsis: { tooltip: true } },
  { key: 'scopes', title: '权限范围', minWidth: 160, ellipsis: { tooltip: true } },
  {
    key: 'accessTokenLifetime',
    title: '访问令牌',
    minWidth: 130,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatSeconds(Number(row.accessTokenLifetime || 0)))
    },
  },
  {
    key: 'skipConsent',
    title: '授权确认',
    width: 110,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.skipConsent ? 'warning' : 'default', bordered: false }, () => row.skipConsent ? '跳过' : '确认')
    },
  },
  {
    key: 'status',
    title: '状态',
    width: 82,
    render(row) {
      return h(NTag, { size: 'small', round: true, type: row.status === EnableStatus.Enabled ? 'success' : 'error', bordered: false }, () => row.status === EnableStatus.Enabled ? '启用' : '禁用')
    },
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 170,
    render(row) {
      return h(NSpace, { size: 4 }, () => [
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleDetail(row) }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })),
            }),
          default: () => '详情',
        }),
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '启停',
              circle: true,
              quaternary: true,
              size: 'small',
              type: row.status === EnableStatus.Enabled ? 'warning' : 'success',
              onClick: () => handleToggleStatus(row),
            }, {
              icon: () => h(NIcon, { icon: row.status === EnableStatus.Enabled ? 'lucide:pause' : 'lucide:play' }),
            }),
          default: () => row.status === EnableStatus.Enabled ? '停用' : '启用',
        }),
        h(NPopconfirm, { onPositiveClick: () => handleDelete(row) }, {
          trigger: () =>
            h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, {
              icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })),
            }),
          default: () => '确定删除该 OAuth 应用？删除后不可恢复。',
        }),
      ])
    },
  },
])

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.appType = undefined
  queryParams.skipConsent = undefined
  queryParams.status = undefined
  currentPage.value = 1
  fetchData()
}

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
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
    fetchData()
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
    fetchData()
  }
  catch {
    message.error('删除应用失败')
  }
  finally {
    actionLoading.value = false
  }
}

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
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
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页</div>
        <NPagination :page="currentPage" :page-count="totalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="pageSize" show-size-picker @update:page="handlePageChange" @update:page-size="handlePageSizeChange" />
      </div>
    </NCard>

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
