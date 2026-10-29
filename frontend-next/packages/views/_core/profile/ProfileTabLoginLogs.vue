<script lang="ts" setup>
import type { LoginAuditResult, LoginLogItem } from '~/types'
import { XhButton, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { SchemaPagination } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabLoginLogs' })

const { apis } = useAppContext()
const { t } = useI18n()

/** 紧凑行布局下一屏约可容纳的条数 */
const PAGE_SIZE = 10

const logs = ref<LoginLogItem[]>([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)

/**
 * 登录结果随认证审计事件增长（模仿登录、角色激活等）：4.x 取后端 LoginResult 枚举元数据，
 * 6.0 没有枚举元数据，标签用本地文案（键静态写出以便 i18n 门禁核对），前端只管配色与图标
 */
const RESULT_LABELS: Record<LoginAuditResult, () => string> = {
  AccountDisabled: () => t('component.profile.login_logs.results.account_disabled'),
  AccountLocked: () => t('component.profile.login_logs.results.account_locked'),
  Failed: () => t('component.profile.login_logs.results.failed'),
  ImpersonationEnded: () => t('component.profile.login_logs.results.impersonation_ended'),
  ImpersonationStarted: () => t('component.profile.login_logs.results.impersonation_started'),
  InvalidCredentials: () => t('component.profile.login_logs.results.invalid_credentials'),
  Logout: () => t('component.profile.login_logs.results.logout'),
  MfaBound: () => t('component.profile.login_logs.results.mfa_bound'),
  MfaUnbound: () => t('component.profile.login_logs.results.mfa_unbound'),
  PasswordChanged: () => t('component.profile.login_logs.results.password_changed'),
  PasswordReset: () => t('component.profile.login_logs.results.password_reset'),
  RequiresTwoFactor: () => t('component.profile.login_logs.results.requires_two_factor'),
  RolesActivated: () => t('component.profile.login_logs.results.roles_activated'),
  SessionRevoked: () => t('component.profile.login_logs.results.session_revoked'),
  Success: () => t('component.profile.login_logs.results.success'),
  TenantSwitched: () => t('component.profile.login_logs.results.tenant_switched'),
  TokenRefreshed: () => t('component.profile.login_logs.results.token_refreshed'),
  TwoFactorFailed: () => t('component.profile.login_logs.results.two_factor_failed'),
}

function resultLabel(result: LoginAuditResult) {
  return RESULT_LABELS[result]()
}

type TagType = 'neutral' | 'danger' | 'info' | 'success' | 'warning'

const ERROR_RESULTS: LoginAuditResult[] = ['InvalidCredentials', 'TwoFactorFailed', 'Failed']
const NEUTRAL_RESULTS: LoginAuditResult[] = ['Logout', 'TokenRefreshed', 'TenantSwitched']

function resultTagType(result: LoginAuditResult): TagType {
  if (result === 'Success')
    return 'success'
  if (NEUTRAL_RESULTS.includes(result))
    return 'info'
  if (ERROR_RESULTS.includes(result))
    return 'danger'
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
  if (result === 'ImpersonationStarted' || result === 'ImpersonationEnded')
    return 'lucide:user-round-cog'
  if (result === 'PasswordChanged' || result === 'PasswordReset' || result === 'MfaBound' || result === 'MfaUnbound')
    return 'lucide:key-round'
  return 'lucide:shield-alert'
}

/** 仅认证失败类记录用危险配色，登出/审计事件保持中性 */
function isDanger(result: LoginAuditResult) {
  return ERROR_RESULTS.includes(result) || result === 'AccountLocked' || result === 'AccountDisabled'
}

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError ? error.detail : (error as Error | undefined)?.message
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
    toast.danger(messageOf(e) || t('component.profile.login_logs.err_load_failed'))
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
          <XhButton size="sm" variant="ghost" icon-only :aria-label="t('common.actions.refresh')" @click="loadLogs(page)">
            <Icon icon="lucide:refresh-cw" />
          </XhButton>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
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
                  <XhTagRoot variant="subtle" :tone="resultTagType(log.loginResult)" size="sm">
                    <XhTagLabel>
                      {{ resultLabel(log.loginResult) }}
                    </XhTagLabel>
                  </XhTagRoot>
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
            <SchemaPagination
              compact
              :page="page"
              :page-size="PAGE_SIZE"
              :total="total"
              @update:page="loadLogs"
            />
          </div>
        </div>
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
