# SharpCode

A small LeetCode-style app for C#: add problems with test cases, pick one, write a
solution in the browser, and have it compiled and judged against the test cases.

```
client/   Vite + Vue 3 + TypeScript front end (CodeMirror editor, Markdown statements)
server/
  SharpCode.Api/     ASP.NET Core minimal API, EF Core + SQLite, Roslyn compiler
  SharpCode.Runner/  Disposable console process that executes a compiled solution
```

## How judging works

1. The API compiles the submitted source with Roslyn into an in-memory assembly
   (`System`, `System.Collections.Generic`, `System.Linq`, `System.Text` are imported
   implicitly). Compile errors are returned with line/column.
2. The assembly plus the test cases are handed to `SharpCode.Runner`, a separate
   process. It instantiates the problem's class (default `Solution`), calls the
   configured method with the arguments from each test case, and prints one JSON
   result per test.
3. Arguments and expected outputs are JSON. Input is a JSON array of arguments
   (`[[2,7,11,15], 9]`), the expected value is the JSON-serialized return value
   (`[0,1]`). Comparison is structural JSON equality with a small tolerance for numbers.
4. Each test runs on its own thread with the problem's time limit. On a timeout the
   runner reports it and exits; on a crash (stack overflow, `Environment.Exit`) the
   API detects the missing results and reports a runtime error. Either way the API
   process is never affected.
5. **Run** executes only the visible sample cases and stores nothing. **Submit**
   executes every case, hidden ones included, and records the submission.

The runner is a plain child process, not a sandbox: user code has the same OS
permissions as the API. That is fine for a personal, local tool. Do not expose it to
untrusted users without adding real isolation (container, seccomp, etc.).

## Prerequisites

- .NET SDK 10 (`dotnet --version`)
- Node.js 22+

## Run it

Terminal 1, API on http://localhost:5055:

```bash
cd server/SharpCode.Api
dotnet run
```

Terminal 2, client on http://localhost:5173 (proxies `/api` to the API):

```bash
cd client
npm install
npm run dev
```

The SQLite database (`server/SharpCode.Api/sharpcode.db`) is created on first start
and seeded with four sample problems. Delete the file to reset.

## API

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/problems` | List problems (with solved flag) |
| GET | `/api/problems/{id-or-slug}` | Problem with its visible test cases |
| GET | `/api/problems/{id-or-slug}/full` | Problem including hidden test case details |
| POST | `/api/problems` | Create a problem with test cases |
| PUT | `/api/problems/{id}` | Update a problem; `testCases` replaces the whole set |
| DELETE | `/api/problems/{id}` | Delete a problem, its test cases and submissions |
| POST | `/api/problems/{id}/testcases` | Add one test case |
| PUT | `/api/testcases/{id}` | Update a test case |
| DELETE | `/api/testcases/{id}` | Delete a test case |
| POST | `/api/problems/{id}/run` | Compile and run the sample cases |
| POST | `/api/problems/{id}/submit` | Compile, run all cases, record a submission |
| GET | `/api/problems/{id}/submissions` | Submission history |
| GET | `/api/submissions/{id}` | One submission with its full judge result |

Problem payload:

```json
{
  "title": "Two Sum",
  "slug": "two-sum",
  "description": "Markdown…",
  "difficulty": "Easy",
  "starterCode": "public class Solution { public int[] TwoSum(int[] nums, int target) { … } }",
  "className": "Solution",
  "methodName": "TwoSum",
  "timeLimitMs": 2000,
  "testCases": [
    { "input": "[[2,7,11,15], 9]", "expected": "[0,1]", "isHidden": false }
  ]
}
```
