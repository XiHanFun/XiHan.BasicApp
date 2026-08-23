<script setup lang="ts">
import { XhButton } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'WelcomeWidget' })

const { t } = useI18n()

function open(url: string) {
  window.open(url, '_blank', 'noopener')
}
</script>

<template>
  <WidgetCard icon="lucide:sparkles" :title="t('workbench.widgets.welcome.title')">
    <div class="flex h-full flex-col gap-3">
      <h3 class="text-lg font-semibold text-foreground">
        {{ t('workbench.widgets.welcome_greeting') }}
      </h3>
      <p class="text-sm leading-relaxed text-muted-foreground">
        {{ t('workbench.widgets.welcome_tip') }}
      </p>
      <div class="mt-auto flex gap-2 pt-2">
        <XhButton size="sm" tone="brand" variant="subtle" @click="open('https://docs.xihanfun.com')">
          <Icon icon="lucide:book-open" />
          {{ t('workbench.widgets.welcome_docs') }}
        </XhButton>
        <XhButton size="sm" variant="ghost" @click="open('https://github.com/XiHanFun/XiHan.BasicApp')">
          <Icon icon="tabler:brand-github" />
          {{ t('workbench.widgets.welcome_github') }}
        </XhButton>
      </div>
    </div>
  </WidgetCard>
</template>
