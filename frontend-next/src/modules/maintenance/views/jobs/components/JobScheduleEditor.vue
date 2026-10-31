<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XNumberInput, XSegmented } from '~/components'
import CronExpression from '~/components/common/CronExpression.vue'
import { intervalExpression, intervalSecondsOf } from '../job'

/**
 * 计划编辑：照 4.x 任务表单的「触发方式」——Cron 用 4.x 的 Cron 构建器（输入框加「构建」弹窗），间隔填秒数。
 * 对外只有一个计划文本（v-model:expression）：Cron 即表达式本身，间隔写成 `every [d.]hh:mm:ss`，与后端的计划写法一致。
 */
defineOptions({ name: 'JobScheduleEditor' })

const props = defineProps<{
  expression: string
  invalid?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:expression', value: string): void
}>()

type Kind = 'cron' | 'interval'

const { t } = useI18n()
const kind = ref<Kind>('cron')
const seconds = ref<number | null>(null)
const cron = ref('')

const kindOptions = computed(() => [
  { value: 'cron' as const, label: t('maintenance.jobs.kind_cron') },
  { value: 'interval' as const, label: t('maintenance.jobs.kind_interval') },
])

/** 自己刚发出去的计划：父级回传时不再回填，免得清空间隔秒数时被当成 Cron */
let emitted: string | null = null

// 外部换了计划（打开弹窗、点预设）就按它的写法回填：every … 是间隔，其余当 Cron
watch(() => props.expression, (value) => {
  if (value === emitted)
    return
  const interval = intervalSecondsOf(value)
  if (interval !== null) {
    kind.value = 'interval'
    seconds.value = interval
  }
  else {
    kind.value = 'cron'
    cron.value = value
  }
}, { immediate: true })

function emitCurrent() {
  emitted = kind.value === 'cron' ? cron.value : (seconds.value && seconds.value > 0 ? intervalExpression(seconds.value) : '')
  emit('update:expression', emitted)
}

function onKind(value: unknown) {
  kind.value = value === 'interval' ? 'interval' : 'cron'
  emitCurrent()
}

function onCron(value: string) {
  cron.value = value
  emitCurrent()
}

function onSeconds(value: number | null) {
  seconds.value = value
  emitCurrent()
}
</script>

<template>
  <div class="job-schedule-editor">
    <XSegmented :value="kind" :options="kindOptions" @update:value="onKind" />
    <CronExpression v-if="kind === 'cron'" :value="cron" @update:value="onCron" />
    <XNumberInput
      v-else
      :value="seconds"
      :min="1"
      :invalid="invalid"
      :placeholder="t('maintenance.jobs.interval_placeholder')"
      @update:value="onSeconds"
    >
      <template #suffix>
        {{ t('maintenance.jobs.seconds') }}
      </template>
    </XNumberInput>
  </div>
</template>

<style scoped>
.job-schedule-editor {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}
</style>
