import { autocompletion, type CompletionContext, type CompletionResult, type Completion } from '@codemirror/autocomplete'
import { api } from './api'

// Maps the API's Roslyn-derived kinds onto CodeMirror's built-in completion icons.
const kindToType: Record<string, string> = {
  method: 'method',
  property: 'property',
  variable: 'variable',
  constant: 'constant',
  class: 'class',
  interface: 'interface',
  enum: 'enum',
  namespace: 'namespace',
  keyword: 'keyword',
  text: 'text',
}

/**
 * Asks the API (Roslyn) for completions at the cursor. The result is marked as valid while the user keeps typing
 * identifier characters, so CodeMirror filters the list locally (case-insensitive, fuzzy) without new requests.
 */
async function roslynSource(context: CompletionContext): Promise<CompletionResult | null> {
  const word = context.matchBefore(/[\w.]*/)
  if (!context.explicit && (!word || word.from === word.to)) return null

  const controller = new AbortController()
  context.addEventListener('abort', () => controller.abort())

  let response
  try {
    response = await api.complete(context.state.doc.toString(), context.pos, controller.signal)
  } catch {
    return null // aborted or API unreachable: fail quietly
  }
  if (controller.signal.aborted || response.items.length === 0) return null

  const options: Completion[] = response.items.map((item, i) => ({
    label: item.label,
    type: kindToType[item.kind] ?? 'text',
    detail: item.detail,
    boost: -i / response.items.length, // keep the API's relevance order among equal matches
  }))

  return { from: response.from, options, validFor: /^\w*$/ }
}

export function csharpCompletion() {
  return autocompletion({
    override: [roslynSource],
    activateOnTyping: true,
    maxRenderedOptions: 50,
    icons: true,
  })
}
