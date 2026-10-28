/**
 * 分格验证码与表单串之间的换算。
 *
 * PinInput 的值是逐格数组（长度恒等于格数，空格为空串），表单、接口与发码回填的调试码用的是
 * 拼接后的串。串到数组只在「外部整份写入」（回填、清空）时做一次；用户逐格编辑那一路不走这里：
 * 清掉中间一格后拼串会把空位挤掉，再拆回数组时后面的字符整体前移，用户改的位置就不对了。
 */

/** 短信 / 邮件一次性验证码的位数，与服务端 OneTimeCodeOptions 的默认 6 位一致 */
export const OTP_CODE_LENGTH = 6

/** 登录图形验证码的位数，与服务端 CaptchaService 的 CodeLength 一致 */
export const CAPTCHA_CODE_LENGTH = 4

/** 把一段串按格拆开：超出格数的截掉，不足的补空串，每格一个码点 */
export function splitPinCode(code: string, length: number): string[] {
  const chars = [...code]
  return Array.from({ length }, (_, index) => chars[index] ?? '')
}
