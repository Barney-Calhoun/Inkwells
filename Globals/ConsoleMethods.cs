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
            this IEnumerable<T> selections,
            string question,
            string defaultSelection = null)
        {
            return string.Format(
                "{0} {1}{2}{3}",
                char.IsPunctuation(question[^1]) ? question[..^1] : question,
                selections.ToString("|", "(", ")"),
                string.IsNullOrEmpty(defaultSelection) ? string.Empty : $" [{defaultSelection}]",
                char.IsPunctuation(question[^1]) ? question[^1].ToString() : string.Empty);
        }

        private static void WriteInputError<T>(this IEnumerable<T> selections)
        {
            Console.WriteLine(ErrorMessage);
            Console.WriteLine($"Select {selections.ToString("', '", "'", "'")}.");
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
            IEnumerable<int> selections,
            int? defaultSelection = null,
            string question = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(selections.ToQuestionString(question, defaultSelection?.ToString()));
            }

            while (true)
            {
                string input = Console.ReadLine().Trim();

                if (string.IsNullOrEmpty(input) && defaultSelection != null)
                {
                    return defaultSelection.Value;
                }

                if (int.TryParse(input, out int number) && selections.Contains(number))
                {
                    return number;
                }

                selections.WriteInputError();
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
            IEnumerable<string> selections,
            string defaultSelection = null,
            bool caseSensitive = false,
            string question = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(selections.ToQuestionString(question, defaultSelection));
            }

            if (caseSensitive)
            {
                while (true)
                {
                    string input = Console.ReadLine().Trim();

                    if (string.IsNullOrEmpty(input) && defaultSelection != null)
                    {
                        return defaultSelection;
                    }

                    if (selections.Contains(input))
                    {
                        return input;
                    }

                    selections.WriteInputError();
                }
            }
            else
            {
                var caseInsensitiveSelections = selections
                    .Select(c => new KeyValuePair<string, string>(c.ToLower(), c))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                while (true)
                {
                    string input = Console.ReadLine().Trim().ToLower();

                    if (string.IsNullOrEmpty(input) && defaultSelection != null)
                    {
                        return defaultSelection;
                    }

                    if (caseInsensitiveSelections.ContainsKey(input))
                    {
                        return caseInsensitiveSelections[input];
                    }

                    selections.WriteInputError();
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

        public static int ReadSelection<TValue>(
            IDictionary<int, TValue> selections,
            int? defaultSelection = null,
            string question = "Select action:")
        {
            Console.WriteLine(
                selections.Keys.ToQuestionString(
                    question,
                    defaultSelection?.ToString()));

            var selectionKeyFormat = $"{{0, -{selections.Keys.Max(k => k.ToString().Length + 1)}}}";

            foreach (var kvp in selections)
            {
                var selectionKey = string.Format(selectionKeyFormat, $"{kvp.Key}.");
                var selectionValue = kvp.Value;
                Console.WriteLine($"{selectionKey} {selectionValue}");
            }

            return ReadNumericalInput(selections.Keys, defaultSelection, null);
        }

        public static int ReadSelection<TValue>(
            IDictionary<int, TValue[]> selections,
            int? defaultSelection = null,
            string question = "Select action:")
        {
            Console.WriteLine(
                selections.Keys.ToQuestionString(
                    question,
                    defaultSelection?.ToString()));

            var selectionKeyFormat = $"{{0, -{selections.Keys.Max(k => k.ToString().Length + 1)}}}";

            foreach (var kvp in selections)
            {
                var selectionKey = string.Format(selectionKeyFormat, $"{kvp.Key}.");
                var selectionValue = string.Join(' ', kvp.Value);
                Console.WriteLine($"{selectionKey} {selectionValue}");
            }

            return ReadNumericalInput(selections.Keys, defaultSelection, null);
        }

        public static KeyValuePair<int, string> ReadDomain()
        {
            var domainKey = ReadSelection(Domains, IncelsDomain, "Select domain:");

            if (domainKey == OtherDomain)
            {
                return new KeyValuePair<int, string>(
                    domainKey,
                    ReadStringInput("Enter other domain:").Trim().ToLower());
            }

            return new KeyValuePair<int, string>(domainKey, Domains[domainKey]);
        }

        public static KeyValuePair<int, string[]> ReadDomains()
        {
            var domainKey = ReadSelection(DomainHistory, IncelsDomain, "Select domains:");

            if (domainKey == OtherDomain)
            {
                var otherDomains = ReadStringInput("Enter other domains (separated by whitespace):")
                    .Trim()
                    .ToLower()
                    .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

                return new KeyValuePair<int, string[]>(domainKey, otherDomains);
            }

            return new KeyValuePair<int, string[]>(domainKey, DomainHistory[domainKey]);
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

        public static bool ReadConfirmation(string question = null, string defaultSelection = null)
        {
            if (!string.IsNullOrEmpty(question))
            {
                Console.WriteLine(
                    ConfirmationActions.Keys.ToQuestionString(
                        question,
                        defaultSelection));
            }

            return ConfirmationActions[
                ReadStringInput(
                    ConfirmationActions.Keys,
                    defaultSelection,
                    false,
                    null)];
        }

        public static void WaitForExit()
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
