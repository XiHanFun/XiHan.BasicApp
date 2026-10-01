<script setup lang="ts">
import { XhImageFallback, XhImageImage, XhImageRoot, XhSpinner } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'ChatMessageImage' })

defineProps<{
  /** 可显示的图片地址；还在换取预签名地址时为空 */
  url: string
  alt?: null | string
  /** 相册多图时用方形缩略图；单图用自适应大图 */
  thumb?: boolean
}>()

/** 点了这张：消息经 XhImageViewerRoot 插槽的 setOpen 打开看片浮层，从这张开始看 */
const emit = defineEmits<{ select: [] }>()

const { t } = useI18n()
</script>

<template>
  <!-- 普通按钮而非 XhImageViewerTrigger：一个浮层只有一个触发器 id，相册每张各挂一个会撞 id；
       关闭浮层后焦点仍回到按下的这颗。加载中图片隐藏，可及名不能只靠 img 的 alt -->
  <button
    v-if="url"
    type="button"
    class="chat-image"
    :class="thumb ? 'chat-image--thumb' : 'chat-image--single'"
    aria-haspopup="dialog"
    :aria-label="alt || t('chat.thread.view_image')"
    @click="emit('select')"
  >
    <!-- src / alt 是根部件的属性：写在 image 部件上状态机拿不到地址，直接落到失败态 -->
    <XhImageRoot v-slot="{ status }" :src="url" :alt="alt ?? undefined">
      <XhImageImage class="chat-image__img" />
      <XhImageFallback class="chat-image__fallback">
        <Icon v-if="status === 'error'" icon="lucide:image-off" width="20" height="20" />
        <XhSpinner v-else />
      </XhImageFallback>
    </XhImageRoot>
  </button>
  <div
    v-else
    class="flex items-center justify-center rounded bg-muted/40"
    :class="thumb ? 'h-[88px] w-[88px]' : 'h-24 w-40'"
  >
    <XhSpinner />
  </div>
</template>

<style scoped>
/* 全局按钮重置留着 UA 内距，图片按钮得自己清掉，否则图边与按钮边错开一圈 */
.chat-image {
  padding: 0;
  vertical-align: top;
  border-radius: var(--xh-shape-control);
  cursor: zoom-in;
}

.chat-image:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

/* 相册缩略图：宽度由消息里的网格列给出，这里只定成方形 */
.chat-image--thumb {
  --xh-image-ratio: 1;

  display: block;
  inline-size: 100%;
}

/* 单图：按原图比例显示，长宽都不超过 240px，超出的一边居中裁切 */
.chat-image--single {
  --chat-image-max: calc(var(--xh-space-8) * 7.5);

  max-inline-size: var(--chat-image-max);
}

.chat-image--single .chat-image__img {
  max-block-size: var(--chat-image-max);
}

/* 单图取图中与失败时没有原图比例可依：占位与换地址时的那块同尺寸，图出来前后不跳 */
.chat-image--single .chat-image__fallback:not([data-state='loaded']) {
  inline-size: calc(var(--xh-space-8) * 5);
  block-size: calc(var(--xh-space-8) * 3);
}
</style>
