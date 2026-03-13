<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysOAuthApp } from '~/types'
import {
  NButton,
  NCard,
  NDataTable,
  NDivider,
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
import {
  createOAuthAppApi,
  deleteOAuthAppApi,
  getOAuthAppDetailApi,
  getOAuthAppOpenApiSecurityApi,
  getOAuthAppPageApi,
  updateOAuthAppApi,
  updateOAuthAppOpenApiSecurityApi,
} from '@/api'
import {
  DEFAULT_PAGE_SIZE,
  OAUTH_APP_TYPE_OPTIONS,
  OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS,
  OPENAPI_ENCRYPT_ALGORITHM_OPTIONS,
  OPENAPI_SIGNATURE_ALGORITHM_OPTIONS,
  STATUS_OPTIONS,
} from '~/constants'
import { Icon } from '~/iconify'
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
const modalTitle = ref('新增 OpenAPI 客户端')
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
  openApiSecurityEnabled: true,
  openApiSignatureAlgorithm: 'HMACSHA256',
  openApiContentSignAlgorithm: 'SHA256',
  openApiEncryptionAlgorithm: 'AES-CBC',
  openApiEncryptKey: '',
  openApiPublicKey: '',
  openApiSm2PublicKey: '',
  openApiAllowResponseEncryption: true,
  openApiIpWhitelist: '',
  remark: '',
})

async function fetchData() {
  try {
    loading.value = true
    const result = await getOAuthAppPageApi(queryParams)
    tableData.value = result.items
    total.value = result.total
  }
  catch {
    message.error('获取 OpenAPI 客户端列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd() {
  modalTitle.value = '新增 OpenAPI 客户端'
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
    openApiSecurityEnabled: true,
    openApiSignatureAlgorithm: 'HMACSHA256',
    openApiContentSignAlgorithm: 'SHA256',
    openApiEncryptionAlgorithm: 'AES-CBC',
    openApiEncryptKey: '',
    openApiPublicKey: '',
    openApiSm2PublicKey: '',
    openApiAllowResponseEncryption: true,
    openApiIpWhitelist: '',
    remark: '',
  }
  modalVisible.value = true
}

async function handleEdit(row: SysOAuthApp) {
  modalTitle.value = '编辑 OpenAPI 客户端'
  try {
    submitLoading.value = true
    const [detail, security] = await Promise.all([
      getOAuthAppDetailApi(row.basicId),
      getOAuthAppOpenApiSecurityApi(row.basicId),
    ])
    formData.value = { ...detail, ...security }
    modalVisible.value = true
  }
  catch {
    message.error('获取客户端详情失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(id: string) {
  try {
    await deleteOAuthAppApi(id)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    let saved: SysOAuthApp | undefined
    if (formData.value.basicId) {
      saved = await updateOAuthAppApi(formData.value.basicId, formData.value)
    }
    else {
      saved = await createOAuthAppApi(formData.value)
    }

    const appId = saved?.basicId ?? formData.value.basicId
    if (!appId) {
      throw new Error('未获取到应用ID')
    }
    await updateOAuthAppOpenApiSecurityApi(appId, formData.value)
    message.success('保存成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysOAuthApp> = [
  {
    title: '客户端名称',
    key: 'appName',
    width: 180,
  },
  {
    title: 'AccessKey',
    key: 'clientId',
    width: 200,
    ellipsis: { tooltip: true },
  },
  {
    title: '客户端类型',
    key: 'appType',
    width: 110,
    render: row => getOptionLabel(OAUTH_APP_TYPE_OPTIONS, row.appType),
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
    render: row =>
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
    render: row =>
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
    render: row => formatDate(row.createTime ?? ''),
  },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    fixed: 'right',
    render: row =>
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
                default: () => '确认删除该客户端？',
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
          placeholder="搜索客户端名称/AccessKey"
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
          新增客户端
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
      style="width: 760px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="120px">
        <NFormItem label="客户端名称" path="appName">
          <NInput v-model:value="formData.appName" placeholder="请输入客户端名称" />
        </NFormItem>
        <NFormItem label="应用描述" path="appDescription">
          <NInput
            v-model:value="formData.appDescription"
            type="textarea"
            :rows="2"
            placeholder="可选"
          />
        </NFormItem>
        <NFormItem label="AccessKey" path="clientId">
          <NInput
            v-model:value="formData.clientId"
            :disabled="!!formData.basicId"
            placeholder="请输入 AccessKey"
          />
        </NFormItem>
        <NFormItem label="SecretKey" path="clientSecret">
          <NInput v-model:value="formData.clientSecret" placeholder="请输入 SecretKey" />
        </NFormItem>
        <NFormItem label="客户端类型" path="appType">
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
        <NDivider title-placement="left">
          OpenAPI 安全策略
        </NDivider>
        <NFormItem label="启用安全" path="openApiSecurityEnabled">
          <NSwitch v-model:value="formData.openApiSecurityEnabled" />
        </NFormItem>
        <NFormItem label="签名算法" path="openApiSignatureAlgorithm">
          <NSelect
            v-model:value="formData.openApiSignatureAlgorithm"
            :options="OPENAPI_SIGNATURE_ALGORITHM_OPTIONS"
          />
        </NFormItem>
        <NFormItem label="内容签名" path="openApiContentSignAlgorithm">
          <NSelect
            v-model:value="formData.openApiContentSignAlgorithm"
            :options="OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS"
          />
        </NFormItem>
        <NFormItem label="加密算法" path="openApiEncryptionAlgorithm">
          <NSelect
            v-model:value="formData.openApiEncryptionAlgorithm"
            :options="OPENAPI_ENCRYPT_ALGORITHM_OPTIONS"
          />
        </NFormItem>
        <NFormItem label="加密密钥" path="openApiEncryptKey">
          <NInput
            v-model:value="formData.openApiEncryptKey"
            type="textarea"
            :rows="2"
            placeholder="留空则默认复用 SecretKey。AES 建议 16/24/32 位，BLOWFISH 建议 8~56 位。"
          />
        </NFormItem>
        <NFormItem v-if="formData.openApiSignatureAlgorithm === 'RSASHA256'" label="RSA 公钥" path="openApiPublicKey">
          <NInput
            v-model:value="formData.openApiPublicKey"
            type="textarea"
            :rows="4"
            placeholder="PEM/Base64 格式公钥，仅 RSASHA256 需要"
          />
        </NFormItem>
        <NFormItem v-if="formData.openApiSignatureAlgorithm === 'SM2'" label="SM2 公钥" path="openApiSm2PublicKey">
          <NInput
            v-model:value="formData.openApiSm2PublicKey"
            type="textarea"
            :rows="4"
            placeholder="SM2 公钥，仅 SM2 签名需要"
          />
        </NFormItem>
        <NFormItem label="响应加密" path="openApiAllowResponseEncryption">
          <NSwitch v-model:value="formData.openApiAllowResponseEncryption" />
        </NFormItem>
        <NFormItem label="IP 白名单" path="openApiIpWhitelist">
          <NInput
            v-model:value="formData.openApiIpWhitelist"
            type="textarea"
            :rows="3"
            placeholder="多个 IP 用逗号、分号或换行分隔；留空表示不限制"
          />
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
