import type { DocTopic } from './types'

// The basics of C#, written for the School problems. Every example is a complete console program
// (top-level statements) that runs unchanged in the Sandbox. Keep examples short and print something.

export const topics: DocTopic[] = [
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'variables-and-types',
    title: { en: 'Variables and types', es: 'Variables y tipos' },
    summary: {
      en: 'Declare variables with int, double, string, bool and char, and let the compiler infer with var.',
      es: 'Declara variables con int, double, string, bool y char, y deja que el compilador infiera con var.',
    },
    body: {
      en: `A variable is a named box that holds a value of one **type**. You write the type, then the name, then an initial value.

| Type | Holds | Example |
| --- | --- | --- |
| \`int\` | whole numbers | \`int age = 30;\` |
| \`double\` | numbers with decimals | \`double price = 9.99;\` |
| \`string\` | text | \`string name = "Ana";\` |
| \`bool\` | \`true\` or \`false\` | \`bool active = true;\` |
| \`char\` | a single character | \`char op = '+';\` |

Notice the quotes: a \`string\` uses double quotes \`"..."\`, a \`char\` uses single quotes \`'x'\`.

\`var\` asks the compiler to pick the type from the value (\`var total = 3;\` is an \`int\`). The type is still fixed afterwards: you cannot put text into a variable that holds a number.

Dividing two \`int\` values throws the decimals away (\`7 / 2\` is \`3\`). Use a \`double\` when you need decimals (\`7.0 / 2\` is \`3.5\`).`,
      es: `Una variable es una caja con nombre que guarda un valor de un **tipo**. Escribes el tipo, luego el nombre y luego un valor inicial.

| Tipo | Guarda | Ejemplo |
| --- | --- | --- |
| \`int\` | números enteros | \`int age = 30;\` |
| \`double\` | números con decimales | \`double price = 9.99;\` |
| \`string\` | texto | \`string name = "Ana";\` |
| \`bool\` | \`true\` o \`false\` | \`bool active = true;\` |
| \`char\` | un único carácter | \`char op = '+';\` |

Fíjate en las comillas: un \`string\` usa comillas dobles \`"..."\` y un \`char\` comillas simples \`'x'\`.

\`var\` pide al compilador que deduzca el tipo a partir del valor (\`var total = 3;\` es un \`int\`). El tipo sigue siendo fijo: no puedes guardar texto en una variable que contiene un número.

Dividir dos valores \`int\` descarta los decimales (\`7 / 2\` es \`3\`). Usa \`double\` cuando necesites decimales (\`7.0 / 2\` es \`3.5\`).`,
    },
    examples: [
      {
        id: 'declare',
        title: { en: 'Declaring one of each', es: 'Declarar uno de cada tipo' },
        code: `int age = 30;
double price = 9.99;
string name = "Ana";
bool active = true;
char op = '+';

Console.WriteLine(age);
Console.WriteLine(price);
Console.WriteLine(name);
Console.WriteLine(active);
Console.WriteLine(op);
`,
      },
      {
        id: 'int-vs-double',
        title: { en: 'int division vs double division', es: 'División de int frente a double' },
        code: `int a = 7;
int b = 2;
Console.WriteLine(a / b);        // 3  (whole numbers only)
Console.WriteLine(a / 2.0);      // 3.5
Console.WriteLine((double)a / b); // 3.5 (cast a to double first)
Console.WriteLine(a % b);        // 1  (remainder)
`,
      },
      {
        id: 'var',
        title: { en: 'Using var', es: 'Usar var' },
        code: `var count = 3;        // int
var ratio = 0.5;      // double
var greeting = "Hi";  // string

count = count + 1;
Console.WriteLine(count);
Console.WriteLine(ratio * 2);
Console.WriteLine(greeting + "!");
`,
        note: {
          en: 'Try assigning `count = "three";` and run it: the compiler refuses because `count` is an `int`.',
          es: 'Prueba a asignar `count = "three";` y ejecuta: el compilador lo rechaza porque `count` es un `int`.',
        },
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'operators',
    title: { en: 'Operators', es: 'Operadores' },
    summary: {
      en: 'Arithmetic (+ - * / %), comparisons (== != < <= > >=) and logical operators (&& || !).',
      es: 'Aritméticos (+ - * / %), comparaciones (== != < <= > >=) y lógicos (&& || !).',
    },
    body: {
      en: `**Arithmetic**: \`+\`, \`-\`, \`*\`, \`/\` and \`%\`. The remainder operator \`%\` gives what is left after an integer division: \`17 % 5\` is \`2\`. A number is divisible by \`n\` exactly when \`number % n == 0\`.

**Comparisons** produce a \`bool\`: \`==\` (equal), \`!=\` (not equal), \`<\`, \`<=\`, \`>\`, \`>=\`. Note the double \`==\` for comparing; a single \`=\` assigns.

**Logical** operators combine \`bool\` values:

- \`a && b\` is true only when **both** are true.
- \`a || b\` is true when **at least one** is true.
- \`!a\` flips the value.

Use parentheses to make the intent clear: \`(year % 4 == 0 && year % 100 != 0) || year % 400 == 0\`.

**Shortcuts**: \`x += 5\` means \`x = x + 5\`; \`x++\` adds one.`,
      es: `**Aritméticos**: \`+\`, \`-\`, \`*\`, \`/\` y \`%\`. El operador resto \`%\` devuelve lo que sobra de una división entera: \`17 % 5\` es \`2\`. Un número es divisible entre \`n\` exactamente cuando \`number % n == 0\`.

**Comparaciones** producen un \`bool\`: \`==\` (igual), \`!=\` (distinto), \`<\`, \`<=\`, \`>\`, \`>=\`. Fíjate en el doble \`==\` para comparar; un solo \`=\` asigna.

**Lógicos** combinan valores \`bool\`:

- \`a && b\` es verdadero solo cuando **ambos** lo son.
- \`a || b\` es verdadero cuando **al menos uno** lo es.
- \`!a\` invierte el valor.

Usa paréntesis para dejar clara la intención: \`(year % 4 == 0 && year % 100 != 0) || year % 400 == 0\`.

**Atajos**: \`x += 5\` significa \`x = x + 5\`; \`x++\` suma uno.`,
    },
    examples: [
      {
        id: 'remainder',
        title: { en: 'Divisibility with %', es: 'Divisibilidad con %' },
        code: `int number = 35;

Console.WriteLine(number % 5);        // 0
Console.WriteLine(number % 5 == 0);   // True
Console.WriteLine(number % 2 == 0);   // False (odd)

bool divisibleBy5 = number % 5 == 0;
Console.WriteLine($"{number} divisible by 5? {divisibleBy5}");
`,
      },
      {
        id: 'comparisons',
        title: { en: 'Comparisons give bool', es: 'Las comparaciones dan bool' },
        code: `int age = 20;

Console.WriteLine(age < 12);    // False
Console.WriteLine(age >= 18);   // True
Console.WriteLine(age == 20);   // True
Console.WriteLine(age != 20);   // False

bool isAdult = age >= 18;
Console.WriteLine(isAdult);
`,
      },
      {
        id: 'logical',
        title: { en: 'Combining with && and ||', es: 'Combinar con && y ||' },
        code: `int year = 2024;

bool byFour = year % 4 == 0;
bool byHundred = year % 100 == 0;
bool byFourHundred = year % 400 == 0;

bool leap = (byFour && !byHundred) || byFourHundred;
Console.WriteLine($"{year} leap? {leap}");

Console.WriteLine(true && false);   // False
Console.WriteLine(true || false);   // True
Console.WriteLine(!true);           // False
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'if-else',
    title: { en: 'if, else if and else', es: 'if, else if y else' },
    summary: {
      en: 'Run code only when a condition is true, with else if chains for several ranges.',
      es: 'Ejecuta código solo cuando una condición es verdadera, con cadenas de else if para varios rangos.',
    },
    body: {
      en: `\`if\` runs a block when its condition (a \`bool\`) is true. \`else\` runs when it is not. \`else if\` lets you check several conditions in order: the **first** true one wins and the rest are skipped.

\`\`\`csharp
if (age < 12)
{
    discount = 50;
}
else if (age <= 65)
{
    discount = 0;
}
else
{
    discount = 30;
}
\`\`\`

Because the checks run in order, you can write ranges simply: once \`age < 12\` is false, the next check \`age <= 65\` already knows the age is at least 12.

Braces \`{ }\` group the statements of a block; use them even for one line so the structure stays obvious. Conditions can be nested, and you can combine them with \`&&\` and \`||\` instead of nesting.`,
      es: `\`if\` ejecuta un bloque cuando su condición (un \`bool\`) es verdadera. \`else\` se ejecuta cuando no lo es. \`else if\` permite comprobar varias condiciones en orden: gana la **primera** verdadera y el resto se omiten.

\`\`\`csharp
if (age < 12)
{
    discount = 50;
}
else if (age <= 65)
{
    discount = 0;
}
else
{
    discount = 30;
}
\`\`\`

Como las comprobaciones van en orden, los rangos se escriben de forma sencilla: cuando \`age < 12\` es falso, la siguiente comprobación \`age <= 65\` ya sabe que la edad es al menos 12.

Las llaves \`{ }\` agrupan las instrucciones de un bloque; úsalas incluso para una sola línea para que la estructura quede clara. Las condiciones se pueden anidar, y también combinar con \`&&\` y \`||\` en lugar de anidar.`,
    },
    examples: [
      {
        id: 'basic',
        title: { en: 'if / else', es: 'if / else' },
        code: `int number = 42;

if (number % 2 == 0)
{
    Console.WriteLine($"{number} is even");
}
else
{
    Console.WriteLine($"{number} is odd");
}
`,
      },
      {
        id: 'chain',
        title: { en: 'Ranges with else if', es: 'Rangos con else if' },
        code: `double speed = 72.5;

if (speed < 50)
{
    Console.WriteLine("Velocidad baja");
}
else if (speed <= 100)
{
    Console.WriteLine("Velocidad adecuada");
}
else
{
    Console.WriteLine("Velocidad alta");
}
`,
        note: {
          en: 'Change `speed` to 50, 100 and 100.5 to see which branch each boundary falls into.',
          es: 'Cambia `speed` a 50, 100 y 100.5 para ver en qué rama cae cada límite.',
        },
      },
      {
        id: 'validation-first',
        title: { en: 'Check invalid input first', es: 'Comprobar primero la entrada no válida' },
        code: `double grade = 11;

if (grade < 0 || grade > 10)
{
    Console.WriteLine("Nota no válida");
}
else if (grade < 5)
{
    Console.WriteLine("Insuficiente");
}
else if (grade < 9)
{
    Console.WriteLine("Aprobado");
}
else
{
    Console.WriteLine("Sobresaliente");
}
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'switch',
    title: { en: 'switch', es: 'switch' },
    summary: {
      en: 'Choose between several fixed values, such as the operator of a calculator, without a long if chain.',
      es: 'Elige entre varios valores fijos, como el operador de una calculadora, sin una larga cadena de if.',
    },
    body: {
      en: `When you compare the same variable against several **fixed values**, \`switch\` is clearer than a chain of \`if\`s.

The classic statement form uses \`case\` labels and \`break\`:

\`\`\`csharp
switch (op)
{
    case '+': result = a + b; break;
    case '-': result = a - b; break;
    default:  Console.WriteLine("Unknown"); break;
}
\`\`\`

The newer **switch expression** returns a value directly and is great for short mappings. \`_\` is the "anything else" case:

\`\`\`csharp
string name = day switch
{
    1 => "Monday",
    7 => "Sunday",
    _ => "Other"
};
\`\`\`

A switch expression can also use conditions with \`when\` or relational patterns like \`< 50\`.`,
      es: `Cuando comparas la misma variable con varios **valores fijos**, \`switch\` queda más claro que una cadena de \`if\`.

La forma clásica usa etiquetas \`case\` y \`break\`:

\`\`\`csharp
switch (op)
{
    case '+': result = a + b; break;
    case '-': result = a - b; break;
    default:  Console.WriteLine("Desconocido"); break;
}
\`\`\`

La **expresión switch**, más moderna, devuelve un valor directamente y es ideal para correspondencias cortas. \`_\` es el caso "cualquier otro":

\`\`\`csharp
string name = day switch
{
    1 => "Lunes",
    7 => "Domingo",
    _ => "Otro"
};
\`\`\`

Una expresión switch también admite condiciones con \`when\` o patrones relacionales como \`< 50\`.`,
    },
    examples: [
      {
        id: 'statement',
        title: { en: 'Calculator with a switch statement', es: 'Calculadora con una sentencia switch' },
        code: `double a = 6;
double b = 3;
char op = '/';

switch (op)
{
    case '+':
        Console.WriteLine(a + b);
        break;
    case '-':
        Console.WriteLine(a - b);
        break;
    case '*':
        Console.WriteLine(a * b);
        break;
    case '/':
        if (b == 0)
            Console.WriteLine("Error: division by zero");
        else
            Console.WriteLine(a / b);
        break;
    default:
        Console.WriteLine("Unknown operator");
        break;
}
`,
      },
      {
        id: 'expression',
        title: { en: 'Switch expression', es: 'Expresión switch' },
        code: `int day = 3;

string name = day switch
{
    1 => "Monday",
    2 => "Tuesday",
    3 => "Wednesday",
    4 => "Thursday",
    5 => "Friday",
    6 or 7 => "Weekend",
    _ => "Invalid day"
};

Console.WriteLine(name);
`,
      },
      {
        id: 'relational',
        title: { en: 'Ranges with relational patterns', es: 'Rangos con patrones relacionales' },
        code: `double speed = 120;

string label = speed switch
{
    < 50 => "Velocidad baja",
    <= 100 => "Velocidad adecuada",
    _ => "Velocidad alta"
};

Console.WriteLine(label);
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'strings',
    title: { en: 'Strings', es: 'Cadenas de texto' },
    summary: {
      en: 'Build text with interpolation, compare strings, and use Length, indexing and common methods.',
      es: 'Construye texto con interpolación, compara cadenas y usa Length, índices y métodos habituales.',
    },
    body: {
      en: `A \`string\` is a sequence of characters. Useful things to know:

- **Interpolation**: \`$"Hello {name}, you are {age}"\` inserts values into text. Expressions work too: \`{price * 2}\`.
- **Concatenation**: \`"a" + "b"\` is \`"ab"\`.
- **Length**: \`s.Length\` is the number of characters. \`s[0]\` is the first character (a \`char\`), \`s[s.Length - 1]\` the last.
- **Comparison**: \`a == b\` compares the text exactly (case matters: \`"ana" == "Ana"\` is false). \`a.Equals(b, StringComparison.OrdinalIgnoreCase)\` ignores case.
- **Methods**: \`ToUpper()\`, \`ToLower()\`, \`Trim()\`, \`Contains("x")\`, \`StartsWith("x")\`, \`Replace("a", "b")\`, \`Substring(start, length)\`, \`Split(' ')\`.
- A string cannot be changed in place: every method returns a **new** string. To rebuild one character by character, use \`ToCharArray()\` or a \`StringBuilder\`.
- \`string.IsNullOrEmpty(s)\` checks for missing or empty text.`,
      es: `Un \`string\` es una secuencia de caracteres. Cosas útiles:

- **Interpolación**: \`$"Hola {name}, tienes {age}"\` inserta valores en el texto. También admite expresiones: \`{price * 2}\`.
- **Concatenación**: \`"a" + "b"\` es \`"ab"\`.
- **Length**: \`s.Length\` es el número de caracteres. \`s[0]\` es el primer carácter (un \`char\`), \`s[s.Length - 1]\` el último.
- **Comparación**: \`a == b\` compara el texto exactamente (distingue mayúsculas: \`"ana" == "Ana"\` es falso). \`a.Equals(b, StringComparison.OrdinalIgnoreCase)\` ignora mayúsculas.
- **Métodos**: \`ToUpper()\`, \`ToLower()\`, \`Trim()\`, \`Contains("x")\`, \`StartsWith("x")\`, \`Replace("a", "b")\`, \`Substring(inicio, longitud)\`, \`Split(' ')\`.
- Una cadena no se puede modificar en el sitio: cada método devuelve una cadena **nueva**. Para reconstruirla carácter a carácter usa \`ToCharArray()\` o un \`StringBuilder\`.
- \`string.IsNullOrEmpty(s)\` comprueba si falta texto o está vacío.`,
    },
    examples: [
      {
        id: 'interpolation',
        title: { en: 'Interpolation and Length', es: 'Interpolación y Length' },
        code: `string name = "Ana";
int age = 30;
double price = 9.5;

Console.WriteLine($"{name} is {age} years old.");
Console.WriteLine($"Double price: {price * 2}");
Console.WriteLine($"Name has {name.Length} letters; first is {name[0]}, last is {name[name.Length - 1]}");
`,
      },
      {
        id: 'compare',
        title: { en: 'Comparing strings', es: 'Comparar cadenas' },
        code: `string stored = "ana";
string entered = "Ana";

Console.WriteLine(stored == entered);                                   // False
Console.WriteLine(stored.Equals(entered, StringComparison.OrdinalIgnoreCase)); // True
Console.WriteLine(stored.ToLower() == entered.ToLower());               // True

bool accountActive = true;
bool allowed = stored == entered && accountActive;
Console.WriteLine(allowed ? "Access granted" : "Access denied");
`,
      },
      {
        id: 'reverse',
        title: { en: 'Walking characters and reversing', es: 'Recorrer caracteres e invertir' },
        code: `string s = "hello";

// Build the reversed text one character at a time, from the end.
string reversed = "";
for (int i = s.Length - 1; i >= 0; i--)
{
    reversed += s[i];
}
Console.WriteLine(reversed);

// The same with an array of characters.
char[] chars = s.ToCharArray();
Array.Reverse(chars);
Console.WriteLine(new string(chars));

Console.WriteLine(s.ToUpper());
Console.WriteLine(s.Contains("ell"));
Console.WriteLine(s.Replace("l", "L"));
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'console-io',
    title: { en: 'Console input and output', es: 'Entrada y salida por consola' },
    summary: {
      en: 'Print with Console.WriteLine, read text with Console.ReadLine, and convert it to numbers.',
      es: 'Imprime con Console.WriteLine, lee texto con Console.ReadLine y conviértelo a números.',
    },
    body: {
      en: `\`Console.WriteLine(x)\` prints a value followed by a new line; \`Console.Write(x)\` prints without the line break.

\`Console.ReadLine()\` reads one line of input as a \`string\` (or \`null\` when there is no more input). To turn it into a number:

- \`int.Parse(text)\` / \`double.Parse(text)\`: convert, throwing an error if the text is not a number.
- \`int.TryParse(text, out int value)\`: returns \`false\` instead of throwing, which is the safe way to handle user input.

In the Sandbox, the "Standard input" box provides the lines that \`ReadLine()\` returns, one per call. In the problems, you do not read input at all: the judge passes the values as method parameters.`,
      es: `\`Console.WriteLine(x)\` imprime un valor seguido de un salto de línea; \`Console.Write(x)\` imprime sin el salto.

\`Console.ReadLine()\` lee una línea de entrada como \`string\` (o \`null\` cuando no queda entrada). Para convertirla en número:

- \`int.Parse(texto)\` / \`double.Parse(texto)\`: convierten y lanzan un error si el texto no es un número.
- \`int.TryParse(texto, out int valor)\`: devuelve \`false\` en lugar de lanzar un error; es la forma segura de tratar la entrada del usuario.

En el Sandbox, la caja "Entrada estándar" proporciona las líneas que devuelve \`ReadLine()\`, una por llamada. En los problemas no se lee entrada: el juez pasa los valores como parámetros del método.`,
    },
    examples: [
      {
        id: 'read-name',
        title: { en: 'Read a name and a number', es: 'Leer un nombre y un número' },
        code: `Console.Write("Name: ");
string name = Console.ReadLine() ?? "";

Console.Write("Age: ");
int age = int.Parse(Console.ReadLine() ?? "0");

Console.WriteLine();
Console.WriteLine($"Hello {name}! Next year you will be {age + 1}.");
`,
        stdin: 'Ana\n30\n',
      },
      {
        id: 'tryparse',
        title: { en: 'Safe conversion with TryParse', es: 'Conversión segura con TryParse' },
        code: `string text = Console.ReadLine() ?? "";

if (int.TryParse(text, out int number))
{
    Console.WriteLine($"You typed the number {number}");
}
else
{
    Console.WriteLine($"'{text}' is not a whole number");
}
`,
        stdin: 'forty-two\n',
        note: {
          en: 'Change the standard input to `42` and run again.',
          es: 'Cambia la entrada estándar a `42` y vuelve a ejecutar.',
        },
      },
      {
        id: 'several-lines',
        title: { en: 'Reading until the input ends', es: 'Leer hasta que se acabe la entrada' },
        code: `double total = 0;
int count = 0;

string? line;
while ((line = Console.ReadLine()) != null)
{
    total += double.Parse(line);
    count++;
}

Console.WriteLine($"{count} numbers, total {total}, average {total / count}");
`,
        stdin: '10\n20\n30.5\n',
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'loops',
    title: { en: 'Loops', es: 'Bucles' },
    summary: {
      en: 'Repeat work with for, while and foreach, and stop early with break or skip with continue.',
      es: 'Repite trabajo con for, while y foreach, y sal antes con break o salta con continue.',
    },
    body: {
      en: `- \`for (int i = 0; i < n; i++) { ... }\` runs with \`i\` taking the values \`0, 1, ..., n-1\`. The three parts are: start, condition checked before each turn, and the step.
- \`foreach (var item in collection) { ... }\` visits every element of an array, list or string without managing an index.
- \`while (condition) { ... }\` repeats as long as the condition is true; make sure something inside changes it, or it never ends.
- \`break\` leaves the loop immediately; \`continue\` jumps to the next turn.

To count from 1 to \`n\` inclusive write \`for (int i = 1; i <= n; i++)\`. To walk a string backwards write \`for (int i = s.Length - 1; i >= 0; i--)\`.`,
      es: `- \`for (int i = 0; i < n; i++) { ... }\` ejecuta con \`i\` tomando los valores \`0, 1, ..., n-1\`. Las tres partes son: inicio, condición que se comprueba antes de cada vuelta y paso.
- \`foreach (var item in coleccion) { ... }\` visita cada elemento de un array, lista o cadena sin gestionar un índice.
- \`while (condicion) { ... }\` repite mientras la condición sea verdadera; asegúrate de que algo dentro la cambie o no terminará nunca.
- \`break\` sale del bucle inmediatamente; \`continue\` salta a la siguiente vuelta.

Para contar de 1 a \`n\` incluido escribe \`for (int i = 1; i <= n; i++)\`. Para recorrer una cadena hacia atrás escribe \`for (int i = s.Length - 1; i >= 0; i--)\`.`,
    },
    examples: [
      {
        id: 'for',
        title: { en: 'Counting with for (FizzBuzz)', es: 'Contar con for (FizzBuzz)' },
        code: `int n = 15;

for (int i = 1; i <= n; i++)
{
    if (i % 15 == 0)
        Console.WriteLine("FizzBuzz");
    else if (i % 3 == 0)
        Console.WriteLine("Fizz");
    else if (i % 5 == 0)
        Console.WriteLine("Buzz");
    else
        Console.WriteLine(i);
}
`,
      },
      {
        id: 'foreach',
        title: { en: 'foreach over an array', es: 'foreach sobre un array' },
        code: `int[] nums = { 4, 8, 15, 16, 23, 42 };
int sum = 0;

foreach (int value in nums)
{
    sum += value;
}

Console.WriteLine($"Sum: {sum}");

foreach (char c in "hey")
{
    Console.WriteLine(c);
}
`,
      },
      {
        id: 'while-break',
        title: { en: 'while, break and continue', es: 'while, break y continue' },
        code: `int i = 0;
while (true)
{
    i++;
    if (i % 2 == 0) continue;   // skip even numbers
    if (i > 9) break;           // stop after 9
    Console.WriteLine(i);
}

// Nested loops: every pair (i, j) with i < j
int[] nums = { 1, 2, 3 };
for (int a = 0; a < nums.Length; a++)
    for (int b = a + 1; b < nums.Length; b++)
        Console.WriteLine($"({nums[a]}, {nums[b]})");
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'arrays',
    title: { en: 'Arrays', es: 'Arrays' },
    summary: {
      en: 'Fixed-size sequences: create them, read and write by index, and use Length.',
      es: 'Secuencias de tamaño fijo: créalas, lee y escribe por índice y usa Length.',
    },
    body: {
      en: `An array holds a fixed number of values of one type. Indexes start at **0**, so the last element is at \`Length - 1\`. Reading outside that range throws an \`IndexOutOfRangeException\`.

\`\`\`csharp
int[] nums = { 2, 7, 11, 15 };   // create with values
int[] empty = new int[4];       // four zeros
nums[0] = 5;                    // write
Console.WriteLine(nums.Length); // 4
\`\`\`

Handy helpers from the \`Array\` class and LINQ: \`Array.Reverse(a)\`, \`Array.Sort(a)\`, \`Array.IndexOf(a, value)\`, \`a.Max()\`, \`a.Min()\`, \`a.Sum()\`, \`a.Contains(x)\`.

To return a new array from a method write \`return new[] { i, j };\`.`,
      es: `Un array guarda un número fijo de valores de un mismo tipo. Los índices empiezan en **0**, así que el último elemento está en \`Length - 1\`. Leer fuera de ese rango lanza una \`IndexOutOfRangeException\`.

\`\`\`csharp
int[] nums = { 2, 7, 11, 15 };   // crear con valores
int[] empty = new int[4];       // cuatro ceros
nums[0] = 5;                    // escribir
Console.WriteLine(nums.Length); // 4
\`\`\`

Ayudas útiles de la clase \`Array\` y de LINQ: \`Array.Reverse(a)\`, \`Array.Sort(a)\`, \`Array.IndexOf(a, valor)\`, \`a.Max()\`, \`a.Min()\`, \`a.Sum()\`, \`a.Contains(x)\`.

Para devolver un array nuevo desde un método escribe \`return new[] { i, j };\`.`,
    },
    examples: [
      {
        id: 'basics',
        title: { en: 'Create, index, Length', es: 'Crear, indexar, Length' },
        code: `int[] nums = { 2, 7, 11, 15 };

Console.WriteLine(nums.Length);        // 4
Console.WriteLine(nums[0]);            // 2
Console.WriteLine(nums[nums.Length - 1]); // 15

nums[1] = 70;
Console.WriteLine(string.Join(", ", nums));

int[] zeros = new int[3];
Console.WriteLine(string.Join(", ", zeros));
`,
      },
      {
        id: 'search',
        title: { en: 'Searching with a loop (Two Sum)', es: 'Buscar con un bucle (Two Sum)' },
        code: `int[] nums = { 2, 7, 11, 15 };
int target = 9;

for (int i = 0; i < nums.Length; i++)
{
    for (int j = i + 1; j < nums.Length; j++)
    {
        if (nums[i] + nums[j] == target)
        {
            int[] answer = new[] { i, j };
            Console.WriteLine($"Indices: [{answer[0]}, {answer[1]}]");
        }
    }
}
`,
      },
      {
        id: 'helpers',
        title: { en: 'Array helpers', es: 'Ayudas de Array' },
        code: `int[] nums = { 5, 3, 9, 1 };

Console.WriteLine(nums.Max());   // 9
Console.WriteLine(nums.Min());   // 1
Console.WriteLine(nums.Sum());   // 18
Console.WriteLine(Array.IndexOf(nums, 9)); // 2

Array.Sort(nums);
Console.WriteLine(string.Join(" ", nums)); // 1 3 5 9

Array.Reverse(nums);
Console.WriteLine(string.Join(" ", nums)); // 9 5 3 1
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'lists',
    title: { en: 'Lists', es: 'Listas' },
    summary: {
      en: 'List<T> grows as you Add items; use Count, indexing, Contains and Remove.',
      es: 'List<T> crece a medida que haces Add; usa Count, índices, Contains y Remove.',
    },
    body: {
      en: `A \`List<T>\` is like an array that can grow. \`T\` is the element type: \`List<int>\`, \`List<string>\`.

\`\`\`csharp
var names = new List<string>();
names.Add("Ana");
names.Add("Luis");
Console.WriteLine(names.Count);   // 2  (Count, not Length)
Console.WriteLine(names[0]);      // Ana
\`\`\`

Other common members: \`Insert(index, item)\`, \`Remove(item)\`, \`RemoveAt(index)\`, \`Contains(item)\`, \`IndexOf(item)\`, \`Clear()\`, \`Sort()\`, and \`ToArray()\` to get an array back.

Problems that must return "a list of strings" use \`List<string>\`: create it, \`Add\` each value in order, and return it.`,
      es: `Una \`List<T>\` es como un array que puede crecer. \`T\` es el tipo de los elementos: \`List<int>\`, \`List<string>\`.

\`\`\`csharp
var names = new List<string>();
names.Add("Ana");
names.Add("Luis");
Console.WriteLine(names.Count);   // 2  (Count, no Length)
Console.WriteLine(names[0]);      // Ana
\`\`\`

Otros miembros habituales: \`Insert(indice, elemento)\`, \`Remove(elemento)\`, \`RemoveAt(indice)\`, \`Contains(elemento)\`, \`IndexOf(elemento)\`, \`Clear()\`, \`Sort()\` y \`ToArray()\` para obtener un array.

Los problemas que deben devolver "una lista de cadenas" usan \`List<string>\`: créala, haz \`Add\` de cada valor en orden y devuélvela.`,
    },
    examples: [
      {
        id: 'basics',
        title: { en: 'Add, Count, index', es: 'Add, Count, índice' },
        code: `var names = new List<string>();
names.Add("Ana");
names.Add("Luis");
names.Add("Marta");

Console.WriteLine(names.Count);
Console.WriteLine(names[1]);
Console.WriteLine(names.Contains("Ana"));

names.Remove("Luis");
Console.WriteLine(string.Join(", ", names));
`,
      },
      {
        id: 'build',
        title: { en: 'Building a result list', es: 'Construir una lista de resultados' },
        code: `int n = 10;
var result = new List<string>();

for (int i = 1; i <= n; i++)
{
    if (i % 15 == 0) result.Add("FizzBuzz");
    else if (i % 3 == 0) result.Add("Fizz");
    else if (i % 5 == 0) result.Add("Buzz");
    else result.Add(i.ToString());
}

Console.WriteLine(string.Join(" ", result));
`,
      },
      {
        id: 'init-sort',
        title: { en: 'Initializers and Sort', es: 'Inicializadores y Sort' },
        code: `var scores = new List<int> { 7, 2, 9, 4 };
scores.Add(5);
scores.Sort();

Console.WriteLine(string.Join(" ", scores));
Console.WriteLine($"Highest: {scores[scores.Count - 1]}");
Console.WriteLine($"Average: {scores.Average()}");
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'dictionaries',
    title: { en: 'Dictionaries', es: 'Diccionarios' },
    summary: {
      en: 'Dictionary<TKey, TValue> maps keys to values for instant lookups: TryGetValue, ContainsKey, indexer.',
      es: 'Dictionary<TKey, TValue> asocia claves con valores para búsquedas instantáneas: TryGetValue, ContainsKey, indexador.',
    },
    body: {
      en: `A \`Dictionary<TKey, TValue>\` stores pairs and finds a value by its key immediately, no matter how many entries it has. Keys are unique.

\`\`\`csharp
var ages = new Dictionary<string, int>();
ages["Ana"] = 30;             // add or overwrite
ages.Add("Luis", 25);         // add (throws if the key exists)
int a = ages["Ana"];          // read (throws if missing)
bool has = ages.ContainsKey("Eva");
\`\`\`

\`TryGetValue\` reads safely: \`if (ages.TryGetValue("Eva", out int age)) { ... }\`. The variable is filled only when the key exists.

Typical uses: counting occurrences (\`counts[c] = counts.GetValueOrDefault(c) + 1\`), remembering where something was last seen (index by value), or caching results. Loop with \`foreach (var (key, value) in dict)\`.`,
      es: `Un \`Dictionary<TKey, TValue>\` guarda pares y encuentra un valor por su clave de inmediato, sin importar cuántas entradas tenga. Las claves son únicas.

\`\`\`csharp
var ages = new Dictionary<string, int>();
ages["Ana"] = 30;             // añadir o sobrescribir
ages.Add("Luis", 25);         // añadir (lanza error si la clave existe)
int a = ages["Ana"];          // leer (lanza error si no existe)
bool has = ages.ContainsKey("Eva");
\`\`\`

\`TryGetValue\` lee de forma segura: \`if (ages.TryGetValue("Eva", out int age)) { ... }\`. La variable solo se rellena cuando la clave existe.

Usos típicos: contar apariciones (\`counts[c] = counts.GetValueOrDefault(c) + 1\`), recordar dónde se vio algo por última vez (índice por valor) o guardar resultados. Recorre con \`foreach (var (key, value) in dict)\`.`,
    },
    examples: [
      {
        id: 'basics',
        title: { en: 'Add, read, TryGetValue', es: 'Añadir, leer, TryGetValue' },
        code: `var ages = new Dictionary<string, int>();
ages["Ana"] = 30;
ages["Luis"] = 25;

Console.WriteLine(ages["Ana"]);
Console.WriteLine(ages.ContainsKey("Eva"));

if (ages.TryGetValue("Luis", out int age))
    Console.WriteLine($"Luis is {age}");

if (!ages.TryGetValue("Eva", out int missing))
    Console.WriteLine("Eva is not in the dictionary");

foreach (var (name, value) in ages)
    Console.WriteLine($"{name} -> {value}");
`,
      },
      {
        id: 'counting',
        title: { en: 'Counting characters', es: 'Contar caracteres' },
        code: `string text = "hello world";
var counts = new Dictionary<char, int>();

foreach (char c in text)
{
    if (c == ' ') continue;
    counts[c] = counts.GetValueOrDefault(c) + 1;
}

foreach (var (letter, count) in counts)
    Console.WriteLine($"{letter}: {count}");
`,
      },
      {
        id: 'two-sum',
        title: { en: 'Two Sum in one pass', es: 'Two Sum en una pasada' },
        code: `int[] nums = { 2, 7, 11, 15 };
int target = 9;

// value -> index of where we saw it
var seen = new Dictionary<int, int>();

for (int i = 0; i < nums.Length; i++)
{
    int needed = target - nums[i];
    if (seen.TryGetValue(needed, out int j))
    {
        Console.WriteLine($"[{j}, {i}]");
        break;
    }
    seen[nums[i]] = i;
}
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'methods',
    title: { en: 'Methods', es: 'Métodos' },
    summary: {
      en: 'Define reusable code with parameters and a return value; this is what the judge calls on your Solution class.',
      es: 'Define código reutilizable con parámetros y un valor de retorno; es lo que el juez llama en tu clase Solution.',
    },
    body: {
      en: `A method has a **return type**, a **name**, a list of **parameters**, and a body. \`return\` sends the result back and ends the method. \`void\` means it returns nothing.

\`\`\`csharp
public class Solution
{
    public int DiscountByAge(int age)
    {
        if (age < 12) return 50;
        if (age <= 65) return 0;
        return 30;
    }
}
\`\`\`

Every problem gives you a class and a method signature like this: keep the name, the parameter types and the return type exactly as given, and return the value instead of printing it. Once a \`return\` runs, nothing after it executes, which is why early returns can replace \`else\`.

A short method can use the expression form: \`public bool IsEven(int n) => n % 2 == 0;\`. You can add extra helper methods to the class and call them from the main one.`,
      es: `Un método tiene un **tipo de retorno**, un **nombre**, una lista de **parámetros** y un cuerpo. \`return\` devuelve el resultado y termina el método. \`void\` significa que no devuelve nada.

\`\`\`csharp
public class Solution
{
    public int DiscountByAge(int age)
    {
        if (age < 12) return 50;
        if (age <= 65) return 0;
        return 30;
    }
}
\`\`\`

Cada problema te da una clase y una firma de método como esta: mantén el nombre, los tipos de los parámetros y el tipo de retorno exactamente como se indican, y devuelve el valor en lugar de imprimirlo. Cuando se ejecuta un \`return\`, nada de lo que sigue se ejecuta, por eso los retornos tempranos pueden sustituir al \`else\`.

Un método corto puede usar la forma de expresión: \`public bool IsEven(int n) => n % 2 == 0;\`. Puedes añadir métodos auxiliares a la clase y llamarlos desde el principal.`,
    },
    examples: [
      {
        id: 'define-call',
        title: { en: 'Define and call', es: 'Definir y llamar' },
        code: `int Square(int x)
{
    return x * x;
}

bool IsEven(int n) => n % 2 == 0;

void Greet(string name)
{
    Console.WriteLine($"Hello, {name}!");
}

Console.WriteLine(Square(7));
Console.WriteLine(IsEven(10));
Greet("Ana");
`,
      },
      {
        id: 'solution-class',
        title: { en: 'A Solution class like the problems use', es: 'Una clase Solution como la de los problemas' },
        code: `var solution = new Solution();
Console.WriteLine(solution.DiscountByAge(8));
Console.WriteLine(solution.DiscountByAge(40));
Console.WriteLine(solution.DiscountByAge(70));

public class Solution
{
    public int DiscountByAge(int age)
    {
        if (age < 12) return 50;
        if (age <= 65) return 0;
        return 30;
    }
}
`,
        note: {
          en: 'In a console program the class goes after the top-level statements. In the problems you only write the class.',
          es: 'En un programa de consola la clase va después de las instrucciones de nivel superior. En los problemas solo escribes la clase.',
        },
      },
      {
        id: 'helpers',
        title: { en: 'Helper methods', es: 'Métodos auxiliares' },
        code: `var s = new Solution();
Console.WriteLine(s.LargestOfThree(4, 9, 2));
Console.WriteLine(s.LargestOfThree(7, 7, 7));

public class Solution
{
    public int LargestOfThree(int a, int b, int c)
    {
        return Larger(Larger(a, b), c);
    }

    private int Larger(int x, int y)
    {
        if (x >= y) return x;
        return y;
    }
}
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'nullable-types',
    title: { en: 'null and nullable types', es: 'null y tipos anulables' },
    summary: {
      en: 'Represent "no value" with null, use double? for optional numbers, and check with HasValue or ??.',
      es: 'Representa "sin valor" con null, usa double? para números opcionales y comprueba con HasValue o ??.',
    },
    body: {
      en: `\`null\` means "no value". Reference types such as \`string\` can hold it; the compiler warns you when a value might be null (\`string?\`).

Value types like \`int\` and \`double\` cannot be null unless you add \`?\`: a \`double?\` holds either a number or \`null\`. This is handy for "no result" cases, such as a calculator that cannot divide by zero.

\`\`\`csharp
double? result = null;
if (b != 0) result = a / b;

if (result.HasValue) Console.WriteLine(result.Value);
Console.WriteLine(result ?? 0);     // ?? gives a fallback when null
\`\`\`

- \`x ?? fallback\` returns \`x\` unless it is null.
- \`x?.Length\` stops and gives null instead of crashing when \`x\` is null.
- A method with return type \`double?\` can \`return null;\` or \`return 2.5;\`.`,
      es: `\`null\` significa "sin valor". Los tipos por referencia como \`string\` pueden contenerlo; el compilador avisa cuando un valor puede ser null (\`string?\`).

Los tipos por valor como \`int\` y \`double\` no pueden ser null salvo que añadas \`?\`: un \`double?\` contiene un número o \`null\`. Es útil para casos de "sin resultado", como una calculadora que no puede dividir entre cero.

\`\`\`csharp
double? result = null;
if (b != 0) result = a / b;

if (result.HasValue) Console.WriteLine(result.Value);
Console.WriteLine(result ?? 0);     // ?? da un valor alternativo cuando es null
\`\`\`

- \`x ?? alternativa\` devuelve \`x\` salvo que sea null.
- \`x?.Length\` se detiene y da null en lugar de fallar cuando \`x\` es null.
- Un método con tipo de retorno \`double?\` puede hacer \`return null;\` o \`return 2.5;\`.`,
    },
    examples: [
      {
        id: 'double-nullable',
        title: { en: 'double? for a division that may fail', es: 'double? para una división que puede fallar' },
        code: `double a = 10;
double b = 0;

double? result = null;
if (b != 0)
{
    result = a / b;
}

if (result.HasValue)
    Console.WriteLine($"Result: {result.Value}");
else
    Console.WriteLine("Error: cannot divide by zero");

Console.WriteLine(result ?? -1);
`,
        note: {
          en: 'Set `b = 4` to see the other branch.',
          es: 'Pon `b = 4` para ver la otra rama.',
        },
      },
      {
        id: 'method',
        title: { en: 'Returning null from a method', es: 'Devolver null desde un método' },
        code: `double? Calculate(double a, double b, char op)
{
    if (op == '+') return a + b;
    if (op == '-') return a - b;
    if (op == '*') return a * b;
    if (op == '/' && b != 0) return a / b;
    return null; // division by zero or unknown operator
}

Console.WriteLine(Calculate(6, 3, '/'));
Console.WriteLine(Calculate(6, 0, '/') == null ? "null" : "number");
Console.WriteLine(Calculate(2, 2, '%') ?? double.NaN);
`,
      },
      {
        id: 'strings',
        title: { en: 'Null strings and ??', es: 'Cadenas null y ??' },
        code: `string? maybe = Console.ReadLine();   // null when there is no input

Console.WriteLine(maybe == null ? "nothing was read" : $"read: {maybe}");
Console.WriteLine(maybe?.Length);         // prints nothing when null
Console.WriteLine((maybe ?? "default").ToUpper());
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'linq-basics',
    title: { en: 'LINQ basics', es: 'LINQ básico' },
    summary: {
      en: 'Transform and filter collections in one line with Select, Where, Enumerable.Range and ToList.',
      es: 'Transforma y filtra colecciones en una línea con Select, Where, Enumerable.Range y ToList.',
    },
    body: {
      en: `LINQ adds methods to arrays, lists and strings for working with all their elements at once. The most useful ones:

- \`Enumerable.Range(1, n)\` produces the numbers \`1..n\`.
- \`.Select(x => expr)\` transforms each element.
- \`.Where(x => condition)\` keeps the elements that match.
- \`.Sum()\`, \`.Max()\`, \`.Min()\`, \`.Count()\`, \`.Average()\`, \`.Any(cond)\`, \`.All(cond)\`.
- \`.ToList()\` / \`.ToArray()\` turn the result into a list or array.
- \`.Reverse()\` and \`.Distinct()\` do what they say.

The \`x => ...\` part is a **lambda**: a tiny function whose parameter is \`x\`. These calls chain left to right, so you can read them as a pipeline. Loops do the same job; LINQ is just shorter once you are used to it.`,
      es: `LINQ añade métodos a arrays, listas y cadenas para trabajar con todos sus elementos a la vez. Los más útiles:

- \`Enumerable.Range(1, n)\` produce los números \`1..n\`.
- \`.Select(x => expr)\` transforma cada elemento.
- \`.Where(x => condicion)\` conserva los elementos que cumplen la condición.
- \`.Sum()\`, \`.Max()\`, \`.Min()\`, \`.Count()\`, \`.Average()\`, \`.Any(cond)\`, \`.All(cond)\`.
- \`.ToList()\` / \`.ToArray()\` convierten el resultado en lista o array.
- \`.Reverse()\` y \`.Distinct()\` hacen lo que su nombre indica.

La parte \`x => ...\` es una **lambda**: una función diminuta cuyo parámetro es \`x\`. Las llamadas se encadenan de izquierda a derecha, así que se leen como una tubería. Los bucles hacen el mismo trabajo; LINQ solo es más corto cuando te acostumbras.`,
    },
    examples: [
      {
        id: 'range-select',
        title: { en: 'Range + Select (FizzBuzz in one line)', es: 'Range + Select (FizzBuzz en una línea)' },
        code: `int n = 15;

List<string> result = Enumerable.Range(1, n)
    .Select(i => i % 15 == 0 ? "FizzBuzz" : i % 3 == 0 ? "Fizz" : i % 5 == 0 ? "Buzz" : i.ToString())
    .ToList();

Console.WriteLine(string.Join(" ", result));
`,
      },
      {
        id: 'where',
        title: { en: 'Where, Sum and Any', es: 'Where, Sum y Any' },
        code: `int[] nums = { 4, 8, 15, 16, 23, 42 };

var evens = nums.Where(x => x % 2 == 0).ToArray();
Console.WriteLine(string.Join(", ", evens));
Console.WriteLine(nums.Sum());
Console.WriteLine(nums.Where(x => x > 10).Count());
Console.WriteLine(nums.Any(x => x == 23));
Console.WriteLine(nums.All(x => x > 0));
`,
      },
      {
        id: 'strings',
        title: { en: 'LINQ on strings', es: 'LINQ sobre cadenas' },
        code: `string s = "hello world";

Console.WriteLine(new string(s.Reverse().ToArray()));
Console.WriteLine(s.Count(c => c == 'l'));
Console.WriteLine(new string(s.Distinct().ToArray()));
Console.WriteLine(string.Join("", s.Where(c => c != ' ').Select(c => char.ToUpper(c))));
`,
      },
    ],
  },
  // ------------------------------------------------------------------------------------------------
  {
    slug: 'math',
    title: { en: 'Math and number formatting', es: 'Math y formato de números' },
    summary: {
      en: 'Math.Max, Math.Min, Math.Abs, Math.Round and Math.Pow, plus percentages and rounding with doubles.',
      es: 'Math.Max, Math.Min, Math.Abs, Math.Round y Math.Pow, además de porcentajes y redondeo con doubles.',
    },
    body: {
      en: `The static \`Math\` class has the usual helpers:

- \`Math.Max(a, b)\`, \`Math.Min(a, b)\`: larger / smaller of two values (nest them for three).
- \`Math.Abs(x)\`: distance from zero. \`Math.Sqrt(x)\`, \`Math.Pow(x, y)\`.
- \`Math.Round(x, 2)\`: round to 2 decimals. \`Math.Floor\` / \`Math.Ceiling\` round down / up.

**Percentages**: a 30% discount on \`price\` is \`price * 0.7\`, or \`price - price * 30 / 100.0\`. Keep one operand a \`double\` so the division keeps decimals.

Doubles are stored in binary, so some results come out like \`0.30000000000000004\`. The judge compares numbers with a tiny tolerance, so that is fine for the problems; when printing for people use \`Math.Round\` or a format: \`price.ToString("0.00")\` or \`$"{price:F2}"\`.`,
      es: `La clase estática \`Math\` tiene las ayudas habituales:

- \`Math.Max(a, b)\`, \`Math.Min(a, b)\`: el mayor / el menor de dos valores (anídalos para tres).
- \`Math.Abs(x)\`: distancia respecto a cero. \`Math.Sqrt(x)\`, \`Math.Pow(x, y)\`.
- \`Math.Round(x, 2)\`: redondea a 2 decimales. \`Math.Floor\` / \`Math.Ceiling\` redondean hacia abajo / arriba.

**Porcentajes**: un 30% de descuento sobre \`price\` es \`price * 0.7\`, o \`price - price * 30 / 100.0\`. Mantén un operando como \`double\` para que la división conserve decimales.

Los doubles se guardan en binario, así que algunos resultados salen como \`0.30000000000000004\`. El juez compara los números con una tolerancia diminuta, así que no afecta a los problemas; para mostrarlos a personas usa \`Math.Round\` o un formato: \`price.ToString("0.00")\` o \`$"{price:F2}"\`.`,
    },
    examples: [
      {
        id: 'max-min',
        title: { en: 'Max, Min, Abs', es: 'Max, Min, Abs' },
        code: `int a = 4, b = 9, c = 2;

Console.WriteLine(Math.Max(a, b));              // 9
Console.WriteLine(Math.Max(Math.Max(a, b), c)); // 9  (largest of three)
Console.WriteLine(Math.Min(a, c));              // 2
Console.WriteLine(Math.Abs(-7.5));              // 7.5
Console.WriteLine(Math.Pow(2, 10));             // 1024
Console.WriteLine(Math.Sqrt(81));               // 9
`,
      },
      {
        id: 'percent',
        title: { en: 'Discounts and rounding', es: 'Descuentos y redondeo' },
        code: `double price = 10;

double half = price * 0.5;       // 50% discount
double senior = price * 0.7;     // 30% discount
Console.WriteLine(half);
Console.WriteLine(senior);

double messy = 0.1 + 0.2;
Console.WriteLine(messy);                  // 0.30000000000000004
Console.WriteLine(Math.Round(messy, 2));   // 0.3
Console.WriteLine($"{messy:F2} €");        // 0.30 €
Console.WriteLine(Math.Floor(7.9));        // 7
Console.WriteLine(Math.Ceiling(7.1));      // 8
`,
      },
    ],
  },
]

export const topicBySlug: Record<string, DocTopic> = Object.fromEntries(topics.map((t) => [t.slug, t]))
