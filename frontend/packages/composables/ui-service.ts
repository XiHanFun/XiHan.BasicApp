import type { Tone } from '@xihan-ui/kernel'
import type { AlertOptions, ConfirmOptions, DialogService, ToastMessageOptions, ToastService, XhTranslationOverrides } from '@xihan-ui/vue'
import { createDialogService, createToastService } from '@xihan-ui/vue'
import { reactive } from 'vue'
import { $t, i18n } from '~/locales'
import { xhTranslations } from '~/locales/xihan-ui'

/**
 * 命令式 UI 服务：轻提示、确认框、顶部进度条。
 *
 * 三者都要能在组件树之外调用——请求拦截器、pinia store、路由守卫都会用到，
 * 那些地方拿不到组件实例。XiHan.UI 的 toast / dialog 服务本身就是为此设计的：
 * 各自挂一个独立的宿主应用到 body，与业务应用的组件树无关。
 *
 * 服务实例懒建：createXxxService 需要 document，模块被 node 侧的测试引到时不能当场炸。
 */

let toastInstance: ToastService | null = null
let dialogInstance: DialogService | null = null

/**
 * 两个服务各自挂一个独立宿主应用，拿不到 provideXhConfig 的注入，
 * 内建文案要在创建时单独喂一份。服务是懒建的，此刻 i18n 已就绪。
 */
function currentXhTranslations(): XhTranslationOverrides {
  return xhTranslations[i18n.global.locale.value] ?? xhTranslations['zh-CN']!
}

function toastService(): ToastService {
  const t = currentXhTranslations()
  // 顶部居中：与旧版轻提示的落位一致。右下角那几条通知在调用点单独传 placement
  toastInstance ??= createToastService({
    placement: 'top',
    max: 5,
    translations: t.toaster,
    toastTranslations: t.toast,
  })
  return toastInstance
}

function dialogService(): DialogService {
  // 确定/取消的兜底文案在每次调用时按当前语言取，这里给的只是服务档缺省值
  dialogInstance ??= createDialogService()
  return dialogInstance
}

/**
 * 轻提示。用法与位置同旧版：`toast.success('保存成功')`。
 * loading 返回 id，收尾用 `toast.update(id, { type: 'success', title: '完成' })`。
 */
export const toast = {
  create: (options?: Parameters<ToastService['create']>[0]) => toastService().create(options),
  update: (id: string, options: Parameters<ToastService['update']>[1]) => toastService().update(id, options),
  dismiss: (id: string) => toastService().dismiss(id),
  dismissAll: () => toastService().dismissAll(),
  info: (msg: string, options?: ToastMessageOptions) => toastService().info(msg, options),
  success: (msg: string, options?: ToastMessageOptions) => toastService().success(msg, options),
  warning: (msg: string, options?: ToastMessageOptions) => toastService().warning(msg, options),
  error: (msg: string, options?: ToastMessageOptions) => toastService().error(msg, options),
  /**
   * 返回带收尾方法的句柄：等待期的提示要么改写成结果、要么撤掉，
   * 拿着 id 再调一次服务不如把两个动作挂在句柄上顺手。
   */
  loading: (msg: string, options?: ToastMessageOptions) => {
    const id = toastService().loading(msg, options)
    return {
      id,
      destroy: () => toastService().dismiss(id),
      update: (patch: Parameters<ToastService['update']>[1]) => toastService().update(id, patch),
    }
  },
}

/** 补上当前语言的确定/取消文案；调用点显式给了就以调用点为准。 */
function withLocaleText<T extends { okText?: string, cancelText?: string }>(options: T): T {
  return {
    ...options,
    okText: options.okText ?? $t('common.actions.confirm'),
    cancelText: options.cancelText ?? $t('common.actions.cancel'),
  }
}

/**
 * 确认框与告知框。
 *
 * confirm 返回 Promise<boolean>：确认走完 onOk 才 resolve(true)，取消/Esc resolve(false)。
 * onOk 返回 Promise 时确认钮自动进入 pending 并拦住关闭，拒绝则保持打开以便重试。
 * 删除这类不可逆操作传 `tone: 'danger'`，确认钮即转危险色。
 */
export const dialog = {
  confirm: (options: ConfirmOptions) => dialogService().confirm(withLocaleText(options)),
  info: (options: AlertOptions) => dialogService().info(withLocaleText(options)),
  success: (options: AlertOptions) => dialogService().success(withLocaleText(options)),
  warning: (options: AlertOptions) => dialogService().warning(withLocaleText(options)),
  error: (options: AlertOptions) => dialogService().error(withLocaleText(options)),
}

/** 危险操作确认：带警示徽记与危险色确认钮的两按钮确认框。 */
export function confirmDanger(options: Omit<ConfirmOptions, 'tone' | 'badge'>): Promise<boolean> {
  return dialog.confirm({ ...options, tone: 'danger', badge: 'warning' })
}

/** 顶部进度条状态。App.vue 把它绑到 XhLoadingBarRoot，路由守卫与请求层只管翻这几个开关。 */
export const loadingBarState = reactive({
  loading: false,
  tone: 'brand' as Tone,
})

export const loadingBar = {
  start(): void {
    loadingBarState.tone = 'brand'
    loadingBarState.loading = true
  },
  finish(): void {
    loadingBarState.loading = false
  },
  /** 出错收尾：条子转危险色再收，与正常收尾区分开。 */
  error(): void {
    loadingBarState.tone = 'danger'
    loadingBarState.loading = false
  },
}

/** 应用卸载时释放两个宿主应用（热更新与测试用）。 */
export function disposeUiServices(): void {
  toastInstance?.dispose()
  toastInstance = null
  dialogInstance?.dispose()
  dialogInstance = null
}
