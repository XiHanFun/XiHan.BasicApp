<script lang="ts" setup>
import { NTabPane, NTabs } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'

defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const entryList = computed(() => [
  { path: '/auth/login', label: t('page.login.title') },
  { path: '/auth/code-login', label: t('page.auth.mobile_login') },
  { path: '/auth/qrcode-login', label: t('page.auth.qrcode_login') },
])

const activePath = computed(() => {
  const path = route.path
  return entryList.value.some((item) => item.path === path) ? path : '/auth/login'
})

function goTo(path: string) {
  if (route.path === path) return
  router.push(path)
}
</script>

<template>
  <NTabs class="entry-switcher" type="segment" animated :value="activePath" @update:value="goTo">
    <NTabPane v-for="item in entryList" :key="item.path" :name="item.path" :tab="item.label" />
  </NTabs>
</template>

<style scoped>
.entry-switcher {
  width: 100%;
}

.entry-switcher :deep(.n-tabs-nav) {
  margin-bottom: 0 !important;
}

.entry-switcher :deep(.n-tabs-rail) {
  padding: 4px;
  border-radius: 12px;
  border: 1px solid hsl(var(--border));
  background: hsl(var(--muted)) !important;
}

.entry-switcher :deep(.n-tabs-tab) {
  height: 42px;
  border-radius: 9px;
  font-size: 14px;
  font-weight: 500;
  justify-content: center;
  color: hsl(var(--muted-foreground)) !important;
  transition: none !important;
}

.entry-switcher :deep(.n-tabs-tab:hover:not(.n-tabs-tab--active)) {
  color: hsl(var(--foreground)) !important;
}

.entry-switcher :deep(.n-tabs-tab.n-tabs-tab--active) {
  color: hsl(var(--primary-foreground)) !important;
}

.entry-switcher :deep(.n-tabs-tab.n-tabs-tab--active .n-tabs-tab__label) {
  color: hsl(var(--primary-foreground)) !important;
  font-weight: 600;
}

.entry-switcher :deep(.n-tabs-tab .n-tabs-tab__label) {
  transition: none !important;
}

/* segment 滑块 */
.entry-switcher :deep(.n-tabs-capsule) {
  border-radius: 9px !important;
  background: hsl(var(--primary)) !important;
  box-shadow: 0 4px 14px hsl(var(--primary) / 0.3) !important;
  transition: left 0.25s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

.entry-switcher :deep(.n-tabs-pane-wrapper) {
  display: none;
}
</style>
