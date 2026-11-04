<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { EditorView, basicSetup } from 'codemirror'
import { EditorState, Compartment } from '@codemirror/state'
import { keymap } from '@codemirror/view'
import { indentWithTab } from '@codemirror/commands'
import { StreamLanguage, indentUnit } from '@codemirror/language'
import { csharp } from '@codemirror/legacy-modes/mode/clike'
import { oneDark } from '@codemirror/theme-one-dark'

const props = defineProps<{ modelValue: string; readonly?: boolean }>()
const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>()

const host = ref<HTMLDivElement | null>(null)
let view: EditorView | null = null
const readonlyCompartment = new Compartment()

onMounted(() => {
  view = new EditorView({
    parent: host.value!,
    state: EditorState.create({
      doc: props.modelValue,
      extensions: [
        basicSetup,
        keymap.of([indentWithTab]),
        indentUnit.of('    '),
        StreamLanguage.define(csharp),
        oneDark,
        readonlyCompartment.of(EditorState.readOnly.of(!!props.readonly)),
        EditorView.updateListener.of((u) => {
          if (u.docChanged) emit('update:modelValue', u.state.doc.toString())
        }),
      ],
    }),
  })
})

watch(
  () => props.modelValue,
  (value) => {
    if (!view || value === view.state.doc.toString()) return
    view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value } })
  },
)

watch(
  () => props.readonly,
  (ro) => view?.dispatch({ effects: readonlyCompartment.reconfigure(EditorState.readOnly.of(!!ro)) }),
)

onBeforeUnmount(() => view?.destroy())
</script>

<template>
  <div ref="host" class="editor-host"></div>
</template>

<style scoped>
.editor-host { height: 100%; min-height: 200px; overflow: hidden; border-radius: 8px; }
.editor-host :deep(.cm-editor) { height: 100%; }
</style>
