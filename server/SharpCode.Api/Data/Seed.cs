namespace SharpCode.Api.Data;

public static class Seed
{
    public static void Apply(AppDbContext db)
    {
        if (db.Problems.Any()) return;
        db.Problems.AddRange(
            new Problem
            {
                Slug = "two-sum", Title = "Two Sum", Difficulty = Difficulty.Easy, MethodName = "TwoSum",
                Description = """
                    Given an array of integers `nums` and an integer `target`, return the **indices** of the two numbers such that they add up to `target`.

                    You may assume that each input has exactly one solution, and you may not use the same element twice. Return the answer with the smaller index first.

                    **Example**

                    ```
                    Input:  nums = [2, 7, 11, 15], target = 9
                    Output: [0, 1]
                    ```
                    """,
                StarterCode = """
                    public class Solution
                    {
                        public int[] TwoSum(int[] nums, int target)
                        {
                            // your code here
                            return new int[0];
                        }
                    }
                    """,
                TestCases =
                [
                    new() { Input = "[[2,7,11,15], 9]", Expected = "[0,1]", Order = 0 },
                    new() { Input = "[[3,2,4], 6]", Expected = "[1,2]", Order = 1 },
                    new() { Input = "[[3,3], 6]", Expected = "[0,1]", Order = 2 },
                    new() { Input = "[[-1,-2,-3,-4,-5], -8]", Expected = "[2,4]", Order = 3, IsHidden = true },
                ]
            },
            new Problem
            {
                Slug = "reverse-string", Title = "Reverse String", Difficulty = Difficulty.Easy, MethodName = "Reverse",
                Description = """
                    Write a function that reverses a string.

                    **Example**

                    ```
                    Input:  "hello"
                    Output: "olleh"
                    ```
                    """,
                StarterCode = """
                    public class Solution
                    {
                        public string Reverse(string s)
                        {
                            return s;
                        }
                    }
                    """,
                TestCases =
                [
                    new() { Input = "[\"hello\"]", Expected = "\"olleh\"", Order = 0 },
                    new() { Input = "[\"\"]", Expected = "\"\"", Order = 1 },
                    new() { Input = "[\"a b c\"]", Expected = "\"c b a\"", Order = 2, IsHidden = true },
                ]
            },
            new Problem
            {
                Slug = "fizz-buzz", Title = "Fizz Buzz", Difficulty = Difficulty.Easy, MethodName = "FizzBuzz",
                Description = """
                    Given an integer `n`, return a list of strings where for each `i` from 1 to `n`:

                    - `"FizzBuzz"` if `i` is divisible by 3 and 5
                    - `"Fizz"` if `i` is divisible by 3
                    - `"Buzz"` if `i` is divisible by 5
                    - `i` as a string otherwise
                    """,
                StarterCode = """
                    using System.Collections.Generic;

                    public class Solution
                    {
                        public List<string> FizzBuzz(int n)
                        {
                            var result = new List<string>();
                            return result;
                        }
                    }
                    """,
                TestCases =
                [
                    new() { Input = "[5]", Expected = "[\"1\",\"2\",\"Fizz\",\"4\",\"Buzz\"]", Order = 0 },
                    new() { Input = "[15]", Expected = "[\"1\",\"2\",\"Fizz\",\"4\",\"Buzz\",\"Fizz\",\"7\",\"8\",\"Fizz\",\"Buzz\",\"11\",\"Fizz\",\"13\",\"14\",\"FizzBuzz\"]", Order = 1 },
                    new() { Input = "[1]", Expected = "[\"1\"]", Order = 2, IsHidden = true },
                ]
            },
            new Problem
            {
                Slug = "longest-substring-without-repeating", Title = "Longest Substring Without Repeating Characters",
                Difficulty = Difficulty.Medium, MethodName = "LengthOfLongestSubstring",
                Description = """
                    Given a string `s`, find the length of the **longest substring** without repeating characters.

                    **Example**

                    ```
                    Input:  "abcabcbb"
                    Output: 3   // "abc"
                    ```
                    """,
                StarterCode = """
                    public class Solution
                    {
                        public int LengthOfLongestSubstring(string s)
                        {
                            return 0;
                        }
                    }
                    """,
                TestCases =
                [
                    new() { Input = "[\"abcabcbb\"]", Expected = "3", Order = 0 },
                    new() { Input = "[\"bbbbb\"]", Expected = "1", Order = 1 },
                    new() { Input = "[\"pwwkew\"]", Expected = "3", Order = 2 },
                    new() { Input = "[\"\"]", Expected = "0", Order = 3, IsHidden = true },
                    new() { Input = "[\"dvdf\"]", Expected = "3", Order = 4, IsHidden = true },
                ]
            });
        db.SaveChanges();
    }
}
