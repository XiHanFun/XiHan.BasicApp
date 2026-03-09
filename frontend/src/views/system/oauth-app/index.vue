<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysOAuthApp } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import { createOAuthAppApi, deleteOAuthAppApi, getOAuthAppPageApi, updateOAuthAppApi } from '@/api'
import { DEFAULT_PAGE_SIZE, OAUTH_APP_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOAuthAppPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysOAuthApp[]>([])
const total = ref(0)

const queryParams = reactive({
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  keyword: '',
  appType: undefined as number | undefined,
  status: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增 OAuth 应用')
const submitLoading = ref(false)

const formData = ref<Partial<SysOAuthApp>>({
  appName: '',
  appDescription: '',
  clientId: '',
  clientSecret: '',
  appType: 0,
  grantTypes: 'authorization_code',
  redirectUris: '',
  scopes: '',
  accessTokenLifetime: 3600,
  refreshTokenLifetime: 2592000,
  authorizationCodeLifetime: 300,
  skipConsent: false,
  status: 1,
  remark: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getOAuthAppPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  } catch {
    message.error('获取 OAuth 应用列表失败')
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增 OAuth 应用'
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
    refreshTokenLifetime: 2592000,
    authorizationCodeLifetime: 300,
    skipConsent: false,
    status: 1,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysOAuthApp) {
  modalTitle.value = '编辑 OAuth 应用'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteOAuthAppApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateOAuthAppApi(formData.value.basicId, formData.value)
    } else {
      await createOAuthAppApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysOAuthApp> = [
  {
    title: '应用名称',
    key: 'appName',
    width: 180,
  },
  {
    title: '客户端ID',
    key: 'clientId',
    width: 200,
    ellipsis: { tooltip: true },
  },
  {
    title: '应用类型',
    key: 'appType',
    width: 110,
    render: (row) => getOptionLabel(OAUTH_APP_TYPE_OPTIONS, row.appType),
  },
  {
    title: '授权类型',
    key: 'grantTypes',
    width: 180,
    ellipsis: { tooltip: true },
  },
  {
    title: '跳过授权确认',
    key: 'skipConsent',
    width: 130,
    render: (row) =>
      h(
        NTag,
        { type: row.skipConsent ? 'success' : 'default', size: 'small', round: true },
        { default: () => (row.skipConsent ? '是' : '否') },
      ),
  },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '创建时间',
    key: 'createTime',
    width: 170,
    render: (row) => formatDate(row.createTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: (row) =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () => [
            h(
              NButton,
              {
                size: 'small',
                type: 'primary',
                ghost: true,
                onClick: () => handleEdit(row),
              },
              { default: () => '编辑' },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDelete(row.basicId),
              },
              {
                default: () => '确认删除该应用？',
                trigger: () =>
                  h(
                    NButton,
                    { size: 'small', type: 'error', ghost: true },
                    { default: () => '删除' },
                  ),
              },
            ),
          ],
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索应用名称/客户端ID"
          style="width: 240px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.appType"
          :options="OAUTH_APP_TYPE_OPTIONS"
          placeholder="全部类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="全部状态"
          style="width: 120px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton
          @click="
            () => {
              queryParams.keyword = ''
              queryParams.appType = undefined
              queryParams.status = undefined
              queryParams.page = 1
              fetchData()
            }
          "
        >
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增应用
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.basicId"
        :pagination="false"
        :scroll-x="1250"
        size="small"
        striped
      />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="queryParams.page"
          v-model:page-size="queryParams.pageSize"
          :item-count="total"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="fetchData"
          @update:page-size="
            () => {
              queryParams.page = 1
              fetchData()
            }
          "
        />
      </div>
    </NCard>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 680px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="120px">
        <NFormItem label="应用名称" path="appName">
          <NInput v-model:value="formData.appName" placeholder="请输入应用名称" />
        </NFormItem>
        <NFormItem label="应用描述" path="appDescription">
          <NInput
            v-model:value="formData.appDescription"
            type="textarea"
            :rows="2"
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="客户端ID" path="clientId">
          <NInput
            v-model:value="formData.clientId"
            :disabled="!!formData.basicId"
            placeholder="请输入客户端ID"
          />
        </NFormItem>
        <NFormItem label="客户端密钥" path="clientSecret">
          <NInput v-model:value="formData.clientSecret" placeholder="请输入客户端密钥" />
        </NFormItem>
        <NFormItem label="应用类型" path="appType">
          <NSelect v-model:value="formData.appType" :options="OAUTH_APP_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="授权类型" path="grantTypes">
          <NInput v-model:value="formData.grantTypes" placeholder="如: authorization_code,refresh_token" />
        </NFormItem>
        <NFormItem label="重定向地址" path="redirectUris">
          <NInput v-model:value="formData.redirectUris" placeholder="多个地址用逗号分隔" />
        </NFormItem>
        <NFormItem label="权限范围" path="scopes">
          <NInput v-model:value="formData.scopes" placeholder="如: openid,profile,email" />
        </NFormItem>
        <NFormItem label="AccessToken时长" path="accessTokenLifetime">
          <NInputNumber
            v-model:value="formData.accessTokenLifetime"
            :min="60"
            :max="86400"
            class="w-full"
          />
        </NFormItem>
        <NFormItem label="RefreshToken时长" path="refreshTokenLifetime">
          <NInputNumber
            v-model:value="formData.refreshTokenLifetime"
            :min="300"
            :max="2592000"
            class="w-full"
          />
        </NFormItem>
        <NFormItem label="授权码时长" path="authorizationCodeLifetime">
          <NInputNumber
            v-model:value="formData.authorizationCodeLifetime"
            :min="60"
            :max="3600"
            class="w-full"
          />
        </NFormItem>
        <NFormItem label="跳过授权确认" path="skipConsent">
          <NSwitch v-model:value="formData.skipConsent" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">确认</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
