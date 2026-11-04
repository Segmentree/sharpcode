<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, type ProblemSummary } from '../api'
import DifficultyBadge from '../components/DifficultyBadge.vue'

const problems = ref<ProblemSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

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
  if (!confirm(`Delete "${p.title}" and all its test cases and submissions?`)) return
  await api.deleteProblem(p.id)
  await load()
}

onMounted(load)
</script>

<template>
  <div class="page">
    <div class="row head">
      <h1>Problems</h1>
      <span class="spacer"></span>
      <RouterLink class="btn primary" to="/problems/new">+ New problem</RouterLink>
    </div>

    <p v-if="loading" class="muted">Loading…</p>
    <p v-else-if="error" class="error">Could not load problems: {{ error }}</p>
    <p v-else-if="problems.length === 0" class="muted">No problems yet. Create the first one.</p>

    <table v-else class="panel list">
      <thead>
        <tr>
          <th style="width: 3rem"></th>
          <th>Title</th>
          <th>Difficulty</th>
          <th>Tests</th>
          <th style="width: 9rem"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in problems" :key="p.id">
          <td class="status"><span v-if="p.solved" title="Solved" class="solved">✓</span></td>
          <td><RouterLink :to="`/problems/${p.slug}`">{{ p.title }}</RouterLink></td>
          <td><DifficultyBadge :difficulty="p.difficulty" /></td>
          <td class="muted">{{ p.testCaseCount }}</td>
          <td class="actions">
            <RouterLink class="btn sm" :to="`/problems/${p.id}/edit`">Edit</RouterLink>
            <button class="sm danger" @click="remove(p)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.page { max-width: 960px; margin: 0 auto; padding: 1.5rem 1.25rem; }
.head { margin-bottom: 1rem; }
.list { width: 100%; border-collapse: separate; border-spacing: 0; overflow: hidden; }
th { text-align: left; font-size: 0.8rem; color: var(--muted); font-weight: 600; padding: 0.6rem 0.9rem; border-bottom: 1px solid var(--border); }
td { padding: 0.6rem 0.9rem; border-bottom: 1px solid var(--border); }
tr:last-child td { border-bottom: 0; }
tbody tr:hover { background: var(--panel-2); }
.status { text-align: center; }
.solved { color: var(--green); font-weight: 700; }
.actions { display: flex; gap: 0.4rem; justify-content: flex-end; }
</style>
