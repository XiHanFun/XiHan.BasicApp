#!/usr/bin/env node
// 从一轮 CI 的集成测试结果（trx）算出各库每个测试类的耗时，写成 test-shard.mjs 用的权重文件
// 用法：node test-weights.mjs <结果目录> <输出文件> <来源说明>
//   结果目录：gh run download <运行号> --pattern "backend-test-results-*-*" -D <目录> 下载的各片结果（目录名带库名）
//   来源说明：写进文件，标明是哪一轮 CI 测出的，如 "CI 45071459904（7930893aa）"
// 一个类的耗时是它跑了的用例各自时长之和（跳过的不算），四舍五入到秒；perTest 是该库跑了的用例的平均时长，给还没测过的类估重量
import fs from 'node:fs'
import path from 'node:path'

const [root, output, source] = process.argv.slice(2)
if (!root || !output || !source) {
  console.error('usage: node test-weights.mjs <results dir> <output file> <source>')
  process.exit(64)
}

const files = []
const walk = (dir) => {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name)
    if (entry.isDirectory())
      walk(full)
    else if (full.endsWith('.trx'))
      files.push(full)
  }
}
walk(root)
if (files.length === 0) {
  console.error(`no .trx files under ${root}`)
  process.exit(1)
}

// trx 的时长是 hh:mm:ss.fffffff
const secondsOf = (text) => {
  const [hours, minutes, seconds] = text.split(':').map(Number)
  return hours * 3600 + minutes * 60 + seconds
}

const databases = {}
for (const file of files.sort()) {
  const db = file.match(/backend-test-results-([a-z]+)-\d+/)?.[1]
  if (!db) {
    console.error(`cannot tell the database from ${file}`)
    process.exit(1)
  }

  const xml = fs.readFileSync(file, 'utf8')
  const classOf = new Map()
  for (const match of xml.matchAll(/<UnitTest [^>]*id="([^"]+)"[\s\S]*?<TestMethod [^>]*className="([^"]+)"/g))
    classOf.set(match[1], match[2])

  const target = (databases[db] ??= { seconds: 0, tests: 0, classes: {} })
  for (const match of xml.matchAll(/<UnitTestResult ([^>]*)>/g)) {
    const attributes = Object.fromEntries([...match[1].matchAll(/(\w+)="([^"]*)"/g)].map(a => [a[1], a[2]]))
    const type = classOf.get(attributes.testId)
    if (!type) {
      console.error(`test ${attributes.testId} in ${file} has no class`)
      process.exit(1)
    }

    target.classes[type] ??= 0
    if (attributes.outcome === 'NotExecuted' || !attributes.duration)
      continue
    const seconds = secondsOf(attributes.duration)
    target.classes[type] += seconds
    target.seconds += seconds
    target.tests++
  }
}

const result = { source, databases: {} }
for (const db of Object.keys(databases).sort()) {
  const { seconds, tests, classes } = databases[db]
  result.databases[db] = {
    perTest: tests === 0 ? 0 : Math.round(seconds / tests * 10) / 10,
    classes: Object.fromEntries(Object.keys(classes).sort().map(type => [type, Math.round(classes[type])])),
  }
  console.error(`${db}: ${Object.keys(classes).length} classes, ${tests} tests, ${Math.round(seconds)} s`)
}

fs.writeFileSync(output, `${JSON.stringify(result, null, 2)}\n`)
