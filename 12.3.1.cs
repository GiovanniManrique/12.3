using System;

namespace Assignment12_3_1;

// Question 1: LeetCode #1047, Remove All Adjacent Duplicates in String.
public static class Solution
{
    public static string RemoveDuplicates(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        char[] stack = new char[s.Length];
        int count = 0;

        foreach (char letter in s)
        {
            if (count > 0 && stack[count - 1] == letter)
                count--; // Remove the previous matching letter and skip this one.
            else
                stack[count++] = letter;
        }

        return new string(stack, 0, count);
    }
}

#if !ASSIGNMENT_CHECKS
internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--examples")
        {
            foreach (string sample in new[] { "abbaca", "azxxzy" })
                Console.WriteLine($"{sample} -> {Solution.RemoveDuplicates(sample)}");
            return;
        }

        Console.WriteLine("Assignment 12.3.1 - Remove adjacent duplicates");
        if (args.Length == 0)
            Console.Write("Enter a string of lowercase English letters: ");
        string? input = args.Length > 0 ? args[0] : Console.ReadLine();
        if (input is null)
            return;
        foreach (char letter in input)
        {
            if (letter < 'a' || letter > 'z')
            {
                Console.Error.WriteLine("Please use only lowercase English letters (a-z).");
                Environment.ExitCode = 1;
                return;
            }
        }

        Console.WriteLine($"Result: \"{Solution.RemoveDuplicates(input)}\"");
    }
}
#endif
