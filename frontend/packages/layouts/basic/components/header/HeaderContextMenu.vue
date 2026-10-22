<script setup lang="ts">
import type { AppTenantSwitcherItem } from '~/types'
import {
  XhMenuContent,
  XhMenuItem,
  XhMenuItemIcon,
  XhMenuItemIndicator,
  XhMenuItemText,
  XhMenuPositioner,
  XhMenuRadioGroup,
  XhMenuRadioItem,
  XhMenuRoot,
  XhMenuSeparator,
  XhMenuTrigger,
} from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext, useAuthStore, useUserStore } from '~/stores'
import XihanIconButton from '../XihanIconButton.vue'

/**
 * 顶栏的上下文切换：悬停（或点击、键盘）展开，列出平台（平台账号）与可切换的租户，当前所在的一项打勾，
 * 选中即在目标上下文续接会话并整页重建；末项进控制中心。
 */
defineOptions({ name: 'HeaderContextMenu' })

const props = defineProps<{
  /** 当前上下文的显示名：平台或租户名 */
  label: string
  isPlatform: boolean
}>()

const emit = defineEmits<{ controlCenter: [] }>()

const { t } = useI18n()
const appContext = useAppContext()
const authStore = useAuthStore()
const userStore = useUserStore()

/** 单选组里平台一项的值；租户项的值是租户 id */
const PLATFORM = 'platform'

const tenants = ref<AppTenantSwitcherItem[]>([])
const loading = ref(false)
const switching = ref(false)

const canAccessPlatform = computed(() => userStore.userInfo?.canAccessPlatform ?? false)
const currentValue = computed(() => props.isPlatform
  ? PLATFORM
  : (tenants.value.find(tenant => tenant.isCurrent)?.tenantId ?? userStore.userInfo?.tenantId ?? ''))
const radioValue = computed(() => ({ context: String(currentValue.value) }))
const empty = computed(() => !loading.value && !canAccessPlatform.value && tenants.value.length === 0)

/** 每次展开重取一遍：成员关系可能刚变过，列表要与服务端一致 */
async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await appContext.apis.tenantApi.myAvailableTenants()
  }
  catch (error: unknown) {
    toast.danger((error as Error)?.message || t('header.context.load_failed'))
  }
  finally {
    loading.value = false
  }
}

function onOpenChange(details: { open: boolean }) {
  if (details.open) {
    void loadTenants()
  }
}

async function onContextChange(details: { value: Record<string, string> }) {
  const target = details.value.context
  if (!target || target === String(currentValue.value) || switching.value) {
    return
  }
  switching.value = true
  try {
    await authStore.switchContext(target === PLATFORM ? null : target)
  }
  catch (error: unknown) {
    toast.danger((error as Error)?.message || t('header.context.switch_failed'))
    switching.value = false
  }
}

function onItemSelect(details: { value: string }) {
  if (details.value === 'control-center') {
    emit('controlCenter')
  }
}
</script>

<template>
  <XhMenuRoot
    open-on-hover
    placement="bottom-end"
    :radio-value="radioValue"
    @open-change="onOpenChange"
    @radio-value-change="onContextChange"
    @item-select="onItemSelect"
  >
    <XhMenuTrigger as-child>
      <XihanIconButton
        class="xihan-icon-btn--labeled mr-1"
        :aria-label="t('header.context.switch', { name: props.label })"
      >
        <Icon :icon="props.isPlatform ? 'lucide:shield-check' : 'lucide:building-2'" width="16" height="16" class="shrink-0" />
        <span class="hidden max-w-32 truncate text-sm text-foreground md:block">{{ props.label }}</span>
      </XihanIconButton>
    </XhMenuTrigger>
    <XhMenuPositioner>
      <XhMenuContent>
        <XhMenuRadioGroup value="context">
          <XhMenuRadioItem v-if="canAccessPlatform" :value="PLATFORM" :disabled="switching">
            <XhMenuItemIcon><Icon icon="lucide:shield-check" /></XhMenuItemIcon>
            <XhMenuItemText>{{ t('header.context.platform') }}</XhMenuItemText>
            <XhMenuItemIndicator />
          </XhMenuRadioItem>
          <XhMenuRadioItem
            v-for="tenant in tenants"
            :key="tenant.tenantId"
            :value="String(tenant.tenantId)"
            :disabled="switching"
          >
            <XhMenuItemIcon><Icon icon="lucide:building-2" /></XhMenuItemIcon>
            <XhMenuItemText>{{ tenant.tenantShortName || tenant.tenantName }}</XhMenuItemText>
            <XhMenuItemIndicator />
          </XhMenuRadioItem>
        </XhMenuRadioGroup>
        <XhMenuItem v-if="loading && tenants.length === 0" value="loading" disabled>
          <XhMenuItemText>{{ t('header.context.loading') }}</XhMenuItemText>
        </XhMenuItem>
        <XhMenuItem v-else-if="empty" value="empty" disabled>
          <XhMenuItemText>{{ t('header.context.empty') }}</XhMenuItemText>
        </XhMenuItem>
        <XhMenuSeparator />
        <XhMenuItem value="control-center">
          <XhMenuItemIcon><Icon icon="lucide:layout-grid" /></XhMenuItemIcon>
          <XhMenuItemText>{{ t('header.context.manage') }}</XhMenuItemText>
        </XhMenuItem>
      </XhMenuContent>
    </XhMenuPositioner>
  </XhMenuRoot>
</template>
