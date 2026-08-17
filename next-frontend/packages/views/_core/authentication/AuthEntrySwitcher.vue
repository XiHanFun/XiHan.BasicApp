<script lang="ts" setup>
import type { TabsNode } from '@xihan-ui/headless'
import { XhTabsRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

const entryList = computed(() => [
  { path: LOGIN_PATH, label: t('page.login.title') },
  { path: CODE_LOGIN_PATH, label: t('page.auth.mobile_login') },
  { path: EMAIL_LOGIN_PATH, label: t('page.auth.email_login') },
  { path: QRCODE_LOGIN_PATH, label: t('page.auth.qrcode_login') },
])

const activePath = computed(() => {
  const path = route.path
  return entryList.value.some(item => item.path === path) ? path : LOGIN_PATH
})

const tabCollection = computed<TabsNode[]>(() =>
  entryList.value.map(item => ({ value: item.path, label: item.label })),
)

function goTo(path: string) {
  if (route.path === path)
    return
  router.push(path)
}
</script>

<template>
  <!-- 只有一排标签、没有面板：collection 铺开即可，面板插槽不给 -->
  <XhTabsRoot
    class="entry-switcher"
    variant="line"
    :collection="tabCollection"
    :value="activePath"
    @update:value="(next: string | null) => next && goTo(next)"
  />
</template>

<style scoped>
.entry-switcher {
  width: 100%;
}

.entry-switcher :deep(.n-tabs-nav) {
  margin-bottom: 0 !important;
}

.entry-switcher :deep(.n-tabs-tab) {
  font-size: 15px;
  font-weight: 500;
  color: hsl(var(--muted-foreground)) !important;
}

.entry-switcher :deep(.n-tabs-tab:hover:not(.n-tabs-tab--active)) {
  color: hsl(var(--foreground)) !important;
}

.entry-switcher :deep(.n-tabs-tab.n-tabs-tab--active .n-tabs-tab__label) {
  color: hsl(var(--primary)) !important;
  font-weight: 600;
}

.entry-switcher :deep(.n-tabs-bar) {
  background: hsl(var(--primary)) !important;
}

.entry-switcher :deep(.n-tabs-pane-wrapper) {
  display: none;
}
</style>
