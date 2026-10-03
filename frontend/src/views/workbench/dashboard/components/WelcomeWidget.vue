<script setup lang="ts">
import { XhButton, XhTagRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'WelcomeWidget' })

const { t } = useI18n()

/** 当前版本，与页脚同一写法：v版本号(构建日期) */
const version = `v${__APP_VERSION__}(${__APP_BUILD_TIME__})`

/** 更新日志：BasicApp 文档站上的 changelog 页（仓库 docs/changelog.md） */
const CHANGELOG_URL = 'https://basicapp.docs.xihanfun.com/changelog'

/** 源码仓库的三处托管：GitHub 取 package.json 的 repository（与页脚、登录页同源），另有 Gitee 与 GitCode */
const repositories = computed(() => [
  { key: 'github', label: t('workbench.widgets.welcome_github'), icon: 'simple-icons:github', url: __APP_REPOSITORY__ },
  { key: 'gitee', label: t('workbench.widgets.welcome_gitee'), icon: 'simple-icons:gitee', url: 'https://gitee.com/XiHanFun/XiHan.BasicApp' },
  { key: 'gitcode', label: t('workbench.widgets.welcome_gitcode'), icon: 'simple-icons:gitcode', url: 'https://gitcode.com/XiHanFun/XiHan.BasicApp' },
])

function open(url: string) {
  window.open(url, '_blank', 'noopener')
}
</script>

<template>
  <WidgetCard icon="lucide:sparkles" :title="t('workbench.widgets.welcome.title')">
    <template #badge>
      <XhTagRoot variant="subtle" size="sm" tone="brand">
        {{ t('workbench.widgets.welcome_official') }}
      </XhTagRoot>
    </template>
    <!-- 标题、介绍与版本号紧挨着排，去掉全局排版给 h3 / p 的上下外边距，只留列间距 -->
    <div class="flex h-full flex-col gap-2">
      <h3 class="m-0 text-lg font-semibold text-foreground">
        {{ t('workbench.widgets.welcome_greeting') }}
      </h3>
      <p class="m-0 text-sm leading-relaxed text-muted-foreground">
        {{ t('workbench.widgets.welcome_desc') }}
      </p>
      <p class="m-0 text-xs text-muted-foreground tabular-nums">
        {{ t('workbench.widgets.welcome_version', { version }) }}
      </p>
      <div class="mt-auto flex flex-wrap gap-2 pt-2">
        <XhButton size="sm" tone="brand" variant="subtle" @click="open('https://docs.xihanfun.com')">
          <Icon icon="lucide:book-open" />
          {{ t('workbench.widgets.welcome_docs') }}
        </XhButton>
        <XhButton size="sm" variant="ghost" @click="open(CHANGELOG_URL)">
          <Icon icon="lucide:history" />
          {{ t('workbench.widgets.welcome_changelog') }}
        </XhButton>
        <XhButton v-for="repository in repositories" :key="repository.key" size="sm" variant="ghost" @click="open(repository.url)">
          <Icon :icon="repository.icon" />
          {{ repository.label }}
        </XhButton>
      </div>
    </div>
  </WidgetCard>
</template>
