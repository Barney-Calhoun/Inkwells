using static Globals.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Globals
{
    public static class ConsoleMethods
    {
        public const string ErrorMessage = "fuck off god damn incel piece of shit";
        public const string SuccessMessage = "i think im done";

        public const string TrueAction = "Y";
        public const string FalseAction = "N";
        public static readonly Dictionary<string, bool> ConfirmationActions = new Dictionary<string, bool>()
        {
            { TrueAction, true },
            { FalseAction, false }
        };

        private static string ToQuestionString<T>(
            this IEnumerable<T> choices,
            string question,
            string defaultChoice = null)
        {
            return string.Format(
                "{0} {1}{2}{3}",
                char.IsPunctuation(question[^1]) ? question[..^1] : question,
                choices.ToString("|", "(", ")"),
                string.IsNullOrEmpty(defaultChoice) ? string.Empty : $" [{defaultChoice}]",
                char.IsPunctuation(question[^1]) ? question[^1].ToString() : string.Empty);
        }

        private static void WriteInputError<T>(this IEnumerable<T> choices)
        {
            Console.WriteLine(ErrorMessage);
            Console.WriteLine($"Select {choices.ToString("', '", "'", "'")}.");
        }

        public static int ReadNumericalInput(string question = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(question);
            }

            int input;

            while (!int.TryParse(Console.ReadLine().Trim(), out input))
            {
                Console.WriteLine(ErrorMessage);
                Console.WriteLine("Input must be an integer.");
            }

            return input;
        }

        public static int ReadNumericalInput(
            IEnumerable<int> choices,
            string question = null,
            int? defaultChoice = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(choices.ToQuestionString(question, defaultChoice?.ToString()));
            }

            while (true)
            {
                string input = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(input) && defaultChoice != null)
                {
                    return defaultChoice.Value;
                }
                if (int.TryParse(input, out int number) && choices.Contains(number))
                {
                    return number;
                }

                choices.WriteInputError();
            }
        }

        public static string ReadStringInput(string question = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(question);
            }

            return Console.ReadLine().Trim();
        }

        public static string ReadStringInput(
            IEnumerable<string> choices,
            bool caseSensitive = false,
            string question = null,
            string defaultChoice = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(choices.ToQuestionString(question, defaultChoice));
            }

            if (caseSensitive)
            {
                while (true)
                {
                    string input = Console.ReadLine().Trim();
                    if (string.IsNullOrEmpty(input) && defaultChoice != null)
                    {
                        return defaultChoice;
                    }
                    if (choices.Contains(input))
                    {
                        return input;
                    }

                    choices.WriteInputError();
                }
            }
            else
            {
                var caseInsensitiveChoices = choices
                    .Select(c => new KeyValuePair<string, string>(c.ToLower(), c))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                while (true)
                {
                    string input = Console.ReadLine().Trim().ToLower();
                    if (string.IsNullOrEmpty(input) && defaultChoice != null)
                    {
                        return defaultChoice;
                    }
                    if (caseInsensitiveChoices.ContainsKey(input))
                    {
                        return caseInsensitiveChoices[input];
                    }

                    choices.WriteInputError();
                }
            }
        }

        public static string ReadStringInput(string pattern, string errorMessage, string question = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(question);
            }

            string input;

            while (!Regex.IsMatch(input = Console.ReadLine(), pattern))
            {
                Console.WriteLine(ErrorMessage);
                Console.WriteLine(errorMessage);
            }

            return input;
        }

        public static string ReadRegexPatternInput()
        {
            Console.WriteLine("Enter a regular expression:");

            string regexPattern;

            while (true)
            {
                try
                {
                    new Regex(regexPattern = Console.ReadLine().Trim());
                }
                catch (Exception e)
                {
                    Console.WriteLine(ErrorMessage);
                    Console.WriteLine(e.Message);
                    continue;
                }
                break;
            }

            return regexPattern;
        }

        public static string ReadDomain()
        {
            Console.WriteLine($"Enter domain [{DefaultDomain}]:");

            var domain = ReadStringInput(
                @"^\s*[A-Za-z]*\s*$",
                $"Wrong input, examples: {Domains.ToString("', '", "'", "'")}.");

            if (string.IsNullOrEmpty(domain = domain.Trim().ToLower()))
            {
                return DefaultDomain;
            }

            return domain;
        }

        public static int ReadAction(IDictionary<int, string> actions, int? defaultAction = null)
        {
            Console.WriteLine(
                actions.Keys.ToQuestionString(
                    "Select action:",
                    defaultAction?.ToString()));

            foreach (var kvp in actions)
            {
                var actionKey = kvp.Key;
                var actionName = kvp.Value;
                Console.WriteLine($"{actionKey}. {actionName}");
            }

            return ReadNumericalInput(actions.Keys, null, defaultAction);
        }

        public static string ReadPassword()
        {
            Console.WriteLine("Enter user password:");

            var password = string.Empty;

            var info = Console.ReadKey(true);

            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (!string.IsNullOrEmpty(password))
                {
                    password = password[..^1];

                    var pos = Console.CursorLeft;
                    Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    Console.Write(" ");
                    Console.SetCursorPosition(pos - 1, Console.CursorTop);
                }

                info = Console.ReadKey(true);
            }

            Console.WriteLine();

            return password;
        }

        public static bool ReadConfirmation(string question = null, string defaultChoice = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(
                    ConfirmationActions.Keys.ToQuestionString(
                        question,
                        defaultChoice));
            }

            return ConfirmationActions[
                ReadStringInput(
                    ConfirmationActions.Keys,
                    false,
                    null,
                    defaultChoice)];
        }

        public static void WaitForExit()
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
