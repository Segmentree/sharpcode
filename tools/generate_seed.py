#!/usr/bin/env python3
"""Generates server/SharpCode.Api/Data/seed.json: every built-in problem with 5 visible + 20 hidden test cases.

Expected outputs are computed by reference implementations below, so the cases are correct by construction.
Run:  python3 tools/generate_seed.py
"""
import json, random, pathlib

random.seed(20261104)
VISIBLE, HIDDEN = 5, 20
OUT = pathlib.Path(__file__).resolve().parent.parent / "server" / "SharpCode.Api" / "Data" / "seed.json"


def cases(visible, hidden_gen, fn):
    """visible: list of arg tuples shown to the user; hidden_gen(): yields arg tuples. fn: reference impl."""
    seen = {json.dumps(v) for v in visible}
    hidden = []
    while len(hidden) < HIDDEN:
        args = hidden_gen()
        key = json.dumps(args)
        if key in seen:
            continue
        seen.add(key)
        hidden.append(args)
    assert len(visible) == VISIBLE
    out = []
    for args in visible + hidden:
        out.append({"input": json.dumps(list(args)), "expected": json.dumps(fn(*args)), "isHidden": len(out) >= VISIBLE})
    return out


problems = []


# Documentation topics (client/src/docs/topics.ts slugs) shown on each problem page.
DOCS = {
    "divisible-by-5": ["variables-and-types", "operators", "if-else"],
    "age-discount": ["if-else", "operators"],
    "speed-classification": ["variables-and-types", "if-else", "strings"],
    "largest-of-three": ["if-else", "operators", "math"],
    "basic-calculator": ["variables-and-types", "switch", "nullable-types", "if-else"],
    "leap-year": ["operators", "if-else"],
    "cinema-ticket": ["if-else", "operators", "math"],
    "login-check": ["strings", "operators", "variables-and-types"],
    "grade-classification": ["if-else", "operators", "strings"],
    "shipping-costs": ["if-else", "operators"],
    "two-sum": ["arrays", "loops", "dictionaries"],
    "reverse-string": ["strings", "arrays", "loops"],
    "fizz-buzz": ["loops", "lists", "operators", "strings"],
    "longest-substring-without-repeating": ["strings", "dictionaries", "loops", "math"],
}


def add(slug, category, difficulty, method, params_sig, ret, title_en, desc_en, title_es, desc_es, tests, starter_body="throw new NotImplementedException();", usings=""):
    starter = f"{usings}public class Solution\n{{\n    public {ret} {method}({params_sig})\n    {{\n        {starter_body}\n    }}\n}}\n"
    problems.append({
        "slug": slug, "category": category, "difficulty": difficulty, "methodName": method, "className": "Solution",
        "timeLimitMs": 2000, "title": title_en, "description": desc_en.strip() + "\n", "starterCode": starter,
        "translations": {"es": {"title": title_es, "description": desc_es.strip() + "\n"}},
        "docLinks": DOCS[slug],
        "testCases": tests,
    })


# ----------------------------------------------------------------------------------------------------------------
# SCHOOL (from the "Ejercicios BOOL//IF" sheet)
# ----------------------------------------------------------------------------------------------------------------

def divisible5(n): return n % 5 == 0
add("divisible-by-5", "School", "Easy", "IsDivisibleBy5", "int number", "bool",
    "Divisible by 5",
    """
Given an integer `number`, use the `%` operator to determine whether it is divisible by 5.

Return `true` if it is divisible, `false` otherwise.

**Example**

```
Input:  number = 15
Output: true
```
""",
    "Número divisible entre 5",
    """
Dado un número entero `number`, utiliza el operador `%` para determinar si es divisible entre 5.

Devuelve `true` si es divisible y `false` en caso contrario.

**Ejemplo**

```
Entrada: number = 15
Salida:  true
```
""",
    cases([(15,), (7,), (0,), (-20,), (101,)], lambda: (random.randint(-500, 500),), divisible5),
    starter_body="return false;")


def discount(age): return 50 if age < 12 else (0 if age <= 65 else 30)
add("age-discount", "School", "Easy", "DiscountByAge", "int age", "int",
    "Discount by age",
    """
Given a person's `age`, return the discount percentage they receive:

- Under 12 years old: **50%** discount.
- From 12 to 65 years old (inclusive): **no** discount.
- Over 65 years old: **30%** discount.

Return the percentage as an integer (`50`, `0` or `30`).

**Example**

```
Input:  age = 70
Output: 30
```
""",
    "Descuento según la edad",
    """
Dada la `age` (edad) de una persona, devuelve el porcentaje de descuento que le corresponde:

- Menos de 12 años: **50%** de descuento.
- Entre 12 y 65 años (ambos incluidos): **sin** descuento.
- Más de 65 años: **30%** de descuento.

Devuelve el porcentaje como un entero (`50`, `0` o `30`).

**Ejemplo**

```
Entrada: age = 70
Salida:  30
```
""",
    cases([(8,), (12,), (40,), (65,), (70,)], lambda: (random.randint(0, 100),), discount),
    starter_body="return 0;")


def speed(kmh): return "Velocidad baja" if kmh < 50 else ("Velocidad adecuada" if kmh <= 100 else "Velocidad alta")
add("speed-classification", "School", "Easy", "ClassifySpeed", "double speedKmh", "string",
    "Speed classification",
    """
Given a vehicle's speed `speedKmh` in km/h, return exactly one of these strings:

- Below 50: `"Velocidad baja"`
- From 50 to 100 (inclusive): `"Velocidad adecuada"`
- Above 100: `"Velocidad alta"`

**Example**

```
Input:  speedKmh = 72.5
Output: "Velocidad adecuada"
```
""",
    "Clasificación de velocidad",
    """
Dada la velocidad de un vehículo `speedKmh` en km/h, devuelve exactamente una de estas cadenas:

- Inferior a 50: `"Velocidad baja"`
- Entre 50 y 100 (ambos incluidos): `"Velocidad adecuada"`
- Superior a 100: `"Velocidad alta"`

**Ejemplo**

```
Entrada: speedKmh = 72.5
Salida:  "Velocidad adecuada"
```
""",
    cases([(72.5,), (30,), (120,), (50,), (100,)], lambda: (round(random.uniform(0, 180), 1),), speed),
    starter_body='return "";')


def max3(a, b, c): return max(a, b, c)
add("largest-of-three", "School", "Easy", "LargestOfThree", "int a, int b, int c", "int",
    "Largest of three numbers",
    """
Given three integers `a`, `b` and `c`, use `if` statements to determine which one is the largest and return it.

Keep in mind that some or all of the numbers may be equal; in that case return that value.

**Example**

```
Input:  a = 4, b = 9, c = 2
Output: 9
```
""",
    "Comparar tres números",
    """
Dados tres enteros `a`, `b` y `c`, utiliza estructuras `if` para determinar cuál es el mayor y devuélvelo.

Ten en cuenta que algunos o todos los números pueden ser iguales; en ese caso devuelve ese valor.

**Ejemplo**

```
Entrada: a = 4, b = 9, c = 2
Salida:  9
```
""",
    cases([(4, 9, 2), (7, 7, 7), (-1, -5, -3), (10, 10, 3), (0, 2, 2)],
          lambda: tuple(random.randint(-50, 50) for _ in range(3)), max3),
    starter_body="return 0;")


def calc(a, b, op):
    if op == "+": return a + b
    if op == "-": return a - b
    if op == "*": return a * b
    if op == "/": return None if b == 0 else a / b
    return None
add("basic-calculator", "School", "Easy", "Calculate", "double a, double b, char op", "double?",
    "Basic calculator",
    """
Given two numbers `a` and `b` and a character `op` that is one of `+`, `-`, `*` or `/`, perform the operation and return the result.

If the operation is a division by zero, or `op` is not one of the four operators, return `null` (this is the "error message" case).

**Example**

```
Input:  a = 6, b = 3, op = '/'
Output: 2
```
""",
    "Calculadora básica",
    """
Dados dos números `a` y `b` y un carácter `op` que es uno de `+`, `-`, `*` o `/`, realiza la operación correspondiente y devuelve el resultado.

Si se intenta dividir entre cero, o `op` no es uno de los cuatro operadores, devuelve `null` (es el caso del "mensaje de error").

**Ejemplo**

```
Entrada: a = 6, b = 3, op = '/'
Salida:  2
```
""",
    cases([(6, 3, "/"), (2.5, 4, "*"), (10, 0, "/"), (7, 2, "-"), (1.5, 2.25, "+")],
          lambda: (random.randint(-20, 20), random.choice([0, random.randint(-9, 9)]), random.choice("+-*/%")), calc),
    starter_body="return null;")


def leap(y): return y % 400 == 0 or (y % 4 == 0 and y % 100 != 0)
add("leap-year", "School", "Easy", "IsLeapYear", "int year", "bool",
    "Leap year",
    """
Given a `year`, return `true` if it is a leap year and `false` otherwise.

A year is a leap year if it is divisible by 400, or if it is divisible by 4 but not by 100.

**Example**

```
Input:  year = 2000
Output: true
```
""",
    "Comprobar un año bisiesto",
    """
Dado un `year` (año), devuelve `true` si es bisiesto y `false` en caso contrario.

Un año es bisiesto si es divisible entre 400, o si es divisible entre 4 pero no entre 100.

**Ejemplo**

```
Entrada: year = 2000
Salida:  true
```
""",
    cases([(2000,), (1900,), (2024,), (2023,), (2100,)], lambda: (random.randint(1600, 2400),), leap),
    starter_body="return false;")


def ticket(age, price):
    if age < 5: return 0
    if age <= 17: return price * 0.5
    if age <= 64: return price
    return price * 0.7
add("cinema-ticket", "School", "Easy", "TicketPrice", "int age, double price", "double",
    "Cinema ticket price",
    """
Given a person's `age` and the base ticket `price` (for example 10 €), return the final price:

- Under 5 years old: free (`0`).
- From 5 to 17: **50%** discount.
- From 18 to 64: full price.
- 65 or older: **30%** discount.

**Example**

```
Input:  age = 10, price = 10
Output: 5
```
""",
    "Precio de una entrada de cine",
    """
Dada la `age` (edad) de una persona y el `price` (precio) base de la entrada (por ejemplo 10 €), devuelve el precio final:

- Menores de 5 años: entrada gratuita (`0`).
- Entre 5 y 17 años: **50%** de descuento.
- Entre 18 y 64 años: precio completo.
- 65 años o más: **30%** de descuento.

**Ejemplo**

```
Entrada: age = 10, price = 10
Salida:  5
```
""",
    cases([(10, 10), (3, 10), (30, 10), (65, 10), (17, 8.5)],
          lambda: (random.randint(0, 90), random.choice([10, 12.5, 8, 15, 9.9])), ticket),
    starter_body="return 0;")


def login(stored, entered, active): return stored == entered and active
add("login-check", "School", "Easy", "IsAccessAllowed", "string storedUser, string enteredUser, bool accountActive", "bool",
    "Login check",
    """
Given the stored user name `storedUser`, the name typed by the user `enteredUser`, and a flag `accountActive`, return whether access is allowed.

Access is allowed **only** if both names match exactly (case-sensitive) and the account is active.

**Example**

```
Input:  storedUser = "ana", enteredUser = "ana", accountActive = true
Output: true
```
""",
    "Comprobar el inicio de sesión",
    """
Dado el nombre de usuario almacenado `storedUser`, el nombre introducido por el usuario `enteredUser` y una variable `accountActive` (cuenta activa), devuelve si el acceso está permitido.

El acceso **solamente** está permitido si ambos nombres coinciden exactamente (distingue mayúsculas) y la cuenta está activa.

**Ejemplo**

```
Entrada: storedUser = "ana", enteredUser = "ana", accountActive = true
Salida:  true
```
""",
    cases([("ana", "ana", True), ("ana", "Ana", True), ("ana", "ana", False), ("luis", "lucia", True), ("", "", True)],
          lambda: (random.choice(["ana", "luis", "maria", "jose"]), random.choice(["ana", "luis", "maria", "jose", "Ana", "LUIS", "mario", ""]), random.choice([True, False])),
          login),
    starter_body="return false;")


def grade(n):
    if n < 0 or n > 10: return "Nota no válida"
    if n < 5: return "Insuficiente"
    if n < 6: return "Suficiente"
    if n < 7: return "Bien"
    if n < 9: return "Notable"
    return "Sobresaliente"
add("grade-classification", "School", "Easy", "ClassifyGrade", "double grade", "string",
    "Grade classification",
    """
Given a `grade` between 0 and 10, return the matching qualification, exactly as written:

- From 0 up to (but not including) 5: `"Insuficiente"`
- From 5 up to (but not including) 6: `"Suficiente"`
- From 6 up to (but not including) 7: `"Bien"`
- From 7 up to (but not including) 9: `"Notable"`
- From 9 to 10: `"Sobresaliente"`

If the grade is not between 0 and 10, return `"Nota no válida"`.

**Example**

```
Input:  grade = 8.9
Output: "Notable"
```
""",
    "Clasificación de una nota",
    """
Dada una `grade` (nota) comprendida entre 0 y 10, devuelve la calificación correspondiente, exactamente así:

- De 0 a 4,9: `"Insuficiente"`
- De 5 a 5,9: `"Suficiente"`
- De 6 a 6,9: `"Bien"`
- De 7 a 8,9: `"Notable"`
- De 9 a 10: `"Sobresaliente"`

Si la nota no está entre 0 y 10, devuelve `"Nota no válida"`.

**Ejemplo**

```
Entrada: grade = 8.9
Salida:  "Notable"
```
""",
    cases([(8.9,), (4.9,), (5,), (10,), (11,)], lambda: (round(random.uniform(-1, 11), 1),), grade),
    starter_body='return "";')


def shipping(qty, unit_price):
    total = qty * unit_price
    if total > 100: return round(total, 2)
    if total >= 50: return round(total + 5, 2)
    return round(total + 10, 2)

def gen_shipping():
    """Unit prices have at most 2 decimals; totals that land within a cent of a threshold (but not exactly on it)
    are skipped so floating-point rounding can never flip the shipping tier."""
    while True:
        qty, price = random.randint(1, 12), round(random.uniform(1, 60), 2)
        total = qty * price
        if any(abs(total - edge) < 0.01 and abs(total - edge) > 1e-9 for edge in (50, 100)):
            continue
        return (qty, price)

add("shipping-costs", "School", "Easy", "TotalWithShipping", "int quantity, double amount", "double",
    "Purchase with shipping costs",
    """
Given the `quantity` of products bought and the `amount` (unit price) of each product in euros, calculate the final amount the customer pays, including shipping.

The purchase total is `quantity × amount`, and shipping depends on that total:

- If the purchase is over 100 €, shipping is free.
- If the purchase is from 50 € to 100 € (inclusive), shipping costs 5 €.
- If the purchase is under 50 €, shipping costs 10 €.

**Example**

```
Input:  quantity = 3, amount = 25
Output: 80
```

3 × 25 = 75 €, which is between 50 € and 100 €, so shipping costs 5 € and the customer pays 80 €.
""",
    "Compra con gastos de envío",
    """
Dada la `quantity` (cantidad) de productos comprados y el `amount` (precio unitario) de cada producto en euros, calcula el importe final que debe pagar el cliente, incluyendo los gastos de envío.

El importe de la compra es `quantity × amount`, y los gastos de envío dependen de ese importe:

- Si la compra supera los 100 €, los gastos de envío son gratuitos.
- Si la compra es de 50 € a 100 € (ambos incluidos), los gastos de envío son de 5 €.
- Si la compra es inferior a 50 €, los gastos de envío son de 10 €.

**Ejemplo**

```
Entrada: quantity = 3, amount = 25
Salida:  80
```

3 × 25 = 75 €, que está entre 50 € y 100 €, así que el envío cuesta 5 € y el cliente paga 80 €.
""",
    cases([(3, 25), (1, 20), (10, 15), (2, 25), (4, 25)], gen_shipping, shipping),
    starter_body="return 0;")


# ----------------------------------------------------------------------------------------------------------------
# GYM (the original problems)
# ----------------------------------------------------------------------------------------------------------------

def two_sum(nums, target):
    seen = {}
    for i, n in enumerate(nums):
        if target - n in seen: return [seen[target - n], i]
        seen[n] = i
    raise AssertionError

def gen_two_sum():
    while True:
        n = random.randint(2, 12)
        nums = [random.randint(-30, 30) for _ in range(n)]
        i, j = sorted(random.sample(range(n), 2))
        target = nums[i] + nums[j]
        pairs = [(a, b) for a in range(n) for b in range(a + 1, n) if nums[a] + nums[b] == target]
        if len(pairs) == 1: return (nums, target)

add("two-sum", "Gym", "Easy", "TwoSum", "int[] nums, int target", "int[]",
    "Two Sum",
    """
Given an array of integers `nums` and an integer `target`, return the **indices** of the two numbers such that they add up to `target`.

You may assume that each input has exactly one solution, and you may not use the same element twice. Return the answer with the smaller index first.

**Example**

```
Input:  nums = [2, 7, 11, 15], target = 9
Output: [0, 1]
```
""",
    "Suma de dos",
    """
Dado un array de enteros `nums` y un entero `target`, devuelve los **índices** de los dos números cuya suma es igual a `target`.

Puedes asumir que cada entrada tiene exactamente una solución y que no puedes usar el mismo elemento dos veces. Devuelve primero el índice menor.

**Ejemplo**

```
Entrada: nums = [2, 7, 11, 15], target = 9
Salida:  [0, 1]
```
""",
    cases([([2, 7, 11, 15], 9), ([3, 2, 4], 6), ([3, 3], 6), ([-1, -2, -3, -4, -5], -8), ([1, 5, 9, 14], 23)], gen_two_sum, two_sum),
    starter_body="return new int[0];")


def reverse(s): return s[::-1]
WORDS = ["hello", "racecar", "C# rocks", "a b c", "", "x", "Ñandú", "12345", "level up", "open ai", "abc def ghi"]
add("reverse-string", "Gym", "Easy", "Reverse", "string s", "string",
    "Reverse String",
    """
Write a function that reverses a string.

**Example**

```
Input:  "hello"
Output: "olleh"
```
""",
    "Invertir una cadena",
    """
Escribe una función que invierta una cadena de texto.

**Ejemplo**

```
Entrada: "hello"
Salida:  "olleh"
```
""",
    cases([("hello",), ("",), ("a b c",), ("racecar",), ("C# rocks",)],
          lambda: ("".join(random.choice("abcdefghijklmnopqrstuvwxyz ") for _ in range(random.randint(1, 14))),), reverse),
    starter_body="return s;")


def fizzbuzz(n): return ["FizzBuzz" if i % 15 == 0 else "Fizz" if i % 3 == 0 else "Buzz" if i % 5 == 0 else str(i) for i in range(1, n + 1)]
add("fizz-buzz", "Gym", "Easy", "FizzBuzz", "int n", "List<string>",
    "Fizz Buzz",
    """
Given an integer `n`, return a list of strings where for each `i` from 1 to `n`:

- `"FizzBuzz"` if `i` is divisible by 3 and 5
- `"Fizz"` if `i` is divisible by 3
- `"Buzz"` if `i` is divisible by 5
- `i` as a string otherwise
""",
    "Fizz Buzz",
    """
Dado un entero `n`, devuelve una lista de cadenas donde para cada `i` de 1 a `n`:

- `"FizzBuzz"` si `i` es divisible entre 3 y 5
- `"Fizz"` si `i` es divisible entre 3
- `"Buzz"` si `i` es divisible entre 5
- `i` como cadena en cualquier otro caso
""",
    cases([(5,), (15,), (1,), (3,), (10,)], lambda: (random.randint(0, 60),), fizzbuzz),
    starter_body="var result = new List<string>();\n        return result;")


def longest(s):
    best, start, last = 0, 0, {}
    for i, c in enumerate(s):
        if c in last and last[c] >= start: start = last[c] + 1
        last[c] = i
        best = max(best, i - start + 1)
    return best
add("longest-substring-without-repeating", "Gym", "Medium", "LengthOfLongestSubstring", "string s", "int",
    "Longest Substring Without Repeating Characters",
    """
Given a string `s`, find the length of the **longest substring** without repeating characters.

**Example**

```
Input:  "abcabcbb"
Output: 3   // "abc"
```
""",
    "Subcadena más larga sin caracteres repetidos",
    """
Dada una cadena `s`, encuentra la longitud de la **subcadena más larga** sin caracteres repetidos.

**Ejemplo**

```
Entrada: "abcabcbb"
Salida:  3   // "abc"
```
""",
    cases([("abcabcbb",), ("bbbbb",), ("pwwkew",), ("",), ("dvdf",)],
          lambda: ("".join(random.choice("abcdefg ") for _ in range(random.randint(0, 20))),), longest),
    starter_body="return 0;")

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(problems, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"wrote {OUT} with {len(problems)} problems, {sum(len(p['testCases']) for p in problems)} test cases")
for p in problems:
    v = sum(1 for t in p["testCases"] if not t["isHidden"]); h = sum(1 for t in p["testCases"] if t["isHidden"])
    assert (v, h) == (VISIBLE, HIDDEN), p["slug"]
