import { computed } from 'vue'
import { locale } from '../locale'
import type { DocExample, DocTopic, Localized } from './types'
import { topicBySlug, topics } from './topics'

/** Text for the current UI locale, falling back to English. */
export function pick(l: Localized): string {
  const v = l[locale.value]
  return v && v.length > 0 ? v : l.en
}

/** Computed-friendly accessor: re-evaluates when the locale changes. */
export function useDocs() {
  const localizedTopics = computed(() =>
    topics.map((t) => ({ topic: t, title: pick(t.title), summary: pick(t.summary) })),
  )
  return { topics, topicBySlug, localizedTopics, pick }
}

export function findTopic(slug: string | undefined | null): DocTopic | undefined {
  return slug ? topicBySlug[slug] : undefined
}

/** Resolves "topic/exampleId" (the Sandbox `example` query) into its topic and example. */
export function findExample(ref: string | undefined | null): { topic: DocTopic; example: DocExample } | undefined {
  if (!ref) return undefined
  const slash = ref.indexOf('/')
  if (slash <= 0) return undefined
  const topic = topicBySlug[ref.slice(0, slash)]
  const example = topic?.examples.find((e) => e.id === ref.slice(slash + 1))
  return topic && example ? { topic, example } : undefined
}

export function exampleRef(topic: DocTopic, example: DocExample): string {
  return `${topic.slug}/${example.id}`
}
