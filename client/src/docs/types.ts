/** Text in every supported UI language. */
export interface Localized {
  en: string
  es: string
}

/** A runnable example: a complete console program that can be loaded into the Sandbox as-is. */
export interface DocExample {
  /** Unique within its topic, used in URLs: /sandbox?example=<topic>/<id> */
  id: string
  title: Localized
  /** Complete C# console program (top-level statements). */
  code: string
  /** Optional stdin to preload with the example. */
  stdin?: string
  /** Optional short remark shown under the code (Markdown). */
  note?: Localized
}

export interface DocTopic {
  /** URL slug, also what problems reference in `docLinks`. */
  slug: string
  title: Localized
  /** One sentence, shown in lists and on problem pages. */
  summary: Localized
  /** Brief explanation of the resource (Markdown). */
  body: Localized
  examples: DocExample[]
}
