<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { api, type Category, type Difficulty, type ProblemInput, type TestCaseInput } from '../api'
import CodeEditor from '../components/CodeEditor.vue'
import MarkdownView from '../components/MarkdownView.vue'
import { useDocs } from '../docs/useDocs'

const props = defineProps<{ id?: string }>()
const router = useRouter()
const { t } = useI18n()
const { localizedTopics } = useDocs()

const VISIBLE_REQUIRED = 5
const HIDDEN_REQUIRED = 20

const isEdit = computed(() => props.id !== undefined)
const loading = ref(isEdit.value)
const saving = ref(false)
const error = ref<string | null>(null)
const preview = ref(false)
const previewEs = ref(false)

function emptyCase(isHidden: boolean): TestCaseInput {
  return { input: '[]', expected: 'null', isHidden }
}
function defaultCases(): TestCaseInput[] {
  return [
    ...Array.from({ length: VISIBLE_REQUIRED }, () => emptyCase(false)),
    ...Array.from({ length: HIDDEN_REQUIRED }, () => emptyCase(true)),
  ]
}

const form = reactive<ProblemInput>({
  title: '',
  slug: '',
  description: '',
  difficulty: 'Easy',
  category: 'School',
  starterCode: 'public class Solution\n{\n    public int Solve(int n)\n    {\n        return 0;\n    }\n}\n',
  methodName: 'Solve',
  className: 'Solution',
  timeLimitMs: 2000,
  testCases: defaultCases(),
  docLinks: [],
})
const translation = reactive({ title: '', description: '' })

const difficulties: Difficulty[] = ['Easy', 'Medium', 'Hard']
const categories: Category[] = ['School', 'Gym']

const visibleCount = computed(() => form.testCases.filter((c) => !c.isHidden).length)
const hiddenCount = computed(() => form.testCases.filter((c) => c.isHidden).length)
const countsOk = computed(() => visibleCount.value === VISIBLE_REQUIRED && hiddenCount.value === HIDDEN_REQUIRED)

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
  // Fill the visible quota first, then hidden.
  form.testCases.push(emptyCase(visibleCount.value >= VISIBLE_REQUIRED))
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
    const hasTranslation = translation.title.trim() !== '' || translation.description.trim() !== ''
    const payload: ProblemInput = {
      ...form,
      slug: form.slug?.trim() || undefined,
      docLinks: form.docLinks && form.docLinks.length ? form.docLinks : undefined,
      translations: hasTranslation ? { es: { title: translation.title, description: translation.description } } : undefined,
    }
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
      category: p.category,
      starterCode: p.starterCode,
      methodName: p.methodName,
      className: p.className,
      timeLimitMs: p.timeLimitMs,
      testCases: p.testCases.map((tc) => ({ input: tc.input, expected: tc.expected, isHidden: tc.isHidden })),
      docLinks: [...(p.docLinks ?? [])],
    })
    translation.title = p.translations?.es?.title ?? ''
    translation.description = p.translations?.es?.description ?? ''
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
      <h1>{{ isEdit ? t('editor.editTitle') : t('editor.newTitle') }}</h1>
      <span class="spacer"></span>
      <RouterLink class="btn" to="/">{{ t('editor.cancel') }}</RouterLink>
      <button type="button" class="primary" :disabled="saving || loading" @click="save">
        {{ saving ? t('editor.saving') : t('editor.save') }}
      </button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="loading" class="muted">{{ t('app.loading') }}</p>

    <form v-else class="form" @submit.prevent="save">
      <section class="panel block">
        <h3>{{ t('editor.statement') }}</h3>
        <div class="grid-4">
          <div><label>{{ t('editor.title') }}</label><input v-model="form.title" required placeholder="Two Sum" /></div>
          <div><label>{{ t('editor.slug') }}</label><input v-model="form.slug" placeholder="two-sum" /></div>
          <div>
            <label>{{ t('editor.difficulty') }}</label>
            <select v-model="form.difficulty">
              <option v-for="d in difficulties" :key="d" :value="d">{{ t(`difficulty.${d}`) }}</option>
            </select>
          </div>
          <div>
            <label>{{ t('editor.category') }}</label>
            <select v-model="form.category">
              <option v-for="c in categories" :key="c" :value="c">{{ t(`category.${c}`) }}</option>
            </select>
          </div>
        </div>
        <div class="row desc-head">
          <label>{{ t('editor.description') }}</label>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="preview = !preview">{{ preview ? t('editor.edit') : t('editor.preview') }}</button>
        </div>
        <div v-if="preview" class="panel preview"><MarkdownView :source="form.description" /></div>
        <textarea v-else v-model="form.description" rows="10" required :placeholder="t('editor.descriptionPlaceholder')"></textarea>
      </section>

      <section class="panel block">
        <h3>{{ t('editor.translation') }}</h3>
        <p class="muted small">{{ t('editor.translationHint') }}</p>
        <div><label>{{ t('editor.translatedTitle') }}</label><input v-model="translation.title" /></div>
        <div class="row desc-head">
          <label>{{ t('editor.translatedDescription') }}</label>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="previewEs = !previewEs">{{ previewEs ? t('editor.edit') : t('editor.preview') }}</button>
        </div>
        <div v-if="previewEs" class="panel preview"><MarkdownView :source="translation.description" /></div>
        <textarea v-else v-model="translation.description" rows="8"></textarea>
      </section>

      <section class="panel block">
        <h3>{{ t('editor.contract') }}</h3>
        <div class="grid-3">
          <div><label>{{ t('editor.className') }}</label><input v-model="form.className" placeholder="Solution" /></div>
          <div><label>{{ t('editor.methodName') }}</label><input v-model="form.methodName" required placeholder="TwoSum" /></div>
          <div>
            <label>{{ t('editor.timeLimit') }}</label>
            <input v-model.number="form.timeLimitMs" type="number" min="100" max="30000" step="100" />
          </div>
        </div>
        <label>{{ t('editor.starterCode') }}</label>
        <div class="editor"><CodeEditor v-model="form.starterCode" /></div>
        <i18n-t keypath="editor.contractHint" tag="p" class="muted small">
          <template #cls><code>{{ form.className || 'Solution' }}</code></template>
          <template #method><code>{{ form.methodName || '…' }}</code></template>
        </i18n-t>
      </section>

      <section class="panel block">
        <h3>{{ t('editor.docLinks') }}</h3>
        <p class="muted small">{{ t('editor.docLinksHint') }}</p>
        <div class="doc-grid">
          <label v-for="item in localizedTopics" :key="item.topic.slug" class="inline">
            <input type="checkbox" v-model="form.docLinks" :value="item.topic.slug" />
            {{ item.title }} <span class="muted small">({{ item.topic.slug }})</span>
          </label>
        </div>
      </section>

      <section class="panel block">
        <div class="row">
          <h3>{{ t('editor.testCases') }}</h3>
          <span class="spacer"></span>
          <button type="button" class="sm" @click="addCase">{{ t('editor.addCase') }}</button>
        </div>
        <i18n-t keypath="editor.casesHint" tag="p" class="muted small">
          <template #inputExample><code>[[2,7,11,15], 9]</code></template>
          <template #ex1><code>[0,1]</code></template>
          <template #ex2><code>"olleh"</code></template>
          <template #ex3><code>42</code></template>
        </i18n-t>
        <div class="counts" :class="{ ok: countsOk, bad: !countsOk }">
          <strong>{{ t('editor.counts', { visible: visibleCount, visibleRequired: VISIBLE_REQUIRED, hidden: hiddenCount, hiddenRequired: HIDDEN_REQUIRED }) }}</strong>
          <span v-if="!countsOk" class="small">{{ t('editor.countsRequired', { visible: VISIBLE_REQUIRED, hidden: HIDDEN_REQUIRED }) }}</span>
        </div>
        <div v-for="(tc, i) in form.testCases" :key="i" class="case" :class="{ hiddenCase: tc.isHidden }">
          <div class="case-head">
            <strong>{{ t('editor.case', { n: i + 1 }) }}</strong>
            <label class="inline"><input type="checkbox" v-model="tc.isHidden" /> {{ t('editor.hidden') }}</label>
            <span class="spacer"></span>
            <button type="button" class="sm" :disabled="i === 0" :title="t('editor.moveUp')" @click="move(i, -1)">↑</button>
            <button type="button" class="sm" :disabled="i === form.testCases.length - 1" :title="t('editor.moveDown')" @click="move(i, 1)">↓</button>
            <button type="button" class="sm danger" @click="removeCase(i)">{{ t('editor.remove') }}</button>
          </div>
          <div class="grid-2">
            <div>
              <label>{{ t('editor.input') }} <span v-if="!validInput(tc.input)" class="error">{{ t('editor.inputInvalid') }}</span></label>
              <textarea v-model="tc.input" rows="2" :class="{ invalid: !validInput(tc.input) }"></textarea>
            </div>
            <div>
              <label>{{ t('editor.expected') }} <span v-if="!validJson(tc.expected)" class="error">{{ t('editor.expectedInvalid') }}</span></label>
              <textarea v-model="tc.expected" rows="2" :class="{ invalid: !validJson(tc.expected) }"></textarea>
            </div>
          </div>
        </div>
      </section>

      <div class="row">
        <span class="spacer"></span>
        <button class="primary" type="submit" :disabled="saving">{{ saving ? t('editor.saving') : t('editor.saveProblem') }}</button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.page { max-width: 1000px; margin: 0 auto; padding: 1.5rem 1rem; }
.head { margin-bottom: 1rem; }
.form { display: flex; flex-direction: column; gap: 1rem; }
.block { padding: 1rem 1.1rem; display: flex; flex-direction: column; gap: 0.6rem; }
.block h3 { margin: 0; }
.grid-4 { display: grid; grid-template-columns: 2fr 1.5fr 1fr 1fr; gap: 0.75rem; }
.grid-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0.75rem; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.doc-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 0.35rem 0.75rem; }
.small { font-size: 0.82rem; }
.editor { height: 260px; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.preview { padding: 0.75rem 1rem; min-height: 8rem; }
.desc-head label { margin: 0; }
.counts { display: flex; align-items: baseline; gap: 0.75rem; flex-wrap: wrap; padding: 0.5rem 0.75rem; border-radius: 8px; border: 1px solid var(--border); }
.counts.ok { color: var(--green); border-color: rgba(61, 220, 132, 0.4); background: rgba(61, 220, 132, 0.08); }
.counts.bad { color: var(--amber); border-color: rgba(255, 200, 87, 0.4); background: rgba(255, 200, 87, 0.08); }
.case { border: 1px solid var(--border); border-radius: 8px; padding: 0.6rem 0.75rem; display: flex; flex-direction: column; gap: 0.5rem; }
.case.hiddenCase { border-style: dashed; }
.case-head { display: flex; align-items: center; gap: 0.6rem; }
label.inline { display: inline-flex; align-items: center; gap: 0.35rem; margin: 0; }
label.inline input { width: auto; }
textarea.invalid { border-color: var(--red); }

@media (max-width: 720px) {
  .grid-4, .grid-3, .grid-2 { grid-template-columns: 1fr; }
}
</style>
