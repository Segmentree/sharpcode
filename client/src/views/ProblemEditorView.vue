<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, type Difficulty, type ProblemInput, type TestCaseInput } from '../api'
import CodeEditor from '../components/CodeEditor.vue'
import MarkdownView from '../components/MarkdownView.vue'

const props = defineProps<{ id?: string }>()
const router = useRouter()

const isEdit = computed(() => props.id !== undefined)
const loading = ref(isEdit.value)
const saving = ref(false)
const error = ref<string | null>(null)
const preview = ref(false)

const form = reactive<ProblemInput>({
  title: '',
  slug: '',
  description: '',
  difficulty: 'Easy',
  starterCode: 'public class Solution\n{\n    public int Solve(int n)\n    {\n        return 0;\n    }\n}\n',
  methodName: 'Solve',
  className: 'Solution',
  timeLimitMs: 2000,
  testCases: [{ input: '[]', expected: 'null', isHidden: false }],
})

const difficulties: Difficulty[] = ['Easy', 'Medium', 'Hard']

function validJson(s: string): boolean {
  try {
    JSON.parse(s)
    return true
  } catch {
    return false
  }
}
function validInput(s: string): boolean {
  try {
    return Array.isArray(JSON.parse(s))
  } catch {
    return false
  }
}

function addCase() {
  form.testCases.push({ input: '[]', expected: 'null', isHidden: false })
}
function removeCase(i: number) {
  form.testCases.splice(i, 1)
}
function move(i: number, dir: -1 | 1) {
  const j = i + dir
  if (j < 0 || j >= form.testCases.length) return
  const [item] = form.testCases.splice(i, 1)
  form.testCases.splice(j, 0, item as TestCaseInput)
}

async function save() {
  error.value = null
  saving.value = true
  try {
    const payload: ProblemInput = { ...form, slug: form.slug?.trim() || undefined }
    const saved = isEdit.value ? await api.updateProblem(Number(props.id), payload) : await api.createProblem(payload)
    router.push(`/problems/${saved.slug}`)
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  if (!isEdit.value) return
  try {
    const p = await api.getProblemFull(Number(props.id))
    Object.assign(form, {
      title: p.title,
      slug: p.slug,
      description: p.description,
      difficulty: p.difficulty,
      starterCode: p.starterCode,
      methodName: p.methodName,
      className: p.className,
      timeLimitMs: p.timeLimitMs,
      testCases: p.testCases.map((t) => ({ input: t.input, expected: t.expected, isHidden: t.isHidden })),
    })
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="page">
    <div class="row head">
      <h1>{{ isEdit ? 'Edit problem' : 'New problem' }}</h1>
      <span class="spacer"></span>
      <RouterLink class="btn" to="/">Cancel</RouterLink>
      <button class="primary" :disabled="saving || loading" @click="save">{{ saving ? 'Saving…' : 'Save' }}</button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="loading" class="muted">Loading…</p>

    <form v-else class="form" @submit.prevent="save">
      <section class="panel block">
        <h3>Statement</h3>
        <div class="grid-3">
          <div><label>Title</label><input v-model="form.title" required placeholder="Two Sum" /></div>
          <div><label>Slug (optional)</label><input v-model="form.slug" placeholder="two-sum" /></div>
          <div>
            <label>Difficulty</label>
            <select v-model="form.difficulty">
              <option v-for="d in difficulties" :key="d" :value="d">{{ d }}</option>
            </select>
          </div>
        </div>
        <div class="row desc-head">
          <label>Description (Markdown)</label>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="preview = !preview">{{ preview ? 'Edit' : 'Preview' }}</button>
        </div>
        <div v-if="preview" class="panel preview"><MarkdownView :source="form.description" /></div>
        <textarea v-else v-model="form.description" rows="10" required placeholder="Describe the task, constraints and an example…"></textarea>
      </section>

      <section class="panel block">
        <h3>Judge contract</h3>
        <div class="grid-3">
          <div>
            <label>Class name</label>
            <input v-model="form.className" placeholder="Solution" />
          </div>
          <div>
            <label>Method name</label>
            <input v-model="form.methodName" required placeholder="TwoSum" />
          </div>
          <div>
            <label>Time limit per case (ms)</label>
            <input v-model.number="form.timeLimitMs" type="number" min="100" max="30000" step="100" />
          </div>
        </div>
        <label>Starter code</label>
        <div class="editor"><CodeEditor v-model="form.starterCode" /></div>
        <p class="muted small">
          The user's class must be named <code>{{ form.className || 'Solution' }}</code> and expose a public method
          <code>{{ form.methodName || '…' }}</code>. Its parameters are filled from each test case's input array (JSON),
          and its return value is serialized to JSON and compared with the expected output.
        </p>
      </section>

      <section class="panel block">
        <div class="row">
          <h3>Test cases</h3>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="addCase">+ Add test case</button>
        </div>
        <p class="muted small">
          Input is a JSON array of arguments, e.g. <code>[[2,7,11,15], 9]</code>. Expected is the JSON return value,
          e.g. <code>[0,1]</code>, <code>"olleh"</code> or <code>42</code>. Hidden cases only run on submit and their
          details are not shown to the user.
        </p>
        <div v-for="(t, i) in form.testCases" :key="i" class="case">
          <div class="case-head">
            <strong>Case {{ i + 1 }}</strong>
            <label class="inline"><input type="checkbox" v-model="t.isHidden" /> Hidden</label>
            <span class="spacer"></span>
            <button type="button" class="sm" :disabled="i === 0" @click="move(i, -1)">↑</button>
            <button type="button" class="sm" :disabled="i === form.testCases.length - 1" @click="move(i, 1)">↓</button>
            <button type="button" class="sm danger" @click="removeCase(i)">Remove</button>
          </div>
          <div class="grid-2">
            <div>
              <label>Input (JSON array) <span v-if="!validInput(t.input)" class="error">— must be a JSON array</span></label>
              <textarea v-model="t.input" rows="2" :class="{ invalid: !validInput(t.input) }"></textarea>
            </div>
            <div>
              <label>Expected (JSON) <span v-if="!validJson(t.expected)" class="error">— invalid JSON</span></label>
              <textarea v-model="t.expected" rows="2" :class="{ invalid: !validJson(t.expected) }"></textarea>
            </div>
          </div>
        </div>
      </section>

      <div class="row">
        <span class="spacer"></span>
        <button class="primary" type="submit" :disabled="saving">{{ saving ? 'Saving…' : 'Save problem' }}</button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.page { max-width: 1000px; margin: 0 auto; padding: 1.5rem 1.25rem; }
.head { margin-bottom: 1rem; }
.form { display: flex; flex-direction: column; gap: 1rem; }
.block { padding: 1rem 1.1rem; display: flex; flex-direction: column; gap: 0.6rem; }
.block h3 { margin: 0; }
.grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.75rem; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.small { font-size: 0.82rem; }
.editor { height: 260px; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.preview { padding: 0.75rem 1rem; min-height: 8rem; }
.desc-head label { margin: 0; }
.case { border: 1px solid var(--border); border-radius: 8px; padding: 0.6rem 0.75rem; display: flex; flex-direction: column; gap: 0.5rem; }
.case-head { display: flex; align-items: center; gap: 0.6rem; }
label.inline { display: inline-flex; align-items: center; gap: 0.35rem; margin: 0; }
label.inline input { width: auto; }
textarea.invalid { border-color: var(--red); }

@media (max-width: 720px) {
  .grid-3, .grid-2 { grid-template-columns: 1fr; }
}
</style>
