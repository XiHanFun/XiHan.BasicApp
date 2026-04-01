<script lang="ts" setup>
import type { LoginFormAlign } from './LoginToolbar.vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { useAppStore } from '~/stores'
import AuthEntrySwitcher from './AuthEntrySwitcher.vue'
import LoginToolbar from './LoginToolbar.vue'

defineOptions({ name: 'AuthLayout' })

const formAlign = ref<LoginFormAlign>('right')
const { t } = useI18n()
const route = useRoute()
const appStore = useAppStore()

const appTitle = computed(
  () => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan Admin',
)
const appLogo = computed(
  () => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png',
)

const showFooter = computed(
  () => appStore.footerEnable && (appStore.copyrightEnable || appStore.footerShowDevInfo),
)
const showEntryTabs = computed(() =>
  ['/auth/login', '/auth/code-login', '/auth/qrcode-login'].includes(route.path),
)

const appVersion = __APP_VERSION__
const appBuildTime = __APP_BUILD_TIME__
const appHomepage = __APP_HOMEPAGE__
const appName = __APP_NAME__
const appAuthorName = __APP_AUTHOR_NAME__
const appAuthorUrl = __APP_AUTHOR_URL__
</script>

<template>
  <div
    class="auth-page relative min-h-screen overflow-hidden bg-[hsl(var(--background-deep))] text-[hsl(var(--foreground))]"
  >
    <div class="overflow-hidden absolute inset-0 pointer-events-none">
      <div
        class="auth-blob auth-blob--1 absolute h-[620px] w-[620px] rounded-full bg-[hsl(var(--primary)/0.24)] blur-[100px]"
      />
      <div
        class="auth-blob auth-blob--2 absolute h-[520px] w-[520px] rounded-full bg-[hsl(var(--info)/0.20)] blur-[120px]"
      />
      <div
        class="auth-blob auth-blob--3 absolute h-[580px] w-[580px] rounded-full bg-[hsl(var(--success)/0.16)] blur-[100px]"
      />
    </div>

    <LoginToolbar @layout-change="(align) => (formAlign = align)" />

    <div
      class="relative z-[1] mx-auto flex min-h-screen w-full max-w-[1420px] items-center px-4 py-14 sm:px-8"
    >
      <div
        class="auth-card relative w-full overflow-hidden rounded-[30px] border border-[hsl(var(--border))] shadow-[0_32px_80px_hsl(var(--foreground)/0.12)]"
      >
        <div
          class="grid relative grid-cols-1 w-full"
          :class="{
            'lg:grid-cols-[520px_1fr]': formAlign === 'left',
            'lg:grid-cols-1': formAlign === 'center',
            'lg:grid-cols-[1fr_520px]': formAlign === 'right',
          }"
        >
          <!-- 插图面板 -->
          <div
            class="relative hidden overflow-hidden bg-[hsl(var(--muted)/0.5)] px-10 py-14 lg:flex lg:flex-col xl:px-14 xl:py-16"
            :class="{
              'lg:order-1': formAlign === 'left',
              'lg:hidden': formAlign === 'center',
            }"
          >
            <div
              class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,hsl(var(--primary)/0.16),transparent_56%),radial-gradient(circle_at_80%_70%,hsl(var(--info)/0.12),transparent_44%)]"
            />

            <div class="relative z-[1] lg:absolute lg:left-10 lg:top-14 xl:left-14 xl:top-16">
              <div
                class="mb-7 flex h-[78px] w-[78px] items-center justify-center rounded-3xl bg-[hsl(var(--card))] p-2 shadow-xl shadow-[hsl(var(--foreground)/0.08)]"
              >
                <img :src="appLogo" :alt="appTitle" class="object-contain w-12 h-12">
              </div>
              <p
                class="mb-4 text-xs font-semibold uppercase tracking-[0.32em] text-[hsl(var(--primary))]"
              >
                {{ appTitle }}
              </p>
            </div>

            <div class="relative z-[1] flex w-full flex-1 items-center justify-center">
              <div class="inline-flex max-w-[92%] flex-col items-start">
                <h2 class="text-left text-[36px] font-semibold leading-[1.15] xl:text-[44px]">
                  {{ t('page.auth.slogan_title') }}
                </h2>
                <span
                  class="slogan-tag -mt-px inline-block rounded-full border-2 border-[hsl(var(--primary)/0.25)] bg-[hsl(var(--primary)/0.1)] px-3.5 py-1 text-[15px] leading-5 font-semibold text-[hsl(var(--primary))]"
                >
                  {{ t('page.auth.slogan_desc') }}
                </span>
              </div>
            </div>
          </div>

          <!-- 表单面板 -->
          <div
            class="flex min-h-[680px] justify-center px-6 py-12 sm:px-10 lg:h-[760px] lg:min-h-[760px] lg:px-14 lg:py-16"
            :class="{
              'lg:border-r lg:border-[hsl(var(--border))]': formAlign === 'left',
              'lg:border-l lg:border-[hsl(var(--border))]': formAlign === 'right',
              'items-start': showEntryTabs,
              'items-center': !showEntryTabs,
            }"
          >
            <div
              class="overflow-hidden w-full h-full"
              :class="formAlign === 'center' ? 'max-w-[560px]' : 'max-w-[460px]'"
            >
              <AuthEntrySwitcher v-if="showEntryTabs" class="mb-7" />
              <div class="overflow-hidden" :class="showEntryTabs ? 'min-h-[520px]' : ''">
                <router-view v-slot="{ Component }">
                  <transition name="auth-slide" mode="out-in">
                    <component :is="Component" />
                  </transition>
                </router-view>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Footer -->
    <footer
      v-if="showFooter"
      class="auth-footer absolute bottom-0 left-0 flex w-full flex-col items-center justify-center gap-1 px-4 py-3 text-xs text-[hsl(var(--muted-foreground))]"
    >
      <div v-if="appStore.footerShowDevInfo" class="leading-tight">
        <a :href="appHomepage" target="_blank" class="hover:underline">{{ appName }}</a>
        v{{ appVersion }}({{ appBuildTime }}) · by
        <a :href="appAuthorUrl" target="_blank" class="hover:underline">{{ appAuthorName }}</a>
      </div>
      <div
        v-if="appStore.copyrightEnable"
        class="flex flex-wrap gap-x-3 justify-center items-center"
      >
        <span>
          Copyright &copy; {{ appStore.copyrightDate || new Date().getFullYear() }}-{{
            new Date().getFullYear()
          }}
          <a
            v-if="appStore.copyrightSite"
            :href="appStore.copyrightSite"
            target="_blank"
            class="hover:underline"
          >
            {{ appStore.copyrightName }}
          </a>
          <span v-else>{{ appStore.copyrightName }}</span>
          . All Rights Reserved.
        </span>
        <a
          v-if="appStore.copyrightIcp"
          :href="appStore.copyrightIcpUrl || '#'"
          target="_blank"
          class="hover:underline"
        >
          {{ appStore.copyrightIcp }}
        </a>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.auth-card {
  background: hsl(var(--card) / 0.72);
  backdrop-filter: blur(24px);
  -webkit-backdrop-filter: blur(24px);
}

.slogan-tag {
  transition:
    background 0.3s ease,
    border-color 0.3s ease,
    color 0.3s ease;
}

.auth-blob {
  transition: background-color 0.6s ease;
  will-change: transform;
}

.auth-blob--1 {
  left: -8%;
  top: -12%;
  animation: blob-flow-1 16s ease-in-out infinite;
}

.auth-blob--2 {
  right: 10%;
  top: -6%;
  animation: blob-flow-2 20s ease-in-out infinite;
}

.auth-blob--3 {
  left: 20%;
  bottom: -16%;
  animation: blob-flow-3 18s ease-in-out infinite;
}

@keyframes blob-flow-1 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  25% {
    transform: translate(30%, 15%) scale(1.12);
  }

  50% {
    transform: translate(15%, 35%) scale(0.92);
  }

  75% {
    transform: translate(-10%, 20%) scale(1.06);
  }
}

@keyframes blob-flow-2 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  25% {
    transform: translate(-25%, 20%) scale(0.9);
  }

  50% {
    transform: translate(-35%, 40%) scale(1.1);
  }

  75% {
    transform: translate(-15%, 55%) scale(0.95);
  }
}

@keyframes blob-flow-3 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  25% {
    transform: translate(20%, -30%) scale(1.08);
  }

  50% {
    transform: translate(40%, -15%) scale(0.94);
  }

  75% {
    transform: translate(25%, -40%) scale(1.05);
  }
}

.auth-slide-enter-active,
.auth-slide-leave-active {
  transition: opacity 0.24s ease;
}

.auth-slide-enter-from,
.auth-slide-leave-to {
  opacity: 0;
}

.auth-footer :deep(a) {
  color: inherit;
  text-decoration: none;
}
</style>
