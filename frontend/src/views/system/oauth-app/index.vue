<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysOAuthApp } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { oauthAppApi } from '@/api'
import { OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOAuthAppPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  appType: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return oauthAppApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    appType: queryParams.appType,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysOAuthApp>({
  id: 'sys_oauth_app',
  name: 'OAuth 应用',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'appName', title: '应用名称', minWidth: 160, showOverflow: 'tooltip', sortable: true },
    { field: 'clientId', title: 'Client ID', minWidth: 200, showOverflow: 'tooltip' },
    {
      field: 'appType',
      title: '应用类型',
      width: 110,
      formatter: ({ cellValue }) => getOptionLabel(OAUTH_APP_TYPE_OPTIONS, cellValue),
    },
    { field: 'grantTypes', title: '授权类型', minWidth: 160, showOverflow: 'tooltip' },
    { field: 'accessTokenLifetime', title: 'Token有效期(s)', width: 140 },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    { field: 'createTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      title: '操作',
      width: 140,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: { query: ({ page }) => handleQueryApi(page) },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}
function handleReset() {
  queryParams.keyword = ''
  queryParams.appType = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增应用')
const submitLoading = ref(false)
const formData = ref<Partial<SysOAuthApp>>({})

function resetForm() {
  formData.value = {
    appName: '',
    appDescription: '',
    clientId: '',
    clientSecret: '',
    appType: 0,
    grantTypes: 'authorization_code',
    redirectUris: '',
    scopes: '',
    accessTokenLifetime: 3600,
    refreshTokenLifetime: 86400,
    authorizationCodeLifetime: 600,
    skipConsent: false,
    status: 1,
  }
}
function handleAdd() {
  modalTitle.value = '新增应用'
  resetForm()
  modalVisible.value = true
}
function handleEdit(row: SysOAuthApp) {
  modalTitle.value = '编辑应用'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await oauthAppApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId)
      await oauthAppApi.update(formData.value.basicId, formData.value)
    else await oauthAppApi.create(formData.value)
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索应用名称/ClientID" clearable style="width: 280px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.appType" :options="OAUTH_APP_TYPE_OPTIONS" placeholder="应用类型" clearable style="width: 130px" />
        <NSelect v-model:value="queryParams.status" :options="STATUS_OPTIONS" placeholder="状态" clearable style="width: 120px" />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd">
            新增应用
          </NButton>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该应用？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 640px" :auto-focus="false">
      <NForm :model="formData" label-placement="left" label-width="120px">
        <NFormItem label="应用名称" path="appName">
          <NInput v-model:value="formData.appName" placeholder="请输入应用名称" />
        </NFormItem>
        <NFormItem label="应用描述" path="appDescription">
          <NInput v-model:value="formData.appDescription" type="textarea" :rows="2" placeholder="应用描述" />
        </NFormItem>
        <NFormItem label="Client ID" path="clientId">
          <NInput v-model:value="formData.clientId" placeholder="客户端标识" />
        </NFormItem>
        <NFormItem label="Client Secret" path="clientSecret">
          <NInput v-model:value="formData.clientSecret" type="password" show-password-on="click" placeholder="客户端密钥" />
        </NFormItem>
        <NFormItem label="应用类型" path="appType">
          <NSelect v-model:value="formData.appType" :options="OAUTH_APP_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="授权类型" path="grantTypes">
          <NInput v-model:value="formData.grantTypes" placeholder="如: authorization_code,refresh_token" />
        </NFormItem>
        <NFormItem label="回调地址" path="redirectUris">
          <NInput v-model:value="formData.redirectUris" type="textarea" :rows="2" placeholder="回调URL（多个用逗号分隔）" />
        </NFormItem>
        <NFormItem label="Token有效期(s)">
          <NInputNumber v-model:value="formData.accessTokenLifetime" :min="60" style="width: 100%" />
        </NFormItem>
        <NFormItem label="跳过授权确认">
          <NSwitch v-model:value="formData.skipConsent" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
