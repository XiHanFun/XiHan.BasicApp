import type { AlertOptions, ConfirmOptions, DialogService, LoadingBarService, NotificationMessageOptions, NotificationService, ToastMessageOptions, ToastService } from '@xihan-ui/vue'
import { createDialogService, createLoadingBarService, createNotificationService, createToastService } from '@xihan-ui/vue'
import { $t } from '~/locales'
import { xhConfigValue, xhTranslationsOfCurrentLocale } from './xh-config'

/**
 * 命令式 UI 服务：轻提示与确认框，外加顶部进度条的开关。
 *
 * 三者都要能在组件树之外调用——请求拦截器、pinia store、路由守卫都会用到，
 * 那些地方拿不到组件实例。XiHan.UI 的三个服务本身就是为此设计的：
 * 各自挂一个独立的宿主应用到 body，与业务应用的组件树无关，
 * 语言与内建文案由 options.config 喂同一份取值函数，切语言时跟着重渲。
 *
 * 服务实例懒建：createXxxService 需要 document，模块被 node 侧的测试引到时不能当场炸。
 */

let toastInstance: ToastService | null = null
let notificationInstance: NotificationService | null = null
let dialogInstance: DialogService | null = null
let loadingBarInstance: LoadingBarService | null = null

function toastService(): ToastService {
  // 顶部居中：与旧版轻提示的落位一致。落位是整个服务的口径，不逐条各去一处
  toastInstance ??= createToastService({
    placement: 'top',
    max: 5,
    config: xhConfigValue,
    toastTranslations: () => xhTranslationsOfCurrentLocale().toast ?? {},
  })
  return toastInstance
}

/** 通知服务：主动推来的消息落右下角，标题加正文两层。 */
function notificationService(): NotificationService {
  notificationInstance ??= createNotificationService({
    placement: 'bottom-end',
    max: 5,
    config: xhConfigValue,
    translations: () => xhTranslationsOfCurrentLocale().notification ?? {},
    itemTranslations: () => xhTranslationsOfCurrentLocale().toast ?? {},
  })
  return notificationInstance
}

/** 确认框服务；确定/取消的兜底文案按当前语言取，调用点显式给了就以调用点为准。 */
export function dialogService(): DialogService {
  dialogInstance ??= createDialogService({
    config: xhConfigValue,
    okText: () => $t('common.actions.confirm'),
    cancelText: () => $t('common.actions.cancel'),
  })
  return dialogInstance
}

/**
 * 顶部进度条服务。条子自带宿主应用挂到 body，位置是 fixed，与业务组件树无关。
 *
 * 在途计数由服务自己管：路由守卫的重定向链会连开好几笔，
 * 布尔开关下第一笔收尾就把条子收了。
 */
function loadingBarService(): LoadingBarService {
  loadingBarInstance ??= createLoadingBarService({ config: xhConfigValue })
  return loadingBarInstance
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

/**
 * 通知：系统或他人主动推来的一条消息，标题加正文两层，可以常驻不消失。
 *
 * 与轻提示的分工是「谁发起的」——用户刚点了一下、只要一句结果反馈的走 toast。
 */
export const notification = {
  create: (options?: Parameters<NotificationService['create']>[0]) => notificationService().create(options),
  update: (id: string, options: Parameters<NotificationService['update']>[1]) => notificationService().update(id, options),
  dismiss: (id: string) => notificationService().dismiss(id),
  dismissAll: () => notificationService().dismissAll(),
  info: (title: string, options?: NotificationMessageOptions) => notificationService().info(title, options),
  success: (title: string, options?: NotificationMessageOptions) => notificationService().success(title, options),
  warning: (title: string, options?: NotificationMessageOptions) => notificationService().warning(title, options),
  error: (title: string, options?: NotificationMessageOptions) => notificationService().error(title, options),
}

/**
 * 确认框与告知框。
 *
 * confirm 返回 Promise<boolean>：确认走完 onOk 才 resolve(true)，取消/Esc resolve(false)。
 * onOk 返回 Promise 时确认钮自动进入 pending 并拦住关闭，拒绝则保持打开以便重试。
 * 删除这类不可逆操作传 `tone: 'danger'`，确认钮即转危险色。
 */
export const dialog = {
  confirm: (options: ConfirmOptions) => dialogService().confirm(options),
  info: (options: AlertOptions) => dialogService().info(options),
  success: (options: AlertOptions) => dialogService().success(options),
  warning: (options: AlertOptions) => dialogService().warning(options),
  error: (options: AlertOptions) => dialogService().error(options),
}

/** 危险操作确认：带警示徽记与危险色确认钮的两按钮确认框。 */
export function confirmDanger(options: Omit<ConfirmOptions, 'tone' | 'badge'>): Promise<boolean> {
  return dialog.confirm({ ...options, tone: 'danger', badge: 'warning' })
}

/** 顶部进度条开关。落位、爬升与收尾的动效都归服务。 */
export const loadingBar = {
  /** 在途计数 +1，从 0 起跳即开始爬升。 */
  start: () => loadingBarService().start(),
  /** 在途计数 -1，归零才收。 */
  finish: () => loadingBarService().finish(),
  /** 不管还剩几笔在途一律收掉。 */
  finishAll: () => loadingBarService().finishAll(),
  /** 出错收尾：条子转危险色再收，与正常收尾区分开。 */
  error: () => loadingBarService().error(),
}

/** 应用卸载时释放四个宿主应用（热更新与测试用）。 */
export function disposeUiServices(): void {
  toastInstance?.dispose()
  toastInstance = null
  notificationInstance?.dispose()
  notificationInstance = null
  dialogInstance?.dispose()
  dialogInstance = null
  loadingBarInstance?.dispose()
  loadingBarInstance = null
}
