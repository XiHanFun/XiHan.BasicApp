<script setup lang="ts">
import { computed } from 'vue'
import { Icon } from '~/iconify'

export interface ChatContextMenuItem {
  key: string
  label: string
  icon?: string
  /** 危险操作（红色，如撤回） */
  danger?: boolean
  /** 顶部加分隔线 */
  divided?: boolean
}

defineOptions({ name: 'ChatContextMenu' })

const props = withDefaults(defineProps<{
  x: number
  y: number
  items: ChatContextMenuItem[]
  /** 快捷表情条（空则不显示；QQ 式与菜单分离成两张卡片） */
  reactions?: string[]
}>(), {
  reactions: () => [],
})

const emit = defineEmits<{
  select: [key: string]
  react: [emoji: string]
}>()

const show = defineModel<boolean>('show', { default: false })

/** 视口边缘收敛（按估算尺寸夹取，避免菜单溢出屏幕） */
const wrapStyle = computed(() => {
  const menuWidth = 168
  const reactionsWidth = props.reactions.length ? props.reactions.length * 36 + 20 : 0
  const estWidth = Math.max(menuWidth, reactionsWidth)
  const estHeight = (props.reactions.length ? 50 : 0) + props.items.length * 34 + 14
  const vw = window.innerWidth
  const vh = window.innerHeight
  return {
    left: `${Math.max(8, Math.min(props.x, vw - estWidth - 12))}px`,
    top: `${Math.max(8, Math.min(props.y, vh - estHeight - 12))}px`,
  }
})

function handleSelect(key: string) {
  show.value = false
  emit('select', key)
}

function handleReact(emoji: string) {
  show.value = false
  emit('react', emoji)
}
</script>

<template>
  <Teleport to="body">
    <div
      v-if="show"
      class="chat-ctx-overlay"
      @click="show = false"
      @contextmenu.prevent="show = false"
    />
    <Transition name="chat-ctx-fade">
      <div v-if="show" class="chat-ctx-wrap" :style="wrapStyle" @contextmenu.prevent>
        <!-- 快捷表情条（独立卡片，QQ 式） -->
        <div v-if="reactions.length" class="chat-ctx-card chat-ctx-reactions">
          <button
            v-for="emoji in reactions"
            :key="emoji"
            type="button"
            class="chat-ctx-react-btn"
            @click="handleReact(emoji)"
          >
            {{ emoji }}
          </button>
        </div>

        <!-- 操作菜单卡片 -->
        <div v-if="items.length" class="chat-ctx-card chat-ctx-menu">
          <template v-for="item in items" :key="item.key">
            <div v-if="item.divided" class="chat-ctx-divider" />
            <button
              type="button"
              class="chat-ctx-item"
              :class="{ 'chat-ctx-item--danger': item.danger }"
              @click="handleSelect(item.key)"
            >
              <Icon v-if="item.icon" :icon="item.icon" width="15" height="15" class="shrink-0" />
              <span>{{ item.label }}</span>
            </button>
          </template>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.chat-ctx-overlay {
  position: fixed;
  inset: 0;
  z-index: 2999;
}

.chat-ctx-wrap {
  position: fixed;
  z-index: 3000;
  display: flex;
  flex-direction: column;
  gap: 6px;
  align-items: flex-start;
}

.chat-ctx-card {
  background: hsl(var(--card));
  border: 1px solid hsl(var(--border));
  border-radius: 10px;
  box-shadow:
    0 8px 30px hsl(var(--foreground) / 10%),
    0 2px 8px hsl(var(--foreground) / 5%);
}

.chat-ctx-reactions {
  display: flex;
  gap: 2px;
  padding: 6px 8px;
}

.chat-ctx-react-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 8px;
  background: transparent;
  font-size: 18px;
  cursor: pointer;
  transition:
    background 0.12s ease,
    transform 0.12s ease;
}

.chat-ctx-react-btn:hover {
  background: hsl(var(--accent));
  transform: scale(1.15);
}

.chat-ctx-menu {
  min-width: 148px;
  padding: 5px;
}

.chat-ctx-item {
  display: flex;
  gap: 9px;
  align-items: center;
  width: 100%;
  padding: 7px 28px 7px 10px;
  border: none;
  border-radius: 7px;
  background: transparent;
  color: hsl(var(--foreground));
  font-size: 13px;
  line-height: 1.2;
  text-align: left;
  cursor: pointer;
  transition: background 0.12s ease;
}

.chat-ctx-item:hover {
  background: hsl(var(--accent));
}

.chat-ctx-item--danger {
  color: hsl(var(--destructive, 0 84% 60%));
}

.chat-ctx-divider {
  height: 1px;
  margin: 4px 8px;
  background: hsl(var(--border));
}

.chat-ctx-fade-enter-active {
  transition:
    opacity 0.12s ease-out,
    transform 0.12s ease-out;
}

.chat-ctx-fade-leave-active {
  transition: opacity 0.1s ease-in;
}

.chat-ctx-fade-enter-from {
  opacity: 0;
  transform: scale(0.96);
}

.chat-ctx-fade-leave-to {
  opacity: 0;
}
</style>
