<script lang="ts" setup>
import type { LoginFormAlign } from './LoginToolbar.vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useTheme } from '~/hooks'
import { useAppStore } from '~/stores'
import LoginToolbar from './LoginToolbar.vue'

defineOptions({ name: 'AuthLayout' })

const formAlign = ref<LoginFormAlign>('right')
const { isDark } = useTheme()
const { t } = useI18n()
const appStore = useAppStore()

const appTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const appLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

const showFooter = computed(() => appStore.footerEnable && (appStore.copyrightEnable || appStore.footerShowDevInfo))

const appVersion = __APP_VERSION__
const appBuildTime = __APP_BUILD_TIME__
const appHomepage = __APP_HOMEPAGE__
const appName = __APP_NAME__
const appAuthorName = __APP_AUTHOR_NAME__
const appAuthorUrl = __APP_AUTHOR_URL__
</script>

<template>
  <div
    class="relative min-h-screen"
    :class="
      isDark
        ? 'bg-[#0b1220] text-white'
        : 'bg-[hsl(var(--background-deep))] text-[hsl(var(--foreground))]'
    "
  >
    <LoginToolbar @layout-change="(align) => (formAlign = align)" />

    <div
      class="grid min-h-screen grid-cols-1"
      :class="{
        'lg:grid-cols-[420px_1fr]': formAlign === 'left',
        'lg:grid-cols-1': formAlign === 'center',
        'lg:grid-cols-[1fr_420px]': formAlign === 'right',
      }"
    >
      <!-- 插图面板 -->
      <div
        class="relative hidden overflow-hidden lg:flex lg:items-center lg:justify-center"
        :class="{ 'lg:order-1': formAlign === 'left', 'lg:hidden': formAlign === 'center' }"
      >
        <div
          class="pointer-events-none absolute inset-0 opacity-40"
          :class="
            isDark
              ? 'bg-[radial-gradient(circle_at_30%_30%,#1d4ed8_0%,transparent_38%),radial-gradient(circle_at_80%_65%,#0ea5e9_0%,transparent_28%)]'
              : 'bg-[radial-gradient(circle_at_30%_30%,#93c5fd_0%,transparent_45%),radial-gradient(circle_at_75%_70%,#a5f3fc_0%,transparent_35%)]'
          "
        />
        <div class="relative text-center">
          <div
            class="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl"
            :class="isDark ? 'bg-white/90' : 'bg-[hsl(var(--card))] shadow-md'"
          >
            <img :src="appLogo" :alt="appTitle" class="h-10 w-10 object-contain">
          </div>
          <h2 class="mb-3 text-3xl font-semibold">
            {{ t('page.auth.slogan_title') }}
          </h2>
          <p
            class="text-sm"
            :class="isDark ? 'text-gray-300' : 'text-[hsl(var(--muted-foreground))]'"
          >
            {{ t('page.auth.slogan_desc') }}
          </p>
        </div>
      </div>

      <!-- 表单面板 -->
      <div
        class="flex items-center justify-center px-8 py-10 lg:border-[hsl(var(--border))]"
        :class="{
          'lg:border-r': formAlign === 'left',
          'lg:border-l': formAlign === 'right',
          'lg:bg-black/30 backdrop-blur-xl': isDark,
          'lg:bg-[hsl(var(--card))]': !isDark,
        }"
      >
        <div class="w-full" :class="formAlign === 'center' ? 'max-w-[400px]' : 'max-w-[340px]'">
          <router-view v-slot="{ Component }">
            <transition name="auth-slide" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
      </div>
    </div>

    <!-- Footer -->
    <footer
      v-if="showFooter"
      class="auth-footer absolute bottom-0 left-0 flex w-full flex-col items-center justify-center gap-1 px-4 py-3 text-xs"
      :class="isDark ? 'text-gray-500' : 'text-[hsl(var(--muted-foreground))]'"
    >
      <div v-if="appStore.footerShowDevInfo" class="leading-tight">
        <a :href="appHomepage" target="_blank" class="hover:underline">{{ appName }}</a>
        v{{ appVersion }}({{ appBuildTime }})
        · by
        <a :href="appAuthorUrl" target="_blank" class="hover:underline">{{ appAuthorName }}</a>
      </div>
      <div v-if="appStore.copyrightEnable" class="flex flex-wrap items-center justify-center gap-x-3">
        <span>
          Copyright &copy; {{ appStore.copyrightDate || new Date().getFullYear() }}-{{ new Date().getFullYear() }}
          <a
            v-if="appStore.copyrightSite"
            :href="appStore.copyrightSite"
            target="_blank"
            class="hover:underline"
          >{{ appStore.copyrightName }}</a>
          <span v-else>{{ appStore.copyrightName }}</span>.
          All Rights Reserved.
        </span>
        <a
          v-if="appStore.copyrightIcp"
          :href="appStore.copyrightIcpUrl || '#'"
          target="_blank"
          class="hover:underline"
        >{{ appStore.copyrightIcp }}</a>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.auth-slide-enter-active,
.auth-slide-leave-active {
  transition: all 0.25s ease;
}

.auth-slide-enter-from {
  opacity: 0;
  transform: translateX(30px);
}

.auth-slide-leave-to {
  opacity: 0;
  transform: translateX(-30px);
}

.auth-footer :deep(a) {
  color: inherit;
  text-decoration: none;
}
</style>
