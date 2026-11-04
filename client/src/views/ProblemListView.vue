<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type Category, type ProblemSummary } from '../api'
import { locale } from '../locale'
import { isAdmin } from '../admin'
import DifficultyBadge from '../components/DifficultyBadge.vue'

const { t } = useI18n()
const categories: Category[] = ['School', 'Gym']

const problems = ref<ProblemSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const category = ref<Category>('School')

const byCategory = computed(() =>
  Object.fromEntries(categories.map((c) => [c, problems.value.filter((p) => p.category === c)])) as Record<Category, ProblemSummary[]>,
)
const visible = computed(() => byCategory.value[category.value] ?? [])

async function load() {
  loading.value = true
  error.value = null
  try {
    problems.value = await api.listProblems()
  } catch (e) {
    error.value = (e as Error).message
  } finally {
    loading.value = false
  }
}

async function remove(p: ProblemSummary) {
  if (!confirm(t('list.confirmDelete', { title: p.title }))) return
  try {
    await api.deleteProblem(p.id)
    await load()
  } catch (e) {
    error.value = (e as Error).message
  }
}

onMounted(load)
watch(locale, load)
</script>

<template>
  <div class="page">
    <div class="row head">
      <h1>{{ t('list.title') }}</h1>
      <span class="spacer"></span>
      <RouterLink v-if="isAdmin" class="btn primary" to="/problems/new">{{ t('list.newProblem') }}</RouterLink>
    </div>

    <div class="tabs" role="tablist">
      <button
        v-for="c in categories"
        :key="c"
        type="button"
        role="tab"
        :aria-selected="category === c"
        :class="{ active: category === c }"
        @click="category = c"
      >
        {{ t(`category.${c}`) }} <span class="count">{{ byCategory[c]?.length ?? 0 }}</span>
      </button>
    </div>

    <p v-if="loading" class="muted">{{ t('app.loading') }}</p>
    <p v-else-if="error" class="error">{{ t('list.couldNotLoad', { error }) }}</p>
    <p v-else-if="visible.length === 0" class="muted">{{ t('list.empty') }}</p>

    <table v-else class="panel list">
      <thead>
        <tr>
          <th style="width: 3rem"></th>
          <th>{{ t('list.colTitle') }}</th>
          <th>{{ t('list.colDifficulty') }}</th>
          <th>{{ t('list.colTests') }}</th>
          <th v-if="isAdmin" style="width: 9rem"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in visible" :key="p.id">
          <td class="status"><span v-if="p.solved" :title="t('list.solved')" class="solved">✓</span></td>
          <td><RouterLink :to="`/problems/${p.slug}`">{{ p.title }}</RouterLink></td>
          <td><DifficultyBadge :difficulty="p.difficulty" /></td>
          <td class="muted">{{ p.testCaseCount }}</td>
          <td v-if="isAdmin" class="actions">
            <RouterLink class="btn sm" :to="`/problems/${p.id}/edit`">{{ t('list.edit') }}</RouterLink>
            <button type="button" class="sm danger" @click="remove(p)">{{ t('list.delete') }}</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.page { max-width: 960px; margin: 0 auto; padding: 1.5rem 1rem; }
.head { margin-bottom: 0.75rem; }
.tabs { display: flex; gap: 0.25rem; border-bottom: 1px solid var(--border); margin-bottom: 1rem; }
.tabs button { background: transparent; border: 0; border-bottom: 2px solid transparent; border-radius: 0; padding: 0.55rem 0.9rem; color: var(--muted); }
.tabs button.active { color: var(--text); border-bottom-color: var(--accent); }
.count { display: inline-block; min-width: 1.4rem; padding: 0 0.4rem; margin-left: 0.3rem; border-radius: 999px; background: var(--panel-2); font-size: 0.75rem; text-align: center; }
.list { width: 100%; border-collapse: separate; border-spacing: 0; overflow: hidden; }
th { text-align: left; font-size: 0.8rem; color: var(--muted); font-weight: 600; padding: 0.6rem 0.9rem; border-bottom: 1px solid var(--border); }
td { padding: 0.6rem 0.9rem; border-bottom: 1px solid var(--border); }
tr:last-child td { border-bottom: 0; }
tbody tr:hover { background: var(--panel-2); }
.status { text-align: center; }
.solved { color: var(--green); font-weight: 700; }
.actions { display: flex; gap: 0.4rem; justify-content: flex-end; }
</style>
