/**
 * 分格验证码换算。
 *
 * 只锁两条对外可见的约定：拆出来的数组长度恒等于格数，多余的截、不足的补；
 * 一个格子放一个码点，代理对不会被劈成两半。
 */
import { describe, expect, it } from 'vitest'
import { CAPTCHA_CODE_LENGTH, OTP_CODE_LENGTH, splitPinCode } from './pin-code'

describe('splitPinCode', () => {
  it('按格数拆开，不足的格补空串', () => {
    expect(splitPinCode('123', OTP_CODE_LENGTH)).toEqual(['1', '2', '3', '', '', ''])
    expect(splitPinCode('', CAPTCHA_CODE_LENGTH)).toEqual(['', '', '', ''])
  })

  it('超出格数的部分截掉', () => {
    expect(splitPinCode('1234567', OTP_CODE_LENGTH)).toEqual(['1', '2', '3', '4', '5', '6'])
  })

  it('按码点而不是 UTF-16 单元拆格', () => {
    expect(splitPinCode('😀1', 3)).toEqual(['😀', '1', ''])
  })
})
