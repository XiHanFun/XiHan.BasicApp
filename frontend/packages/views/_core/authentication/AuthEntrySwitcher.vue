<script lang="ts" setup>
import { useResizeObserver } from '@vueuse/core'
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { XSegmented } from '~/components'
import { CODE_LOGIN_PATH, EMAIL_LOGIN_PATH, LOGIN_PATH, QRCODE_LOGIN_PATH } from '~/constants'

/**
 * 登录方式切换。四种入口互斥、各自是一条路由，没有面板，
 * 所以用分段控制器而不是标签页——tablist 却没有 tabpanel 是错的语义。
 * 标签用 page.auth.entry.* 短说法，不借页面标题键：登录卡片右栏固定宽，长标题会把扫码入口挤出容器。
 */
defineOptions({ name: 'AuthEntrySwitcher' })

const route = useRoute()
const router = useRouter()
const { t, locale } = useI18n()

const entryList = computed(() => [
  { value: LOGIN_PATH, label: t('page.auth.entry.account'), icon: 'lucide:user' },
  { value: CODE_LOGIN_PATH, label: t('page.auth.entry.mobile'), icon: 'lucide:smartphone' },
  { value: EMAIL_LOGIN_PATH, label: t('page.auth.entry.email'), icon: 'lucide:mail' },
  { value: QRCODE_LOGIN_PATH, label: t('page.auth.entry.qrcode'), icon: 'lucide:qr-code' },
])

/**
 * 窄屏（手机）放不下四段文字时改成只显示图标，文字留给屏幕阅读器。
 * 组件库的分段控制器不支持横向滚动，文字又不能折行，只能换形态。
 * 是否放得下按「纯文字形态实际要的宽度」量，而不是写死断点：德文 / 印地文标签比中文长，断点各语言不同。
 * 宽屏只显示文字不加图标：桌面右栏宽度刚好容下四段文字，再加图标反而会挤不下。
 */
const root = ref<HTMLElement | null>(null)
const iconOnly = ref(false)
/** 纯文字形态需要的宽度；在文字形态溢出的那一刻量下来，之后容器够宽了才切回文字 */
let textWidth = 0

function measure() {
  const container = root.value
  const segmented = container?.firstElementChild as HTMLElement | null | undefined
  if (!container || !segmented) {
    return
  }
  if (!iconOnly.value) {
    if (segmented.scrollWidth > segmented.clientWidth + 1) {
      textWidth = segmented.scrollWidth
      iconOnly.value = true
    }
  }
  else if (container.clientWidth >= textWidth) {
    iconOnly.value = false
  }
}

/** 文字形态不带图标（见上），图标形态才带 */
const shownEntries = computed(() => iconOnly.value ? entryList.value : entryList.value.map(({ icon: _icon, ...entry }) => entry))

useResizeObserver(root, measure)
onMounted(() => nextTick(measure))
// 切回文字后若仍放不下（容器宽度没变、观察器不会再触发）要立刻再量一次
watch(iconOnly, (next) => {
  if (!next) {
    nextTick(measure)
  }
})
// 换语言后标签长度变了，旧的量测作废
watch(locale, () => {
  textWidth = 0
  iconOnly.value = false
  nextTick(measure)
})

/** 选中项取自当前路由；选中即跳转，不另存一份状态 */
const activeEntry = computed({
  get: () => {
    const path = route.path
    return entryList.value.some(item => item.value === path) ? path : LOGIN_PATH
  },
  set: (next: string) => {
    if (route.path !== next) {
      router.push(next)
    }
  },
})
</script>

<template>
  <!-- 外包一层给观察器量容器宽度：分段控制器自身宽度会随形态变，量它判断不了「够不够宽」 -->
  <div ref="root">
    <XSegmented
      v-model:value="activeEntry"
      block
      size="lg"
      :options="shownEntries"
      :icon-only="iconOnly"
      :aria-label="t('page.auth.login_method')"
    />
  </div>
</template>
