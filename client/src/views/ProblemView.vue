<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { api, verdictKey, type JudgeResult, type Problem, type Submission } from '../api'
import { isSignedIn } from '../auth'
import { locale } from '../locale'
import CodeEditor from '../components/CodeEditor.vue'
import MarkdownView from '../components/MarkdownView.vue'
import ResultsPanel from '../components/ResultsPanel.vue'
import SubmitProgress from '../components/SubmitProgress.vue'
import DifficultyBadge from '../components/DifficultyBadge.vue'
import { findTopic, pick } from '../docs/useDocs'

const props = defineProps<{ slug: string }>()
const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const problem = ref<Problem | null>(null)
const code = ref('')
const loading = ref(true)
const error = ref<string | null>(null)
const busy = ref<'run' | 'submit' | null>(null)
const result = ref<JudgeResult | null>(null)
const resultMode = ref<'run' | 'submit'>('run')
const submissions = ref<Submission[]>([])
const tab = ref<'description' | 'submissions'>('description')
/** The animated progress overlay shown while a submission is judged and its verdict is revealed. */
const progressOpen = ref(false)

// ---- Draft autosave (signed-in users only) ----
const DRAFT_DEBOUNCE_MS = 800
/** Code as last persisted on the server (draft or submission), so unchanged code is not re-saved. */
let savedCode = ''
let draftTimer: ReturnType<typeof setTimeout> | null = null
const draftState = ref<'idle' | 'saving' | 'saved' | 'error'>('idle')

async function flushDraft() {
  if (draftTimer) {
    clearTimeout(draftTimer)
    draftTimer = null
  }
  if (!problem.value || !isSignedIn.value || code.value === savedCode) return
  const snapshot = code.value
  draftState.value = 'saving'
  try {
    await api.saveDraft(problem.value.id, snapshot)
    savedCode = snapshot
    draftState.value = code.value === snapshot ? 'saved' : 'idle'
  } catch {
    draftState.value = 'error'
  }
}

watch(code, () => {
  if (!problem.value || !isSignedIn.value || code.value === savedCode) return
  draftState.value = 'idle'
  if (draftTimer) clearTimeout(draftTimer)
  draftTimer = setTimeout(flushDraft, DRAFT_DEBOUNCE_MS)
})

function goSignIn() {
  router.push({ name: 'login', query: { redirect: route.fullPath } })
}

const sampleCases = computed(() => problem.value?.testCases.filter((t) => !t.isHidden) ?? [])
const hiddenCount = computed(() => problem.value?.testCases.filter((t) => t.isHidden).length ?? 0)
const usings = 'System, System.Collections.Generic, System.Linq, System.Text'
const docTopics = computed(() =>
  (problem.value?.docLinks ?? []).map((slug) => findTopic(slug)).filter((x): x is NonNullable<typeof x> => !!x),
)

async function load() {
  loading.value = true
  error.value = null
  result.value = null
  try {
    const p = await api.getProblem(props.slug)
    if (isSignedIn.value) {
      const [draft, subs] = await Promise.all([api.getDraft(p.id), api.listSubmissions(p.id)])
      submissions.value = subs
      // The editor opens on the autosaved draft, else this problem's latest submission, else the starter code.
      savedCode = draft?.code ?? subs[0]?.code ?? p.starterCode
    } else {
      submissions.value = []
      savedCode = p.starterCode
    }
    problem.value = p
    code.value = savedCode
    draftState.value = 'idle'
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

/** Locale change: refresh only the localized text, keep code and results. */
async function reloadText() {
  if (!problem.value) return
  try {
    const fresh = await api.getProblem(props.slug)
    problem.value = { ...fresh }
  } catch (e) {
    error.value = (e as Error).message
  }
}

async function run() {
  if (!problem.value) return
  if (!isSignedIn.value) return goSignIn()
  busy.value = 'run'
  error.value = null
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
  if (!isSignedIn.value) return goSignIn()
  busy.value = 'submit'
  error.value = null
  result.value = null
  progressOpen.value = true
  try {
    await flushDraft()
    const s = await api.submit(problem.value.id, code.value)
    result.value = s.result ?? null
    resultMode.value = 'submit'
    submissions.value = [s, ...submissions.value]
    if (!s.result) progressOpen.value = false
  } catch (e) {
    error.value = (e as Error).message
    progressOpen.value = false
  } finally {
    busy.value = null
  }
}

async function reset() {
  if (!problem.value) return
  if (!confirm(t('problem.confirmReset'))) return
  if (draftTimer) clearTimeout(draftTimer)
  code.value = problem.value.starterCode
  if (isSignedIn.value) {
    savedCode = problem.value.starterCode
    draftState.value = 'idle'
    try {
      await api.deleteDraft(problem.value.id)
    } catch (e) {
      error.value = (e as Error).message
    }
  }
}

async function openSubmission(s: Submission) {
  try {
    const full = await api.getSubmission(s.id)
    code.value = full.code
    result.value = full.result ?? null
    resultMode.value = 'submit'
  } catch (e) {
    error.value = (e as Error).message
  }
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString(locale.value)
}

onMounted(load)
watch(() => props.slug, load)
watch(isSignedIn, load)
watch(locale, reloadText)
onBeforeUnmount(() => {
  void flushDraft()
})
</script>

<template>
  <SubmitProgress
    v-if="progressOpen && problem"
    :result="result"
    :total="problem.testCases.length"
    @close="progressOpen = false"
  />
  <div v-if="loading" class="centered muted">{{ t('app.loading') }}</div>
  <div v-else-if="!problem" class="centered error">{{ error ?? t('problem.notFound') }}</div>

  <div v-else class="workspace">
    <section class="pane left panel">
      <div class="tabs">
        <button type="button" :class="{ active: tab === 'description' }" @click="tab = 'description'">{{ t('problem.description') }}</button>
        <button type="button" :class="{ active: tab === 'submissions' }" @click="tab = 'submissions'">
          {{ t('problem.submissions') }} <span class="muted">({{ submissions.length }})</span>
        </button>
      </div>

      <div v-if="tab === 'description'" class="pane-body">
        <div class="row title">
          <h2>{{ problem.title }}</h2>
          <DifficultyBadge :difficulty="problem.difficulty" />
          <span class="badge cat">{{ t(`category.${problem.category}`) }}</span>
        </div>
        <MarkdownView :source="problem.description" />

        <h3 class="section-title">{{ t('problem.sampleCases') }}</h3>
        <div v-for="(tc, i) in sampleCases" :key="tc.id" class="sample">
          <div class="muted small">{{ t('problem.case', { n: i + 1 }) }}</div>
          <div class="sample-grid">
            <label>{{ t('problem.input') }}</label><pre>{{ tc.input }}</pre>
            <label>{{ t('problem.expected') }}</label><pre>{{ tc.expected }}</pre>
          </div>
        </div>
        <p v-if="hiddenCount" class="muted small">{{ t('problem.hiddenNote', { n: hiddenCount }, hiddenCount) }}</p>

        <template v-if="docTopics.length">
          <h3 class="section-title">{{ t('problem.usefulDocs') }}</h3>
          <ul class="doc-links">
            <li v-for="d in docTopics" :key="d.slug">
              <RouterLink :to="`/docs/${d.slug}`">{{ pick(d.title) }}</RouterLink>
              <span class="muted small"> — {{ pick(d.summary) }}</span>
            </li>
          </ul>
        </template>

        <h3 class="section-title">{{ t('problem.contract') }}</h3>
        <i18n-t keypath="problem.contractText" tag="p" class="muted small">
          <template #signature><code>{{ problem.className }}.{{ problem.methodName }}(…)</code></template>
          <template #ms>{{ problem.timeLimitMs }}</template>
          <template #usings><code>{{ usings }}</code></template>
        </i18n-t>
      </div>

      <div v-else class="pane-body">
        <p v-if="submissions.length === 0" class="muted">{{ t('problem.noSubmissions') }}</p>
        <table v-else class="subs">
          <thead>
            <tr><th>{{ t('problem.colVerdict') }}</th><th>{{ t('problem.colPassed') }}</th><th>{{ t('problem.colWhen') }}</th><th></th></tr>
          </thead>
          <tbody>
            <tr v-for="s in submissions" :key="s.id">
              <td><span class="verdict" :class="s.verdict">{{ t(verdictKey(s.verdict)) }}</span></td>
              <td class="muted">{{ s.passedCount }}/{{ s.totalCount }}</td>
              <td class="muted small">{{ fmtDate(s.createdAt) }}</td>
              <td><button type="button" class="sm" @click="openSubmission(s)">{{ t('problem.load') }}</button></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="pane right">
      <div class="editor panel">
        <div class="toolbar">
          <span class="muted small">{{ t('problem.language') }}</span>
          <span v-if="isSignedIn && draftState !== 'idle'" class="muted small draft" :class="draftState">{{ t(`problem.draft.${draftState}`) }}</span>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="reset">{{ t('problem.reset') }}</button>
          <button type="button" class="sm" :disabled="busy !== null" @click="run">
            {{ busy === 'run' ? t('problem.running') : t('problem.run') }}
          </button>
          <button type="button" class="sm primary" :disabled="busy !== null" @click="submit">
            {{ busy === 'submit' ? t('problem.judging') : t('problem.submit') }}
          </button>
        </div>
        <div class="editor-body">
          <CodeEditor v-model="code" :readonly="busy !== null" />
        </div>
      </div>

      <div class="output panel">
        <div class="toolbar"><span class="muted small">{{ t('problem.results') }}</span></div>
        <div class="output-body">
          <p v-if="error" class="error">{{ error }}</p>
          <ResultsPanel v-if="result" :result="result" :mode="resultMode" />
          <p v-else-if="!isSignedIn" class="muted small">
            {{ t('problem.signInHint') }}
            <a href="#" @click.prevent="goSignIn">{{ t('auth.signIn') }}</a>
          </p>
          <i18n-t v-else-if="!busy" keypath="problem.hint" tag="p" class="muted small">
            <template #run><b>{{ t('problem.run') }}</b></template>
            <template #submit><b>{{ t('problem.submit') }}</b></template>
          </i18n-t>
          <p v-else class="muted small">{{ t('problem.compiling') }}</p>
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
.title { margin-bottom: 0.75rem; flex-wrap: wrap; }
.title h2 { margin: 0; }
.badge.cat { color: var(--accent); background: rgba(124, 156, 255, 0.12); }
.section-title { margin-top: 1.5rem; font-size: 0.95rem; }
.doc-links { margin: 0; padding-left: 1.2rem; display: flex; flex-direction: column; gap: 0.3rem; }
.small { font-size: 0.82rem; }
.draft { margin-left: 0.5rem; }
.draft.saved { color: var(--green); }
.draft.error { color: var(--red); }
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
