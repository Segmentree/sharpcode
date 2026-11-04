<script setup lang="ts">
import type { Difficulty } from '../api'
defineProps<{ difficulty: Difficulty }>()
</script>

<template>
  <span class="badge" :class="difficulty">{{ difficulty }}</span>
</template>
