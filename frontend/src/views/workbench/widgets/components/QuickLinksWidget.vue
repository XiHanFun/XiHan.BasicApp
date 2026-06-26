<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { usePermission } from '~/hooks'
import { Icon } from '~/iconify'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'QuickLinksWidget' })

const { t } = useI18n()
const router = useRouter()
const { hasPermission } = usePermission()

// 常用页面直达；每个入口可声明权限码，无权限则不展示
const allLinks = [
  { path: '/identity/user', icon: 'lucide:users', label: 'menu.identity_user', permission: 'saas:user:read' },
  { path: '/identity/role', icon: 'lucide:shield-check', label: 'menu.identity_role', permission: 'saas:role:read' },
  { path: '/identity/org', icon: 'lucide:network', label: 'menu.identity_org', permission: 'saas:department:read' },
  { path: '/message/notification', icon: 'lucide:megaphone', label: 'menu.message_notification', permission: 'saas:notification:read' },
  { path: '/workbench/inbox', icon: 'lucide:inbox', label: 'menu.workbench_inbox', permission: '' },
  { path: '/workbench/profile', icon: 'lucide:user', label: 'menu.profile', permission: '' },
]

const links = computed(() => allLinks.filter(link => !link.permission || hasPermission(link.permission)))

function go(path: string) {
  router.push(path).catch(() => {})
}
</script>

<template>
  <WidgetCard icon="lucide:zap" :title="t('workbench.widgets.quick_links.title')">
    <div v-if="links.length" class="grid grid-cols-2 gap-2 sm:grid-cols-3">
      <button
        v-for="link in links"
        :key="link.path"
        type="button"
        class="flex flex-col items-center gap-1.5 rounded-lg border border-border/60 bg-background px-2 py-3 text-center transition-colors hover:border-[hsl(var(--primary)/0.4)] hover:bg-muted"
        @click="go(link.path)"
      >
        <Icon :icon="link.icon" width="20" height="20" class="text-[hsl(var(--primary))]" />
        <span class="text-xs text-foreground">{{ t(link.label) }}</span>
      </button>
    </div>
    <div v-else class="flex h-full items-center justify-center text-sm text-muted-foreground">
      {{ t('workbench.widgets.quick_links_empty') }}
    </div>
  </WidgetCard>
</template>
