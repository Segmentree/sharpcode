<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { exampleRef, findTopic, useDocs } from '../docs/useDocs'
import type { DocExample } from '../docs/types'
import CodeEditor from '../components/CodeEditor.vue'
import MarkdownView from '../components/MarkdownView.vue'

const props = defineProps<{ slug: string }>()
const { t } = useI18n()
const router = useRouter()
const { topics, localizedTopics, pick } = useDocs()

const topic = computed(() => findTopic(props.slug))
const index = computed(() => topics.findIndex((x) => x.slug === props.slug))
const prev = computed(() => (index.value > 0 ? topics[index.value - 1] : undefined))
const next = computed(() => (index.value >= 0 && index.value < topics.length - 1 ? topics[index.value + 1] : undefined))

function openInSandbox(example: DocExample) {
  if (!topic.value) return
  router.push({ path: '/sandbox', query: { example: exampleRef(topic.value, example) } })
}
</script>

<template>
  <div class="page">
    <aside class="side">
      <RouterLink to="/docs" class="muted small back">← {{ t('docs.backToDocs') }}</RouterLink>
      <nav class="toc">
        <RouterLink
          v-for="item in localizedTopics"
          :key="item.topic.slug"
          :to="`/docs/${item.topic.slug}`"
          :class="{ current: item.topic.slug === slug }"
        >
          {{ item.title }}
        </RouterLink>
      </nav>
    </aside>

    <article v-if="topic" class="main">
      <RouterLink to="/docs" class="muted small back mobile-only">← {{ t('docs.backToDocs') }}</RouterLink>
      <h1>{{ pick(topic.title) }}</h1>
      <p class="muted lead">{{ pick(topic.summary) }}</p>
      <MarkdownView :source="pick(topic.body)" />

      <h2 class="examples-title">{{ t('docs.examples') }}</h2>
      <div v-for="ex in topic.examples" :key="ex.id" :id="ex.id" class="example panel">
        <div class="example-head">
          <h3>{{ pick(ex.title) }}</h3>
          <span class="spacer"></span>
          <button type="button" class="sm primary" @click="openInSandbox(ex)">{{ t('docs.openInSandbox') }}</button>
        </div>
        <div class="code"><CodeEditor :model-value="ex.code" readonly auto-height /></div>
        <div v-if="ex.stdin !== undefined" class="stdin">
          <label>{{ t('docs.stdin') }}</label>
          <pre>{{ ex.stdin || t('docs.emptyStdin') }}</pre>
        </div>
        <MarkdownView v-if="ex.note" :source="pick(ex.note)" class="note small" />
      </div>

      <div class="pager">
        <RouterLink v-if="prev" :to="`/docs/${prev.slug}`" class="btn">← {{ pick(prev.title) }}</RouterLink>
        <span class="spacer"></span>
        <RouterLink v-if="next" :to="`/docs/${next.slug}`" class="btn">{{ pick(next.title) }} →</RouterLink>
      </div>
    </article>

    <div v-else class="main">
      <p class="error">{{ t('docs.notFound') }}</p>
      <RouterLink to="/docs">← {{ t('docs.backToDocs') }}</RouterLink>
    </div>
  </div>
</template>

<style scoped>
.page { max-width: 1100px; margin: 0 auto; padding: 1.5rem 1rem; display: grid; grid-template-columns: 220px minmax(0, 1fr); gap: 1.5rem; }
.side { position: sticky; top: 1rem; align-self: start; display: flex; flex-direction: column; gap: 0.75rem; }
.back { display: inline-block; }
.toc { display: flex; flex-direction: column; gap: 0.15rem; }
.toc a { color: var(--muted); padding: 0.3rem 0.6rem; border-radius: 6px; font-size: 0.9rem; }
.toc a:hover { background: var(--panel-2); text-decoration: none; }
.toc a.current { color: var(--text); background: var(--panel-2); }
.main { min-width: 0; }
.lead { margin: 0 0 1rem; }
.examples-title { margin-top: 1.75rem; font-size: 1.1rem; }
.example { padding: 0.9rem 1rem; margin-bottom: 0.9rem; display: flex; flex-direction: column; gap: 0.6rem; }
.example-head { display: flex; align-items: center; gap: 0.5rem; }
.example-head h3 { margin: 0; font-size: 0.98rem; }
.code { border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.stdin label { margin-bottom: 0.15rem; }
.stdin pre { margin: 0; white-space: pre-wrap; }
.note { color: var(--muted); }
.note :deep(p:last-child) { margin-bottom: 0; }
.small { font-size: 0.82rem; }
.pager { display: flex; gap: 0.5rem; margin-top: 1.5rem; flex-wrap: wrap; }
.mobile-only { display: none; }

@media (max-width: 760px) {
  .page { grid-template-columns: 1fr; }
  .side { display: none; }
  .mobile-only { display: inline-block; margin-bottom: 0.5rem; }
}
</style>
