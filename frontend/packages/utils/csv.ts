/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import Papa from 'papaparse'

/**
 * 解析 CSV 文本为行数组，跳过空行
 * 应用层不直接依赖 papaparse，统一经 ~/utils 取用
 */
export function parseCsvRows(text: string): string[][] {
  const parsed = Papa.parse<string[]>(text, { skipEmptyLines: true })
  return parsed.data
}

/**
 * 写出 CSV 时防公式注入：公式起始字符开头的单元格前加单引号
 * 单个字符（如占位的 -）与普通数字（负数、带加号的手机号）不是公式，保持原样
 */
export function csvNeutralize(value: string): string {
  return value.length > 1 && /^[=+\-@\t\r\n]/.test(value) && !/^[+-]?\d+(?:\.\d+)?$/.test(value)
    ? `'${value}`
    : value
}

/** CSV 单元格：防公式注入之后，含逗号、引号或换行时包裹双引号并转义内部引号 */
export function csvCell(value: string): string {
  const safe = csvNeutralize(value)
  return /[",\r\n]/.test(safe)
    ? `"${safe.replace(/"/g, '""')}"`
    : safe
}
