<script setup lang="ts">
import type { ImpersonationCandidate, ImpersonationTenantOption } from '~/types'
import {
  XhButton,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogRoot,
  XhDialogTitle,
  XhEmptyStateDescription,
  XhEmptyStateIndicator,
  XhEmptyStateRoot,
  XhEmptyStateTitle,
  XhSpinner,
} from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput, XSelect } from '~/components'
import { Icon } from '~/iconify'
import { useAuthStore, useUserStore } from '~/stores'

defineOptions({ name: 'ImpersonationDialog' })

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const authStore = useAuthStore()
const userStore = useUserStore()

/** 平台范围的选项值：模仿平台账号 */
const PLATFORM_SCOPE = 'platform'

const keyword = ref('')
const reason = ref('')
/** 平台里发起时要先选范围：平台账号，或某个租户的成员；租户里固定为本租户 */
const isPlatform = computed(() => userStore.userInfo?.isPlatform ?? false)
const scope = ref<string>(PLATFORM_SCOPE)
const tenants = ref<ImpersonationTenantOption[]>([])
const tenantsLoading = ref(false)
const scopeOptions = computed(() => [
  { label: t('header.impersonation.scope_platform'), value: PLATFORM_SCOPE },
  ...tenants.value.map(tenant => ({ label: tenant.tenantName, value: tenant.tenantId })),
])
/** 当前要检索的租户：平台里取选中的租户，租户里取本租户 */
const targetTenantId = computed(() => {
  if (!isPlatform.value) {
    return userStore.userInfo?.tenantId ?? undefined
  }
  return scope.value === PLATFORM_SCOPE ? undefined : scope.value
})
const loading = ref(false)
const candidates = ref<ImpersonationCandidate[]>([])
const errorMessage = ref('')

let searchTimer: ReturnType<typeof setTimeout> | null = null
// 候选查询按最新一笔收敛：慢响应回来时不覆盖新结果
let requestSeq = 0

/** 展示名：昵称 → 真名 → 用户名 */
function displayName(candidate: ImpersonationCandidate) {
  return candidate.nickName || candidate.realName || candidate.userName
}

async function loadCandidates() {
  const seq = ++requestSeq
  loading.value = true
  errorMessage.value = ''
  try {
    const items = await authStore.impersonationCandidates({
      keyword: keyword.value.trim() || undefined,
      tenantId: targetTenantId.value,
    })
    if (seq !== requestSeq) {
      return
    }
    // 自己不出现在候选里：服务端也会拒，先在这里省掉一次无效往返
    candidates.value = items.filter(item => item.basicId !== userStore.userInfo?.basicId)
  }
  catch (error) {
    if (seq !== requestSeq) {
      return
    }
    candidates.value = []
    errorMessage.value = (error as Error)?.message || t('header.impersonation.load_failed')
  }
  finally {
    if (seq === requestSeq) {
      loading.value = false
    }
  }
}

function scheduleSearch() {
  if (searchTimer) {
    clearTimeout(searchTimer)
  }
  searchTimer = setTimeout(() => {
    searchTimer = null
    void loadCandidates()
  }, 300)
}

async function start(candidate: ImpersonationCandidate) {
  errorMessage.value = ''
  try {
    await authStore.startImpersonation({
      targetUserId: candidate.basicId,
      // 平台选了租户就进该租户；租户里不传，沿用当前租户
      tenantId: isPlatform.value ? (targetTenantId.value ?? null) : undefined,
      reason: reason.value.trim() || null,
    })
  }
  catch (error) {
    errorMessage.value = (error as Error)?.message || t('header.impersonation.start_failed')
  }
}

async function loadTenants() {
  tenantsLoading.value = true
  try {
    tenants.value = await authStore.impersonationTenants()
  }
  catch (error) {
    tenants.value = []
    errorMessage.value = (error as Error)?.message || t('header.impersonation.load_failed')
  }
  finally {
    tenantsLoading.value = false
  }
}

function onScopeChange(value: string | number | Array<string | number> | null) {
  scope.value = value === null || Array.isArray(value) ? PLATFORM_SCOPE : String(value)
  void loadCandidates()
}

watch(show, (open) => {
  if (!open) {
    return
  }
  keyword.value = ''
  reason.value = ''
  errorMessage.value = ''
  scope.value = PLATFORM_SCOPE
  if (isPlatform.value) {
    void loadTenants()
  }
  // 上一行置空 keyword 会触发下面那个 watch 排一次防抖查，这里撤掉它，只留本次直查
  if (searchTimer) {
    clearTimeout(searchTimer)
    searchTimer = null
  }
  void loadCandidates()
})

watch(keyword, scheduleSearch)

onBeforeUnmount(() => {
  if (searchTimer) {
    clearTimeout(searchTimer)
    searchTimer = null
  }
})
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 420px">
      <XhDialogTitle>{{ t('header.impersonation.title') }}</XhDialogTitle>
      <XhDialogCloseTrigger />

      <div class="flex flex-col gap-3">
        <p class="impersonation-hint">
          {{ t('header.impersonation.hint') }}
        </p>

        <XSelect
          v-if="isPlatform"
          :value="scope"
          :options="scopeOptions"
          :loading="tenantsLoading"
          :aria-label="t('header.impersonation.scope_label')"
          @update:value="onScopeChange"
        />
        <XInput
          v-model:value="keyword"
          clearable
          :placeholder="t('header.impersonation.search_placeholder')"
        />
        <XInput
          v-model:value="reason"
          :max-length="200"
          :placeholder="t('header.impersonation.reason_placeholder')"
        />

        <p v-if="errorMessage" class="impersonation-error">
          {{ errorMessage }}
        </p>

        <div class="impersonation-list">
          <div v-if="loading" class="impersonation-loading">
            <XhSpinner />
          </div>
          <template v-else-if="candidates.length">
            <button
              v-for="candidate in candidates"
              :key="candidate.basicId"
              type="button"
              class="impersonation-item"
              :disabled="authStore.impersonationLoading"
              @click="start(candidate)"
            >
              <span>{{ displayName(candidate) }}</span>
              <span class="impersonation-item-account">{{ candidate.userName }}</span>
            </button>
          </template>
          <XhEmptyStateRoot v-else class="impersonation-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:user-round-search" width="24" height="24" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('header.impersonation.empty_title') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('header.impersonation.empty_description') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>

        <div class="flex justify-end">
          <XhButton size="sm" variant="ghost" @click="show = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
        </div>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.impersonation-hint {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
  color: hsl(var(--muted-foreground));
}

.impersonation-error {
  margin: 0;
  font-size: 12px;
  color: hsl(var(--destructive));
}

.impersonation-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
  max-height: 260px;
  overflow-y: auto;
}

.impersonation-loading {
  display: flex;
  justify-content: center;
  padding: 16px 0;
}

.impersonation-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 6px 12px;
  font-size: 12px;
  line-height: normal;
  color: hsl(var(--foreground));
  text-align: left;
  cursor: pointer;
  background: transparent;
  border: none;
  border-radius: 6px;
  transition: background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.impersonation-item:hover:not(:disabled) {
  background: hsl(var(--accent));
}

.impersonation-item:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.impersonation-item-account {
  color: hsl(var(--muted-foreground));
}

.impersonation-empty {
  padding: 12px 0;
}
</style>
