<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { api, type SandboxResult, type SandboxScript } from '../api'
import CodeEditor from '../components/CodeEditor.vue'
import { findExample, pick } from '../docs/useDocs'
import type { DocExample, DocTopic } from '../docs/types'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const defaultCode = `// A small console program. Input comes from the "Standard input" box.
Console.Write("What is your name? ");
string? name = Console.ReadLine();

Console.Write("Pick a number: ");
int number = int.Parse(Console.ReadLine() ?? "0");

Console.WriteLine();
Console.WriteLine($"Hello, {name}! Twice your number is {number * 2}.");
`
const defaultStdin = 'Ada\n21\n'

const code = ref(defaultCode)
const stdin = ref(defaultStdin)
const busy = ref(false)
const loading = ref(true)
const error = ref<string | null>(null)
const result = ref<SandboxResult | null>(null)

// ---- Draft: whatever is in the editor, autosaved to the server for this user ----
const DRAFT_DEBOUNCE_MS = 800
let savedDraft = { code: '', stdin: '' }
let draftTimer: ReturnType<typeof setTimeout> | null = null
const draftState = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')

function draftChanged() {
  return code.value !== savedDraft.code || stdin.value !== savedDraft.stdin
}

async function flushDraft() {
  if (draftTimer) {
    clearTimeout(draftTimer)
    draftTimer = null
  }
  if (loading.value || !draftChanged()) return
  const snapshot = { code: code.value, stdin: stdin.value }
  draftState.value = 'saving'
  try {
    await api.saveSandboxDraft(snapshot.code, snapshot.stdin)
    savedDraft = snapshot
    draftState.value = draftChanged() ? 'idle' : 'saved'
  } catch {
    draftState.value = 'error'
  }
}

watch([code, stdin], () => {
  if (loading.value || !draftChanged()) return
  draftState.value = 'idle'
  if (draftTimer) clearTimeout(draftTimer)
  draftTimer = setTimeout(flushDraft, DRAFT_DEBOUNCE_MS)
})

// ---- Named scripts ----
const scripts = ref<SandboxScript[]>([])
/** The saved script the editor was loaded from, if any. Edits mark it dirty until "Save" is pressed. */
const currentId = ref<number | null>(null)
const current = computed(() => scripts.value.find((s) => s.id === currentId.value) ?? null)
const scriptDirty = computed(() => !!current.value && (current.value.code !== code.value || current.value.stdin !== stdin.value))
const scriptBusy = ref(false)

function applyScript(s: SandboxScript) {
  currentId.value = s.id
  code.value = s.code
  stdin.value = s.stdin
  result.value = null
  error.value = null
  loaded.value = null
}

function selectScript(e: Event) {
  const value = (e.target as HTMLSelectElement).value
  const s = scripts.value.find((x) => String(x.id) === value)
  if (!s) {
    currentId.value = null
    return
  }
  // Unsaved edits to another script or to a fresh program would be lost (the draft is overwritten).
  if ((current.value ? scriptDirty.value : code.value !== defaultCode) && !confirm(t('sandbox.scripts.confirmLoad', { name: s.name }))) {
    ;(e.target as HTMLSelectElement).value = currentId.value === null ? '' : String(currentId.value)
    return
  }
  applyScript(s)
}

async function withScriptBusy(fn: () => Promise<void>) {
  scriptBusy.value = true
  error.value = null
  try {
    await fn()
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    scriptBusy.value = false
  }
}

function saveScript() {
  const cur = current.value
  if (!cur) return saveScriptAs()
  return withScriptBusy(async () => {
    const updated = await api.updateScript(cur.id, cur.name, code.value, stdin.value)
    scripts.value = [updated, ...scripts.value.filter((s) => s.id !== cur.id)]
  })
}

function saveScriptAs() {
  const name = prompt(t('sandbox.scripts.namePrompt'), current.value?.name ?? '')?.trim()
  if (!name) return
  return withScriptBusy(async () => {
    const created = await api.createScript(name, code.value, stdin.value)
    scripts.value = [created, ...scripts.value]
    currentId.value = created.id
  })
}

function renameScript() {
  const cur = current.value
  if (!cur) return
  const name = prompt(t('sandbox.scripts.renamePrompt'), cur.name)?.trim()
  if (!name || name === cur.name) return
  return withScriptBusy(async () => {
    const updated = await api.updateScript(cur.id, name, cur.code, cur.stdin)
    scripts.value = scripts.value.map((s) => (s.id === cur.id ? updated : s))
  })
}

function deleteScript() {
  const cur = current.value
  if (!cur || !confirm(t('sandbox.scripts.confirmDelete', { name: cur.name }))) return
  return withScriptBusy(async () => {
    await api.deleteScript(cur.id)
    scripts.value = scripts.value.filter((s) => s.id !== cur.id)
    currentId.value = null
  })
}

function newScript() {
  if ((current.value ? scriptDirty.value : code.value !== defaultCode) && !confirm(t('sandbox.confirmReset'))) return
  currentId.value = null
  code.value = defaultCode
  stdin.value = defaultStdin
  result.value = null
  loaded.value = null
}

/** Example loaded from the docs via `?example=topic/id`; shown in a dismissible banner. */
const loaded = ref<{ topic: DocTopic; example: DocExample } | null>(null)

function loadExampleFromQuery(): boolean {
  const raw = route.query.example
  const ref_ = Array.isArray(raw) ? raw[0] : raw
  const found = findExample(typeof ref_ === 'string' ? ref_ : undefined)
  if (!found) return false
  currentId.value = null
  code.value = found.example.code
  stdin.value = found.example.stdin ?? ''
  result.value = null
  error.value = null
  loaded.value = found
  // Drop the query so a reload keeps the user's edits (the draft is autosaved to the server).
  const { example: _example, ...rest } = route.query
  void _example
  router.replace({ path: route.path, query: rest })
  return true
}

async function load() {
  loading.value = true
  try {
    const [draft, list] = await Promise.all([api.getSandboxDraft(), api.listScripts()])
    scripts.value = list
    if (draft) {
      code.value = draft.code
      stdin.value = draft.stdin
      savedDraft = { code: draft.code, stdin: draft.stdin }
    }
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
  // An example link wins over the stored draft (and is then autosaved as the new draft).
  loadExampleFromQuery()
}

onMounted(load)
watch(() => route.query.example, () => void loadExampleFromQuery())
onBeforeUnmount(() => {
  void flushDraft()
})

async function run() {
  busy.value = true
  error.value = null
  try {
    await flushDraft()
    result.value = await api.sandboxRun(code.value, stdin.value)
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    busy.value = false
  }
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
          <span v-if="draftState !== 'idle'" class="muted small draft" :class="draftState">{{ t(`sandbox.draft.${draftState}`) }}</span>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="newScript">{{ t('sandbox.scripts.new') }}</button>
          <button type="button" class="sm primary" :disabled="busy || loading" @click="run">
            {{ busy ? t('sandbox.running') : t('sandbox.run') }}
          </button>
        </div>
        <div class="toolbar scripts">
          <label class="muted small" for="script-select">{{ t('sandbox.scripts.label') }}</label>
          <select id="script-select" :value="currentId ?? ''" :disabled="scriptBusy || loading" @change="selectScript">
            <option value="">{{ t('sandbox.scripts.none') }}</option>
            <option v-for="s in scripts" :key="s.id" :value="String(s.id)">{{ s.name }}</option>
          </select>
          <span v-if="current && scriptDirty" class="muted small">{{ t('sandbox.scripts.modified') }}</span>
          <span class="spacer"></span>
          <button type="button" class="sm" :disabled="scriptBusy || loading || (!!current && !scriptDirty)" @click="saveScript">
            {{ t('sandbox.scripts.save') }}
          </button>
          <button type="button" class="sm" :disabled="scriptBusy || loading" @click="saveScriptAs">{{ t('sandbox.scripts.saveAs') }}</button>
          <button v-if="current" type="button" class="sm" :disabled="scriptBusy" @click="renameScript">{{ t('sandbox.scripts.rename') }}</button>
          <button v-if="current" type="button" class="sm danger" :disabled="scriptBusy" @click="deleteScript">{{ t('sandbox.scripts.delete') }}</button>
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
.scripts { padding-top: 0.35rem; padding-bottom: 0.35rem; }
.scripts select { max-width: 16rem; padding: 0.25rem 0.5rem; font-size: 0.85rem; }
.draft { margin-left: 0.5rem; }
.draft.saved { color: var(--green); }
.draft.error { color: var(--red); }
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
