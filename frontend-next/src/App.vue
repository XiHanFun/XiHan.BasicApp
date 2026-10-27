<script lang="ts" setup>
import type { Locale } from '~/locales'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { LOCALES } from '~/locales'
import { LOGIN_ROUTE } from '~/router'
import { useAppStore, useAuthStore } from '~/stores'

defineOptions({ name: 'App' })

const { t } = useI18n()
const auth = useAuthStore()
const app = useAppStore()
const router = useRouter()
const tenantLabel = computed(() => auth.tenantId ?? t('common.session.platform'))

async function signOut() {
  await auth.logout()
  await router.replace({ name: LOGIN_ROUTE })
}

function onLocaleChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value as Locale
  void app.setLocale(value)
}
</script>

<template>
  <div class="flex min-h-full flex-col bg-page text-fg">
    <header class="flex flex-wrap items-center gap-3 border-b border-border bg-surface px-4 py-2">
      <span class="text-label font-semibold">{{ t('common.app_name') }}</span>
      <span class="text-caption text-fg-muted">{{ t('common.version', { version: '6.0.0-preview.1' }) }}</span>
      <!-- 会话区：登录后显示当前用户与租户、改密入口与注销；授权以后端为准，这里只展示 -->
      <nav v-if="auth.isAuthenticated" class="ml-auto flex items-center gap-3" :aria-label="t('common.session.label')">
        <span class="text-caption text-fg-muted" data-testid="session-user">{{ t('common.session.signed_in', { name: auth.user?.displayName ?? '', tenant: tenantLabel }) }}</span>
        <RouterLink class="text-caption text-fg-brand underline-offset-2 hover:underline" :to="{ name: 'identity.profile.change-password' }">
          {{ t('common.session.change_password') }}
        </RouterLink>
        <XhButton size="sm" @click="signOut">
          {{ t('common.session.sign_out') }}
        </XhButton>
      </nav>
      <label class="text-caption text-fg-muted" :class="auth.isAuthenticated ? '' : 'ml-auto'" for="locale">{{ t('common.locale') }}</label>
      <select id="locale" class="rounded-control border border-border bg-surface px-2 py-1 text-caption text-fg" :value="app.locale" @change="onLocaleChange">
        <option v-for="locale in LOCALES" :key="locale" :value="locale">
          {{ locale }}
        </option>
      </select>
    </header>
    <p v-if="auth.isAuthenticated && auth.mustChangePassword" class="border-b border-border bg-brand-subtle px-4 py-2 text-caption text-fg" role="status">
      {{ t('common.session.must_change_password') }}
      <RouterLink class="text-fg-brand underline-offset-2 hover:underline" :to="{ name: 'identity.profile.change-password' }">
        {{ t('common.session.change_password') }}
      </RouterLink>
    </p>
    <main class="flex flex-1 flex-col p-4" :class="auth.isAuthenticated ? '' : 'items-center justify-center'">
      <RouterView />
    </main>
  </div>
</template>
