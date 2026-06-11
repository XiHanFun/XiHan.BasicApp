<script lang="ts" setup>
import type { ApiCredentialItem, ApiCredentialSecret } from '~/types'
import {
  NAlert,
  NButton,
  NEmpty,
  NIcon,
  NInput,
  NInputGroup,
  NModal,
  NSelect,
  NSpin,
  NSwitch,
  NTag,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { copyToClipboard, formatDate } from '~/utils'

const { apis } = useAppContext()
const message = useMessage()
const dialog = useDialog()

// ── API 凭证 ─────────────────────────────────────────────────────
const credentialsLoading = ref(false)
const credentials = ref<ApiCredentialItem[]>([])
/** 一次性密钥（仅创建/滚动后展示，离开页面即不可再见） */
const newSecret = ref<ApiCredentialSecret | null>(null)

const createModalVisible = ref(false)
const createSubmitting = ref(false)
const createName = ref('')

async function loadCredentials() {
  credentialsLoading.value = true
  try {
    credentials.value = await apis.getApiCredentialsApi()
  }
  catch (e) {
    message.error((e as Error).message || '加载 API 凭证失败')
  }
  finally {
    credentialsLoading.value = false
  }
}

function openCreateModal() {
  createName.value = ''
  createModalVisible.value = true
}

async function handleCreateCredential() {
  createSubmitting.value = true
  try {
    newSecret.value = await apis.createApiCredentialApi(createName.value.trim() || undefined)
    createModalVisible.value = false
    message.success('凭证已创建，请立即保存 Secret')
    await loadCredentials()
  }
  catch (e) {
    message.error((e as Error).message || '创建凭证失败')
  }
  finally {
    createSubmitting.value = false
  }
}

function handleRotateSecret(cred: ApiCredentialItem) {
  dialog.warning({
    title: '滚动密钥',
    content: `生成新密钥后「${cred.credentialName}」的旧密钥将立即失效，确定继续？`,
    positiveText: '确认滚动',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        newSecret.value = await apis.rotateApiCredentialSecretApi(cred.basicId)
        message.success('密钥已滚动，请立即保存新 Secret')
        await loadCredentials()
      }
      catch (e) {
        message.error((e as Error).message || '滚动密钥失败')
      }
    },
  })
}

async function handleToggleStatus(cred: ApiCredentialItem, enabled: boolean) {
  try {
    await apis.updateApiCredentialStatusApi(cred.basicId, enabled ? 'Enabled' : 'Disabled')
    message.success(enabled ? '凭证已启用' : '凭证已停用')
    await loadCredentials()
  }
  catch (e) {
    message.error((e as Error).message || '更新凭证状态失败')
    await loadCredentials()
  }
}

function handleDeleteCredential(cred: ApiCredentialItem) {
  dialog.error({
    title: '删除凭证',
    content: `删除后使用「${cred.credentialName}」（${cred.appKey}）的所有集成将立即失效，此操作不可恢复。`,
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await apis.deleteApiCredentialApi(cred.basicId)
        if (newSecret.value?.basicId === cred.basicId) {
          newSecret.value = null
        }
        message.success('凭证已删除')
        await loadCredentials()
      }
      catch (e) {
        message.error((e as Error).message || '删除凭证失败')
      }
    },
  })
}

function copyText(text: string) {
  void copyToClipboard(text).then(() => message.success('已复制'))
}

// ── 调用安全设置（签名算法 + IP 白名单，按 用户 × 设置键 持久化） ──
const OPENAPI_SETTING_SCENE = 0 // UserSettingScene.Preference
const OPENAPI_SETTING_KEY = 'developer.openapi'

interface OpenApiSettings { signAlgorithm: string, ipWhitelist: string }

const settingsSaving = ref(false)
const signAlgorithm = ref('HmacSHA256')
const ipWhitelist = ref('')
const signAlgorithmOptions = [
  { label: 'HmacSHA256', value: 'HmacSHA256' },
  { label: 'HmacSHA512', value: 'HmacSHA512' },
  { label: 'RSA-SHA256', value: 'RSA-SHA256' },
]

async function loadOpenApiSettings() {
  try {
    const result = await apis.userSettingApi.get({ scene: OPENAPI_SETTING_SCENE, settingKey: OPENAPI_SETTING_KEY })
    if (result.settingValue) {
      const parsed = JSON.parse(result.settingValue) as Partial<OpenApiSettings>
      signAlgorithm.value = parsed.signAlgorithm || 'HmacSHA256'
      ipWhitelist.value = parsed.ipWhitelist || ''
    }
  }
  catch {
    // 设置缺省即默认值，加载失败不打扰
  }
}

async function handleSaveOpenApiSettings() {
  // 简单校验：每行一个 IP / CIDR
  const invalid = ipWhitelist.value
    .split('\n')
    .map(line => line.trim())
    .filter(Boolean)
    .find(line => !/^\d{1,3}(\.\d{1,3}){3}(\/\d{1,2})?$/.test(line))
  if (invalid) {
    message.error(`IP 白名单格式无效：${invalid}`)
    return
  }

  settingsSaving.value = true
  try {
    const payload: OpenApiSettings = { signAlgorithm: signAlgorithm.value, ipWhitelist: ipWhitelist.value.trim() }
    await apis.userSettingApi.save({
      scene: OPENAPI_SETTING_SCENE,
      settingKey: OPENAPI_SETTING_KEY,
      settingValue: JSON.stringify(payload),
    })
    message.success('调用安全设置已保存')
  }
  catch (e) {
    message.error((e as Error).message || '保存失败')
  }
  finally {
    settingsSaving.value = false
  }
}

onMounted(() => {
  void loadCredentials()
  void loadOpenApiSettings()
})
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:key" width="16" />
            <span>API 凭证</span>
          </div>
          <div class="pf-section__desc">
            用于服务端调用 OpenAPI 的个人凭证（最多 5 个）。Secret 仅在创建/滚动时显示一次。
          </div>
        </div>
        <div class="pf-section__extra">
          <NButton size="small" type="primary" @click="openCreateModal">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>创建凭证
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NAlert v-if="newSecret" type="warning" title="请立即保存 API Secret，此密钥仅显示一次" :bordered="false" class="pf-secret-alert">
          <div class="pf-secret-row">
            <span class="pf-secret-label">AppKey</span>
            <NInputGroup>
              <NInput :value="newSecret.appKey" readonly size="small" />
              <NButton size="small" @click="copyText(newSecret.appKey)">
                <template #icon>
                  <NIcon><Icon icon="lucide:copy" /></NIcon>
                </template>
              </NButton>
            </NInputGroup>
          </div>
          <div class="pf-secret-row">
            <span class="pf-secret-label">Secret</span>
            <NInputGroup>
              <NInput :value="newSecret.appSecret" readonly size="small" type="password" show-password-on="click" />
              <NButton size="small" @click="copyText(newSecret.appSecret)">
                <template #icon>
                  <NIcon><Icon icon="lucide:copy" /></NIcon>
                </template>
              </NButton>
            </NInputGroup>
          </div>
        </NAlert>

        <NSpin :show="credentialsLoading">
          <NEmpty v-if="credentials.length === 0 && !credentialsLoading" class="pf-empty" description="暂无 API 凭证，点击右上角「创建凭证」开始接入" />
          <div v-else class="pf-list">
            <div v-for="cred in credentials" :key="String(cred.basicId)" class="pf-list-item pf-credential">
              <div class="pf-list-body">
                <div class="pf-list-title pf-credential__name">
                  <span>{{ cred.credentialName }}</span>
                  <NTag size="small" round :bordered="false" :type="cred.status === 'Enabled' ? 'success' : 'default'">
                    {{ cred.status === 'Enabled' ? '已启用' : '已停用' }}
                  </NTag>
                </div>
                <div class="pf-credential__key">
                  <code>{{ cred.appKey }}</code>
                  <NButton size="tiny" quaternary @click="copyText(cred.appKey)">
                    <template #icon>
                      <NIcon><Icon icon="lucide:copy" /></NIcon>
                    </template>
                  </NButton>
                </div>
                <div class="pf-list-desc">
                  创建于 {{ formatDate(cred.createdTime) }}
                  <template v-if="cred.lastUsedTime">
                    · 最后使用 {{ formatDate(cred.lastUsedTime) }}
                  </template>
                  <template v-else>
                    · 从未使用
                  </template>
                </div>
              </div>
              <div class="pf-credential__actions">
                <NTooltip>
                  <template #trigger>
                    <NSwitch
                      size="small"
                      :value="cred.status === 'Enabled'"
                      @update:value="(v: boolean) => handleToggleStatus(cred, v)"
                    />
                  </template>启用/停用
                </NTooltip>
                <NTooltip>
                  <template #trigger>
                    <NButton size="tiny" quaternary @click="handleRotateSecret(cred)">
                      <template #icon>
                        <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
                      </template>
                    </NButton>
                  </template>滚动密钥
                </NTooltip>
                <NTooltip>
                  <template #trigger>
                    <NButton size="tiny" quaternary type="error" @click="handleDeleteCredential(cred)">
                      <template #icon>
                        <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                      </template>
                    </NButton>
                  </template>删除凭证
                </NTooltip>
              </div>
            </div>
          </div>
        </NSpin>
      </div>
    </section>

    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:shield-check" width="16" />
            <span>调用安全设置</span>
          </div>
          <div class="pf-section__desc">
            签名算法与 IP 白名单，修改后需同步更新客户端配置。
          </div>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="pf-setting-list">
          <div class="pf-setting-row">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">签名算法</div>
              <div class="pf-setting-row__desc">服务端校验请求签名使用的算法</div>
            </div>
            <div class="pf-setting-row__control">
              <NSelect v-model:value="signAlgorithm" :options="signAlgorithmOptions" class="pf-field" size="small" />
            </div>
          </div>
          <div class="pf-setting-row pf-setting-row--block">
            <div class="pf-setting-row__main">
              <div class="pf-setting-row__label">IP 白名单</div>
              <div class="pf-setting-row__desc">每行一个 IP 或 CIDR 网段，留空则不限制</div>
            </div>
            <NInput
              v-model:value="ipWhitelist"
              type="textarea"
              placeholder="192.168.1.1&#10;10.0.0.0/24"
              :autosize="{ minRows: 3, maxRows: 6 }"
            />
          </div>
        </div>
      </div>
      <div class="pf-section__actions">
        <NButton type="primary" size="small" :loading="settingsSaving" @click="handleSaveOpenApiSettings">
          保存设置
        </NButton>
      </div>
    </section>

    <NModal
      v-model:show="createModalVisible"
      preset="dialog"
      title="创建 API 凭证"
      positive-text="创建"
      negative-text="取消"
      :loading="createSubmitting"
      @positive-click="handleCreateCredential"
    >
      <div class="pf-create-form">
        <div class="pf-create-form__tip">
          为凭证起个便于区分用途的名称（如「CI 流水线」「报表同步」）。
        </div>
        <NInput v-model:value="createName" placeholder="凭证名称（可选，默认「默认凭证」）" maxlength="100" show-count @keydown.enter="handleCreateCredential" />
      </div>
    </NModal>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-secret-alert {
  margin-bottom: 16px;
}

.pf-secret-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
}

.pf-secret-label {
  flex-shrink: 0;
  width: 52px;
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  opacity: 0.75;
}

.pf-credential {
  align-items: center;
}

.pf-credential__name {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pf-credential__key {
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 2px 0;
}

.pf-credential__key code {
  font-size: 12px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  opacity: 0.85;
}

.pf-credential__actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.pf-create-form__tip {
  margin-bottom: 8px;
  font-size: 13px;
  opacity: 0.7;
}
</style>
