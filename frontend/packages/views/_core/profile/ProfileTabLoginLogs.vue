<script lang="ts" setup>
import type { LoginAuditResult, LoginLogItem } from '~/types'
import { NButton, NPagination, NSpin, NTag, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabLoginLogs' })

const { apis } = useAppContext()
const message = useMessage()

/** 紧凑行布局下一屏约可容纳的条数 */
const PAGE_SIZE = 10

const logs = ref<LoginLogItem[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)

/** 与后端 LoginResult 枚举（字符串序列化）一致，含认证审计事件 */
const loginResultLabel: Record<LoginAuditResult, string> = {
  Success: '成功',
  InvalidCredentials: '密码错误',
  AccountLocked: '账号锁定',
  AccountDisabled: '账号禁用',
  RequiresTwoFactor: '需二次验证',
  TwoFactorFailed: '二次验证失败',
  Logout: '登出',
  TokenRefreshed: '令牌刷新',
  PasswordChanged: '密码修改',
  PasswordReset: '密码重置',
  MfaBound: '绑定MFA',
  MfaUnbound: '解绑MFA',
  Failed: '其他失败',
}

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const ERROR_RESULTS: LoginAuditResult[] = ['InvalidCredentials', 'TwoFactorFailed', 'Failed']
const NEUTRAL_RESULTS: LoginAuditResult[] = ['Logout', 'TokenRefreshed']

function resultTagType(result: LoginAuditResult): TagType {
  if (result === 'Success')
    return 'success'
  if (NEUTRAL_RESULTS.includes(result))
    return 'info'
  if (ERROR_RESULTS.includes(result))
    return 'error'
  return 'warning'
}

function resultIcon(result: LoginAuditResult) {
  if (result === 'Success')
    return 'lucide:log-in'
  if (result === 'Logout')
    return 'lucide:log-out'
  if (result === 'TokenRefreshed')
    return 'lucide:refresh-cw'
  if (result === 'PasswordChanged' || result === 'PasswordReset' || result === 'MfaBound' || result === 'MfaUnbound')
    return 'lucide:key-round'
  return 'lucide:shield-alert'
}

/** 仅认证失败类记录用危险配色，登出/审计事件保持中性 */
function isDanger(result: LoginAuditResult) {
  return ERROR_RESULTS.includes(result) || result === 'AccountLocked' || result === 'AccountDisabled'
}

async function loadLogs(nextPage = 1) {
  loading.value = true
  try {
    const res = await apis.getLoginLogsApi(nextPage, PAGE_SIZE)
    logs.value = res.items
    total.value = res.total
    page.value = nextPage
  }
  catch (e: unknown) {
    logs.value = []
    total.value = 0
    message.error((e as Error)?.message || '加载登录日志失败')
  }
  finally {
    loading.value = false
  }
}

onMounted(() => loadLogs())
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:file-clock" width="16" />
            <span>登录日志</span>
          </div>
          <div class="pf-section__desc">
            最近的登录记录，发现异常请及时修改密码并登出可疑设备。
          </div>
        </div>
        <div class="pf-section__extra">
          <NButton size="tiny" quaternary @click="loadLogs(page)">
            <template #icon>
              <Icon icon="lucide:refresh-cw" />
            </template>
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="loading">
          <div v-if="logs.length === 0 && !loading" class="pf-empty">
            <span class="pf-empty__icon"><Icon icon="lucide:inbox" width="16" /></span>
            <span>暂无登录记录</span>
          </div>
          <div v-else class="pf-log-grid">
            <div v-for="(log, idx) in logs" :key="idx" class="pf-list-item pf-log-row">
              <div class="pf-list-icon" :class="{ 'pf-list-icon--danger': isDanger(log.loginResult) }">
                <Icon :icon="resultIcon(log.loginResult)" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  <NTag :type="resultTagType(log.loginResult)" size="tiny" :bordered="false">
                    {{ loginResultLabel[log.loginResult] || `状态${log.loginResult}` }}
                  </NTag>
                  <span v-if="log.message" class="pf-log-message">{{ log.message }}</span>
                </div>
                <div class="pf-list-desc">
                  {{ log.loginIp || '未知 IP' }}
                  <template v-if="log.loginLocation">
                    · {{ log.loginLocation }}
                  </template>
                  <template v-if="log.browser">
                    · {{ log.browser }}
                  </template>
                  <template v-if="log.os">
                    · {{ log.os }}
                  </template>
                </div>
              </div>
              <div class="pf-log-time">
                {{ formatDate(log.loginTime) }}
              </div>
            </div>
          </div>
          <div v-if="total > PAGE_SIZE" class="pf-log-pagination">
            <NPagination
              :page="page"
              :page-size="PAGE_SIZE"
              :item-count="total"
              simple
              @update:page="loadLogs"
            />
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-log-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

/* 紧凑两行条目：左图标 + 中间（状态/消息 + 设备信息）+ 右侧时间 */
.pf-log-row {
  padding: 9px 14px;
}

.pf-log-message {
  color: var(--text-secondary);
  font-size: 12px;
}

.pf-log-time {
  flex-shrink: 0;
  align-self: center;
  color: var(--text-secondary);
  font-size: 12.5px;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.pf-log-pagination {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
