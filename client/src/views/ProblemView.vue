<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { api, verdictLabel, type JudgeResult, type Problem, type Submission } from '../api'
import CodeEditor from '../components/CodeEditor.vue'
import MarkdownView from '../components/MarkdownView.vue'
import ResultsPanel from '../components/ResultsPanel.vue'
import DifficultyBadge from '../components/DifficultyBadge.vue'

const props = defineProps<{ slug: string }>()

const problem = ref<Problem | null>(null)
const code = ref('')
const loading = ref(true)
const error = ref<string | null>(null)
const busy = ref<'run' | 'submit' | null>(null)
const result = ref<JudgeResult | null>(null)
const resultMode = ref<'run' | 'submit'>('run')
const submissions = ref<Submission[]>([])
const tab = ref<'description' | 'submissions'>('description')

const draftKey = computed(() => (problem.value ? `sharpcode:draft:${problem.value.id}` : ''))
const sampleCases = computed(() => problem.value?.testCases.filter((t) => !t.isHidden) ?? [])
const hiddenCount = computed(() => problem.value?.testCases.filter((t) => t.isHidden).length ?? 0)

async function load() {
  loading.value = true
  error.value = null
  result.value = null
  try {
    problem.value = await api.getProblem(props.slug)
    let draft: string | null = null
    try {
      draft = localStorage.getItem(draftKey.value)
    } catch {
      /* storage unavailable */
    }
    code.value = draft ?? problem.value.starterCode
    submissions.value = await api.listSubmissions(problem.value.id)
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

watch(code, (value) => {
  if (!draftKey.value) return
  try {
    localStorage.setItem(draftKey.value, value)
  } catch {
    /* ignore */
  }
})

async function run() {
  if (!problem.value) return
  busy.value = 'run'
  try {
    result.value = await api.run(problem.value.id, code.value)
    resultMode.value = 'run'
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    busy.value = null
  }
}

async function submit() {
  if (!problem.value) return
  busy.value = 'submit'
  try {
    const s = await api.submit(problem.value.id, code.value)
    result.value = s.result ?? null
    resultMode.value = 'submit'
    submissions.value = [s, ...submissions.value]
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    busy.value = null
  }
}

function reset() {
  if (!problem.value) return
  if (!confirm('Replace your code with the starter code?')) return
  code.value = problem.value.starterCode
}

async function openSubmission(s: Submission) {
  const full = await api.getSubmission(s.id)
  code.value = full.code
  result.value = full.result ?? null
  resultMode.value = 'submit'
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString()
}

onMounted(load)
watch(() => props.slug, load)
</script>

<template>
  <div v-if="loading" class="centered muted">Loading…</div>
  <div v-else-if="!problem" class="centered error">{{ error ?? 'Problem not found.' }}</div>

  <div v-else class="workspace">
    <section class="pane left panel">
      <div class="tabs">
        <button :class="{ active: tab === 'description' }" @click="tab = 'description'">Description</button>
        <button :class="{ active: tab === 'submissions' }" @click="tab = 'submissions'">
          Submissions <span class="muted">({{ submissions.length }})</span>
        </button>
      </div>

      <div v-if="tab === 'description'" class="pane-body">
        <div class="row title">
          <h2>{{ problem.title }}</h2>
          <DifficultyBadge :difficulty="problem.difficulty" />
        </div>
        <MarkdownView :source="problem.description" />

        <h3 class="section-title">Sample test cases</h3>
        <div v-for="(t, i) in sampleCases" :key="t.id" class="sample">
          <div class="muted small">Case {{ i + 1 }}</div>
          <div class="sample-grid">
            <label>Input</label><pre>{{ t.input }}</pre>
            <label>Expected</label><pre>{{ t.expected }}</pre>
          </div>
        </div>
        <p v-if="hiddenCount" class="muted small">
          Plus {{ hiddenCount }} hidden test case{{ hiddenCount === 1 ? '' : 's' }} evaluated on submit.
        </p>

        <h3 class="section-title">Contract</h3>
        <p class="muted small">
          The judge calls <code>{{ problem.className }}.{{ problem.methodName }}(…)</code> with each test case's
          arguments and compares the returned value to the expected output (JSON equality). Time limit:
          {{ problem.timeLimitMs }} ms per case. <code>System</code>, <code>System.Collections.Generic</code>,
          <code>System.Linq</code> and <code>System.Text</code> are imported for you.
        </p>
      </div>

      <div v-else class="pane-body">
        <p v-if="submissions.length === 0" class="muted">No submissions yet.</p>
        <table v-else class="subs">
          <thead>
            <tr><th>Verdict</th><th>Passed</th><th>When</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="s in submissions" :key="s.id">
              <td><span class="verdict" :class="s.verdict">{{ verdictLabel[s.verdict] }}</span></td>
              <td class="muted">{{ s.passedCount }}/{{ s.totalCount }}</td>
              <td class="muted small">{{ fmtDate(s.createdAt) }}</td>
              <td><button class="sm" @click="openSubmission(s)">Load</button></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="pane right">
      <div class="editor panel">
        <div class="toolbar">
          <span class="muted small">C#</span>
          <span class="spacer"></span>
          <button class="sm" @click="reset">Reset</button>
          <button class="sm" :disabled="busy !== null" @click="run">
            {{ busy === 'run' ? 'Running…' : 'Run' }}
          </button>
          <button class="sm primary" :disabled="busy !== null" @click="submit">
            {{ busy === 'submit' ? 'Judging…' : 'Submit' }}
          </button>
        </div>
        <div class="editor-body">
          <CodeEditor v-model="code" :readonly="busy !== null" />
        </div>
      </div>

      <div class="output panel">
        <div class="toolbar"><span class="muted small">Results</span></div>
        <div class="output-body">
          <p v-if="error" class="error">{{ error }}</p>
          <ResultsPanel v-if="result" :result="result" :mode="resultMode" />
          <p v-else-if="!busy" class="muted small">
            Press <b>Run</b> to check the sample cases, or <b>Submit</b> to judge against every test case.
          </p>
          <p v-else class="muted small">Compiling and running…</p>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.centered { padding: 3rem; text-align: center; }
.workspace {
  height: 100%;
  display: grid;
  grid-template-columns: minmax(320px, 2fr) minmax(420px, 3fr);
  gap: 0.75rem;
  padding: 0.75rem;
}
.pane { min-height: 0; display: flex; flex-direction: column; }
.left { overflow: hidden; }
.right { display: grid; grid-template-rows: minmax(0, 3fr) minmax(0, 2fr); gap: 0.75rem; }
.pane-body { padding: 1rem 1.1rem; overflow: auto; flex: 1; }
.tabs { display: flex; border-bottom: 1px solid var(--border); padding: 0 0.5rem; }
.tabs button { background: transparent; border: 0; border-bottom: 2px solid transparent; border-radius: 0; padding: 0.6rem 0.75rem; color: var(--muted); }
.tabs button.active { color: var(--text); border-bottom-color: var(--accent); }
.title { margin-bottom: 0.75rem; }
.title h2 { margin: 0; }
.section-title { margin-top: 1.5rem; font-size: 0.95rem; }
.small { font-size: 0.82rem; }
.sample { margin-bottom: 0.75rem; }
.sample-grid { display: grid; grid-template-columns: auto 1fr; gap: 0.25rem 0.75rem; align-items: start; }
.sample-grid label { margin: 0.35rem 0 0; }
.sample-grid pre { margin: 0; white-space: pre-wrap; word-break: break-all; }
.editor, .output { display: flex; flex-direction: column; min-height: 0; overflow: hidden; }
.toolbar { display: flex; align-items: center; gap: 0.4rem; padding: 0.45rem 0.75rem; border-bottom: 1px solid var(--border); }
.editor-body { flex: 1; min-height: 0; }
.output-body { flex: 1; min-height: 0; overflow: auto; padding: 0.75rem; }
.subs { width: 100%; border-collapse: collapse; }
.subs th { text-align: left; font-size: 0.8rem; color: var(--muted); padding: 0.35rem 0.5rem; border-bottom: 1px solid var(--border); }
.subs td { padding: 0.4rem 0.5rem; border-bottom: 1px solid var(--border); }

@media (max-width: 900px) {
  .workspace { grid-template-columns: 1fr; height: auto; }
  .right { grid-template-rows: 420px auto; }
  .left { max-height: 60vh; }
}
</style>
