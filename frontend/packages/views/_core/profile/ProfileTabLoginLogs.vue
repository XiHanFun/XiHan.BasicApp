<script lang="ts" setup>
import type { LoginAuditResult, LoginLogItem } from '~/types'
import { NButton, NPagination, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabLoginLogs' })

const { apis } = useAppContext()
const message = useMessage()
const { t } = useI18n()

/** 紧凑行布局下一屏约可容纳的条数 */
const PAGE_SIZE = 10

const logs = ref<LoginLogItem[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)

/** 与后端 LoginResult 枚举（字符串序列化）一致，含认证审计事件 */
const loginResultLabel = computed<Record<LoginAuditResult, string>>(() => ({
  Success: t('component.profile.login_logs.result_success'),
  InvalidCredentials: t('component.profile.login_logs.result_invalid_credentials'),
  AccountLocked: t('component.profile.login_logs.result_account_locked'),
  AccountDisabled: t('component.profile.login_logs.result_account_disabled'),
  RequiresTwoFactor: t('component.profile.login_logs.result_requires_two_factor'),
  TwoFactorFailed: t('component.profile.login_logs.result_two_factor_failed'),
  Logout: t('component.profile.login_logs.result_logout'),
  TokenRefreshed: t('component.profile.login_logs.result_token_refreshed'),
  PasswordChanged: t('component.profile.login_logs.result_password_changed'),
  PasswordReset: t('component.profile.login_logs.result_password_reset'),
  MfaBound: t('component.profile.login_logs.result_mfa_bound'),
  MfaUnbound: t('component.profile.login_logs.result_mfa_unbound'),
  TenantSwitched: t('component.profile.login_logs.result_tenant_switched'),
  SessionRevoked: t('component.profile.login_logs.result_session_revoked'),
  Failed: t('component.profile.login_logs.result_failed'),
}))

type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const ERROR_RESULTS: LoginAuditResult[] = ['InvalidCredentials', 'TwoFactorFailed', 'Failed']
const NEUTRAL_RESULTS: LoginAuditResult[] = ['Logout', 'TokenRefreshed', 'TenantSwitched']

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
  if (result === 'TenantSwitched')
    return 'lucide:building-2'
  if (result === 'SessionRevoked')
    return 'lucide:shield-off'
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
    message.error((e as Error)?.message || t('component.profile.login_logs.err_load_failed'))
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
            <span>{{ t('component.profile.login_logs.section_title') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.login_logs.section_desc') }}
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
            <span>{{ t('component.profile.login_logs.empty') }}</span>
          </div>
          <div v-else class="pf-log-grid">
            <div v-for="(log, idx) in logs" :key="idx" class="pf-list-item pf-log-row">
              <div class="pf-list-icon" :class="{ 'pf-list-icon--danger': isDanger(log.loginResult) }">
                <Icon :icon="resultIcon(log.loginResult)" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  <NTag :type="resultTagType(log.loginResult)" size="tiny" :bordered="false">
                    {{ loginResultLabel[log.loginResult] || t('component.profile.login_logs.result_unknown', { result: log.loginResult }) }}
                  </NTag>
                  <span v-if="log.message" class="pf-log-message">{{ log.message }}</span>
                </div>
                <div class="pf-list-desc">
                  {{ log.loginIp || t('component.profile.login_logs.unknown_ip') }}
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
