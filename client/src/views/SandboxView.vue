<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { api, type SandboxResult } from '../api'
import CodeEditor from '../components/CodeEditor.vue'
import { findExample, pick } from '../docs/useDocs'
import type { DocExample, DocTopic } from '../docs/types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const CODE_KEY = 'sharpcode:sandbox:code'
const STDIN_KEY = 'sharpcode:sandbox:stdin'

const defaultCode = `// A small console program. Input comes from the "Standard input" box.
Console.Write("What is your name? ");
string? name = Console.ReadLine();

Console.Write("Pick a number: ");
int number = int.Parse(Console.ReadLine() ?? "0");

Console.WriteLine();
Console.WriteLine($"Hello, {name}! Twice your number is {number * 2}.");
`
const defaultStdin = 'Ada\n21\n'

function stored(key: string, fallback: string): string {
  try {
    return localStorage.getItem(key) ?? fallback
  } catch {
    return fallback
  }
}
function store(key: string, value: string) {
  try {
    localStorage.setItem(key, value)
  } catch {
    /* ignore */
  }
}

const code = ref(stored(CODE_KEY, defaultCode))
const stdin = ref(stored(STDIN_KEY, defaultStdin))
const busy = ref(false)
const error = ref<string | null>(null)
const result = ref<SandboxResult | null>(null)

watch(code, (v) => store(CODE_KEY, v))
watch(stdin, (v) => store(STDIN_KEY, v))

/** Example loaded from the docs via `?example=topic/id`; shown in a dismissible banner. */
const loaded = ref<{ topic: DocTopic; example: DocExample } | null>(null)

function loadExampleFromQuery() {
  const raw = route.query.example
  const ref_ = Array.isArray(raw) ? raw[0] : raw
  const found = findExample(typeof ref_ === 'string' ? ref_ : undefined)
  if (!found) return
  code.value = found.example.code
  stdin.value = found.example.stdin ?? ''
  result.value = null
  error.value = null
  loaded.value = found
  // Drop the query so a reload keeps the user's edits (the drafts are persisted in localStorage).
  const { example: _example, ...rest } = route.query
  void _example
  router.replace({ path: route.path, query: rest })
}

onMounted(loadExampleFromQuery)
watch(() => route.query.example, loadExampleFromQuery)

async function run() {
  busy.value = true
  error.value = null
  try {
    result.value = await api.sandboxRun(code.value, stdin.value)
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    busy.value = false
  }
}

function reset() {
  if (!confirm(t('sandbox.confirmReset'))) return
  code.value = defaultCode
  stdin.value = defaultStdin
  result.value = null
}

// Reuse the workspace's verdict colours for sandbox statuses.
const statusClass: Record<SandboxResult['status'], string> = {
  Ok: 'Accepted',
  CompileError: 'CompileError',
  RuntimeError: 'RuntimeError',
  TimeLimitExceeded: 'TimeLimitExceeded',
}
</script>

<template>
  <div class="sandbox">
    <section class="pane left">
      <div class="editor panel">
        <div class="toolbar">
          <span class="muted small">{{ t('sandbox.program') }}</span>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="reset">{{ t('sandbox.reset') }}</button>
          <button type="button" class="sm primary" :disabled="busy" @click="run">
            {{ busy ? t('sandbox.running') : t('sandbox.run') }}
          </button>
        </div>
        <div class="editor-body">
          <CodeEditor v-model="code" :readonly="busy" />
        </div>
      </div>

      <div class="stdin panel">
        <div class="toolbar"><span class="muted small">{{ t('sandbox.stdin') }}</span></div>
        <div class="stdin-body">
          <textarea v-model="stdin" rows="4" spellcheck="false" :disabled="busy"></textarea>
          <p class="muted small hint">{{ t('sandbox.stdinHint') }}</p>
        </div>
      </div>
    </section>

    <section class="pane right panel">
      <div class="toolbar">
        <span class="muted small">{{ t('sandbox.output') }}</span>
        <span class="spacer"></span>
        <template v-if="result">
          <span class="verdict" :class="statusClass[result.status]">{{ t(`sandbox.status.${result.status}`) }}</span>
          <span class="muted small meta">
            <template v-if="result.timedOut">{{ t('sandbox.timedOut') }}</template>
            <template v-else-if="result.exitCode !== null">{{ t('sandbox.exitCode', { code: result.exitCode }) }}</template>
            · {{ t('results.ms', { n: result.durationMs.toFixed(0) }) }}
          </span>
        </template>
      </div>
      <div class="output-body">
        <div v-if="loaded" class="banner">
          <i18n-t keypath="sandbox.loadedExample" tag="span">
            <template #title>{{ pick(loaded.example.title) }}</template>
            <template #topic><RouterLink :to="`/docs/${loaded.topic.slug}`">{{ pick(loaded.topic.title) }}</RouterLink></template>
          </i18n-t>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="loaded = null">{{ t('sandbox.dismiss') }}</button>
        </div>
        <p class="muted small intro">{{ t('sandbox.intro') }}</p>
        <p v-if="error" class="error">{{ error }}</p>

        <template v-if="result">
          <div v-if="result.diagnostics.length" class="diagnostics">
            <div v-for="(d, i) in result.diagnostics" :key="i" class="diag" :class="{ err: d.isError }">
              <span class="loc">{{ t('results.line', { line: d.line, col: d.column }) }}</span>
              <span class="code">{{ d.code }}</span>
              <span>{{ d.message }}</span>
            </div>
          </div>

          <div v-if="result.status !== 'CompileError'" class="field">
            <label>{{ t('sandbox.stdout') }}</label>
            <pre class="console">{{ result.stdout || t('sandbox.noOutput') }}</pre>
          </div>
          <div v-if="result.stderr" class="field">
            <label>{{ t('sandbox.stderr') }}</label>
            <pre class="console bad">{{ result.stderr }}</pre>
          </div>
        </template>

        <i18n-t v-else-if="!busy && !error" keypath="sandbox.hint" tag="p" class="muted small">
          <template #run><b>{{ t('sandbox.run') }}</b></template>
        </i18n-t>
        <p v-else-if="busy" class="muted small">{{ t('sandbox.running') }}</p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.sandbox {
  height: 100%;
  display: grid;
  grid-template-columns: minmax(360px, 3fr) minmax(320px, 2fr);
  gap: 0.75rem;
  padding: 0.75rem;
}
.pane { min-height: 0; display: flex; flex-direction: column; }
.left { display: grid; grid-template-rows: minmax(0, 1fr) auto; gap: 0.75rem; }
.right { overflow: hidden; }
.editor, .stdin { display: flex; flex-direction: column; min-height: 0; overflow: hidden; }
.toolbar { display: flex; align-items: center; gap: 0.5rem; padding: 0.45rem 0.75rem; border-bottom: 1px solid var(--border); flex-wrap: wrap; }
.editor-body { flex: 1; min-height: 0; }
.stdin-body { padding: 0.6rem 0.75rem; display: flex; flex-direction: column; gap: 0.35rem; }
.stdin-body textarea { font-family: var(--mono); }
.hint { margin: 0; }
.intro { margin: 0 0 0.75rem; }
.banner { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; padding: 0.5rem 0.75rem; border-radius: 8px; border: 1px solid rgba(124, 156, 255, 0.4); background: rgba(124, 156, 255, 0.1); font-size: 0.9rem; }
.output-body { flex: 1; min-height: 0; overflow: auto; padding: 0.75rem; display: flex; flex-direction: column; gap: 0.6rem; }
.small { font-size: 0.82rem; }
.meta { white-space: nowrap; }
.diagnostics { display: flex; flex-direction: column; gap: 0.3rem; }
.diag { display: flex; gap: 0.6rem; font-family: var(--mono); font-size: 0.85rem; padding: 0.4rem 0.6rem; border-radius: 6px; background: var(--panel-2); flex-wrap: wrap; }
.diag.err { color: var(--red); background: rgba(255, 107, 107, 0.08); }
.diag .loc { color: var(--muted); min-width: 6rem; }
.diag .code { color: var(--amber); }
.field label { margin-bottom: 0.15rem; }
.console { margin: 0; white-space: pre-wrap; word-break: break-word; min-height: 3rem; }
.console.bad { color: var(--red); border-color: rgba(255, 107, 107, 0.4); }

@media (max-width: 900px) {
  .sandbox { grid-template-columns: 1fr; height: auto; }
  .left { grid-template-rows: 380px auto; }
  .right { min-height: 240px; }
}
</style>
