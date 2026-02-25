<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { NCard, NDivider, NIcon, NTag } from 'naive-ui'
import { useI18n } from 'vue-i18n'

defineOptions({ name: 'AboutPage' })

const { t } = useI18n()

const frameworks = [
  { name: 'Vue 3', icon: 'logos:vue', href: 'https://vuejs.org' },
  { name: 'Naive UI', icon: 'logos:naiveui', href: 'https://www.naiveui.com' },
  { name: 'Vite', icon: 'logos:vitejs', href: 'https://vitejs.dev' },
  { name: 'Pinia', icon: 'logos:pinia', href: 'https://pinia.vuejs.org' },
  { name: 'TypeScript', icon: 'logos:typescript-icon', href: 'https://www.typescriptlang.org' },
  { name: 'Tailwind CSS', icon: 'logos:tailwindcss-icon', href: 'https://tailwindcss.com' },
]
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-4 p-4">
    <!-- 应用信息 -->
    <NCard :bordered="false">
      <div class="flex items-center gap-4">
        <div class="flex h-14 w-14 items-center justify-center rounded-2xl bg-[hsl(var(--primary)/0.1)]">
          <NIcon size="32" color="hsl(var(--primary))">
            <Icon icon="lucide:layout-dashboard" />
          </NIcon>
        </div>
        <div>
          <div class="text-xl font-bold text-[hsl(var(--foreground))]">
            XiHan BasicApp
          </div>
          <div class="mt-1 text-sm text-[hsl(var(--muted-foreground))]">
            {{ t('page.about.description') }}
          </div>
        </div>
      </div>
      <NDivider class="my-4" />
      <div class="grid grid-cols-2 gap-3 text-sm sm:grid-cols-3">
        <div class="space-y-1">
          <div class="text-xs text-[hsl(var(--muted-foreground))]">
            {{ t('page.about.version') }}
          </div>
          <NTag type="primary" size="small">
            v1.0.0
          </NTag>
        </div>
        <div class="space-y-1">
          <div class="text-xs text-[hsl(var(--muted-foreground))]">
            {{ t('page.about.license') }}
          </div>
          <NTag size="small">
            MIT
          </NTag>
        </div>
        <div class="space-y-1">
          <div class="text-xs text-[hsl(var(--muted-foreground))]">
            {{ t('page.about.author') }}
          </div>
          <span class="text-[hsl(var(--foreground))]">XiHan Team</span>
        </div>
      </div>
    </NCard>

    <!-- 技术栈 -->
    <NCard :title="t('page.about.tech_stack')" :bordered="false">
      <div class="grid grid-cols-3 gap-3 sm:grid-cols-6">
        <a
          v-for="f in frameworks"
          :key="f.name"
          :href="f.href"
          target="_blank"
          rel="noopener noreferrer"
          class="flex flex-col items-center gap-2 rounded-lg p-3 text-center transition-colors hover:bg-[hsl(var(--accent))]"
        >
          <NIcon size="28">
            <Icon :icon="f.icon" />
          </NIcon>
          <span class="text-xs text-[hsl(var(--muted-foreground))]">{{ f.name }}</span>
        </a>
      </div>
    </NCard>

    <!-- 开源地址 -->
    <NCard :bordered="false">
      <div class="flex items-center justify-between">
        <div>
          <div class="font-medium text-[hsl(var(--foreground))]">
            {{ t('page.about.open_source') }}
          </div>
          <div class="mt-1 text-sm text-[hsl(var(--muted-foreground))]">
            {{ t('page.about.open_source_desc') }}
          </div>
        </div>
        <a
          href="https://github.com/XiHanFun/XiHan.BasicApp"
          target="_blank"
          rel="noopener noreferrer"
          class="flex items-center gap-1.5 rounded-lg border border-[hsl(var(--border))] px-3 py-2 text-sm transition-colors hover:bg-[hsl(var(--accent))]"
        >
          <NIcon size="16">
            <Icon icon="lucide:github" />
          </NIcon>
          GitHub
        </a>
      </div>
    </NCard>
  </div>
</template>
