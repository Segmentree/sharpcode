<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useDocs } from '../docs/useDocs'

const { t } = useI18n()
const { localizedTopics } = useDocs()
</script>

<template>
  <div class="page">
    <h1>{{ t('docs.title') }}</h1>
    <p class="muted intro">{{ t('docs.intro') }}</p>

    <div class="grid">
      <RouterLink v-for="item in localizedTopics" :key="item.topic.slug" :to="`/docs/${item.topic.slug}`" class="card panel">
        <h3>{{ item.title }}</h3>
        <p class="muted">{{ item.summary }}</p>
        <span class="muted small">{{ t('docs.exampleCount', { n: item.topic.examples.length }, item.topic.examples.length) }}</span>
      </RouterLink>
    </div>
  </div>
</template>

<style scoped>
.page { max-width: 1000px; margin: 0 auto; padding: 1.5rem 1rem; }
.intro { margin: 0 0 1.25rem; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 0.75rem; }
.card { display: flex; flex-direction: column; gap: 0.35rem; padding: 1rem 1.1rem; color: var(--text); transition: border-color 0.15s, background 0.15s; }
.card:hover { text-decoration: none; border-color: var(--accent); background: var(--panel-2); }
.card h3 { margin: 0; font-size: 1rem; }
.card p { margin: 0; flex: 1; }
.small { font-size: 0.82rem; }
</style>
