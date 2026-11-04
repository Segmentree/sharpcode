<script setup lang="ts">
import { computed } from 'vue'
import { marked } from 'marked'

const props = defineProps<{ source: string }>()

function escapeHtml(s: string) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
}

// Problems are authored by the app's admin, but escape raw HTML anyway so a stray tag cannot break the page.
const html = computed(() => {
  const renderer = new marked.Renderer()
  renderer.html = ({ text }) => escapeHtml(text)
  return marked.parse(props.source ?? '', { renderer, async: false }) as string
})
</script>

<template>
  <div class="markdown" v-html="html"></div>
</template>
