<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { Difficulty } from '../api'

defineProps<{ difficulty: Difficulty }>()
const { t } = useI18n()
</script>

<template>
  <span class="badge" :class="difficulty">{{ t(`difficulty.${difficulty}`) }}</span>
</template>
