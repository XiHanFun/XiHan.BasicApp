<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { OAuthAppDetailDto, OAuthAppListItemDto } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, EnableStatus, oauthAppApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOAuthAppPage' })

interface OAuthAppGridResult {
  items: OAuthAppListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<OAuthAppListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<OAuthAppDetailDto | null>(null)

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
  return oauthAppApi
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
        width: 90,
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

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.appType = undefined
  queryParams.skipConsent = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDetail(row: OAuthAppListItemDto) {
  detailVisible.value = true
  detailLoading.value = true

  try {
    currentDetail.value = await oauthAppApi.detail(row.basicId)
  }
  catch {
    currentDetail.value = null
    message.error('加载 OAuth 应用详情失败')
  }
  finally {
    detailLoading.value = false
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
          <!-- 操作列仅图标 -->
          <NButton aria-label="详情" circle quaternary size="small" type="primary" @click="handleDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
          </NButton>
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
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
