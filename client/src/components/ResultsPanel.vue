<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { JudgeResult, TestCaseResult } from '../api'
import { verdictKey } from '../api'

defineProps<{ result: JudgeResult; mode: 'run' | 'submit' }>()
const { t } = useI18n()

const open = ref<Record<number, boolean>>({})
function toggle(r: TestCaseResult) {
  open.value[r.testCaseId] = !open.value[r.testCaseId]
}
function isOpen(r: TestCaseResult, index: number) {
  return open.value[r.testCaseId] ?? (!r.passed || index === 0)
}
</script>

<template>
  <div class="results">
    <div class="summary">
      <span class="verdict" :class="result.verdict">{{ t(verdictKey(result.verdict)) }}</span>
      <span class="muted">{{ result.message }}</span>
      <span class="spacer"></span>
      <span class="muted small">
        {{ mode === 'run' ? t('results.sampleCases') : t('results.allCases') }} ·
        {{ t('results.ms', { n: result.totalDurationMs.toFixed(0) }) }}
      </span>
    </div>

    <div v-if="result.diagnostics.length" class="diagnostics">
      <div v-for="(d, i) in result.diagnostics" :key="i" class="diag" :class="{ err: d.isError }">
        <span class="loc">{{ t('results.line', { line: d.line, col: d.column }) }}</span>
        <span class="code">{{ d.code }}</span>
        <span>{{ d.message }}</span>
      </div>
    </div>

    <div v-if="result.results.length" class="cases">
      <div
        v-for="(r, i) in result.results"
        :key="r.testCaseId"
        class="case"
        :class="{ passed: r.passed, failed: !r.passed }"
      >
        <button type="button" class="case-head" @click="toggle(r)">
          <span class="dot"></span>
          <span>{{ t('problem.case', { n: i + 1 }) }}<span v-if="r.isHidden" class="muted"> {{ t('results.hidden') }}</span></span>
          <span class="spacer"></span>
          <span class="muted small">{{ t('results.ms', { n: r.durationMs.toFixed(1) }) }}</span>
          <span class="chev">{{ isOpen(r, i) ? '▾' : '▸' }}</span>
        </button>
        <div v-if="isOpen(r, i)" class="case-body">
          <template v-if="r.input !== undefined">
            <div class="field"><label>{{ t('problem.input') }}</label><pre>{{ r.input }}</pre></div>
            <div class="field"><label>{{ t('problem.expected') }}</label><pre>{{ r.expected }}</pre></div>
            <div v-if="r.actual !== undefined" class="field">
              <label>{{ t('problem.output') }}</label><pre :class="{ bad: !r.passed }">{{ r.actual }}</pre>
            </div>
            <div v-if="r.stdout" class="field"><label>{{ t('problem.stdout') }}</label><pre>{{ r.stdout }}</pre></div>
          </template>
          <p v-else class="muted small">{{ t('results.hiddenDetails') }}</p>
          <div v-if="r.error" class="field"><label>{{ t('problem.error') }}</label><pre class="bad">{{ r.error }}</pre></div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.results { display: flex; flex-direction: column; gap: 0.75rem; }
.summary { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
.small { font-size: 0.82rem; }
.diagnostics { display: flex; flex-direction: column; gap: 0.3rem; }
.diag { display: flex; gap: 0.6rem; font-family: var(--mono); font-size: 0.85rem; padding: 0.4rem 0.6rem; border-radius: 6px; background: var(--panel-2); flex-wrap: wrap; }
.diag.err { color: var(--red); background: rgba(255, 107, 107, 0.08); }
.diag .loc { color: var(--muted); min-width: 6rem; }
.diag .code { color: var(--amber); }
.cases { display: flex; flex-direction: column; gap: 0.4rem; }
.case { border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.case-head { width: 100%; display: flex; align-items: center; gap: 0.6rem; background: transparent; border: 0; border-radius: 0; padding: 0.5rem 0.75rem; text-align: left; }
.case-head:hover { background: var(--panel-2); }
.dot { width: 9px; height: 9px; border-radius: 50%; background: var(--red); }
.passed .dot { background: var(--green); }
.chev { color: var(--muted); font-size: 0.8rem; }
.case-body { padding: 0.25rem 0.75rem 0.75rem; display: flex; flex-direction: column; gap: 0.5rem; border-top: 1px solid var(--border); }
.field pre { margin: 0; white-space: pre-wrap; word-break: break-all; }
.field label { margin-bottom: 0.15rem; }
pre.bad { color: var(--red); border-color: rgba(255, 107, 107, 0.4); }
</style>
