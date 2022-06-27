using Fastenshtein;
using X39.Util;
using X39.Util.Console;
using QuickTrack.Commands;

namespace QuickTrack;

public record UnmatchedCommand(string Pattern, string Description, Func<string, bool> Action);

public class CommandParser
{
    private readonly Dictionary<string, ICommand> _commandDictionary = new();
    private readonly HashSet<ICommand>            _commands          = new();
    private readonly UnmatchedCommand?            _unmatchedCommand;

    public CommandParser(UnmatchedCommand? unmatchedCommand)
    {
        _unmatchedCommand = unmatchedCommand;
    }


    public void RegisterCommand(ICommand command)
    {
        if (_commands.Contains(command))
            throw new ArgumentException("Command was added already.");
        foreach (var key in command.Keys)
        {
            if (_commandDictionary.ContainsKey(key.ToLower()))
                throw new ArgumentException("A command with the same key exists.");
            if (key.StartsWith("!"))
                throw new ArgumentException("Commands may not start with '!'.");
            if (key.ToLower() is "help" or "?")
                throw new ArgumentException("Reserved command cannot be added.");
        }
        foreach (var key in command.Keys)
        {
            _commandDictionary[key.ToLower()] = command;
        }

        _commands.Add(command);
    }

    public void RegisterCommand(string key, string pattern, string description, Action<string[]> action)
        => RegisterCommand(new FluentCommand(key.MakeArray(), () => pattern, description, action));
    public void RegisterCommand(string[] keys, string pattern, string description, Action<string[]> action)
        => RegisterCommand(new FluentCommand(keys, () => pattern, description, action));
    public void RegisterCommand(string key, Func<string> pattern, string description, Action<string[]> action)
        => RegisterCommand(new FluentCommand(key.MakeArray(), pattern, description, action));
    public void RegisterCommand(string[] keys, Func<string> pattern, string description, Action<string[]> action)
        => RegisterCommand(new FluentCommand(keys, pattern, description, action));
    public void RegisterCommand(string key, string pattern, string description, Action action)
        => RegisterCommand(new FluentCommand(key.MakeArray(), () => pattern, description, (_) => action()));
    public void RegisterCommand(string[] keys, string pattern, string description, Action action)
        => RegisterCommand(new FluentCommand(keys, () => pattern, description, (_) => action()));
    public void RegisterCommand(string key, Func<string> pattern, string description, Action action)
        => RegisterCommand(new FluentCommand(key.MakeArray(), pattern, description, (_) => action()));
    public void RegisterCommand(string[] keys, Func<string> pattern, string description, Action action)
        => RegisterCommand(new FluentCommand(keys, pattern, description, (_) => action()));

    public void PromptCommand(Stack<string> undoQueue, Stack<string> redoQueue, CancellationToken cancellationToken)
    {
        var line = InteractiveConsoleInput.ReadLine(undoQueue, redoQueue, cancellationToken).Trim();
        if (line.IsNullOrWhiteSpace())
        {
            _unmatchedCommand?.Action(string.Empty);
            return;
        }
        var words = SmartSplit(line);
        if (!words.Any())
        {
            _unmatchedCommand?.Action(string.Empty);
            return;
        }

        var bang = false;
        var firstWord = words.First();
        if (firstWord.StartsWith("!"))
        {
            firstWord = firstWord[1..];
            line = line[1..];
            bang = true;
        }
        if (firstWord is "help" or "?")
        {
            DisplayHelp();
            return;
        }
        if (!bang &&_commandDictionary.TryGetValue(firstWord, out var command))
        {
            command.Execute(words.Skip(1).ToArray());
            return;
        }
        if (!bang && CouldBeMistake(firstWord))
            return;

        _unmatchedCommand?.Action(line);
    }

    private bool CouldBeMistake(string firstWord)
    {
        var candidates = _commandDictionary.Keys
            .Select((q) => (q, dst: Levenshtein.Distance(q, firstWord)))
            .Where((q) => q.dst < 3)
            .ToArray();
        if (!candidates.Any())
            return false;
        new ConsoleString("Did you mean:")
                {Foreground = ConsoleColor.DarkYellow, Background = ConsoleColor.Black}
            .WriteLine();
        foreach (var (candidate, _) in candidates.OrderBy((q) => q.dst))
        {
            new ConsoleString($"- {candidate}")
                    {Foreground = ConsoleColor.DarkYellow, Background = ConsoleColor.Black}
                .WriteLine();
        }
        new ConsoleString("use bang (!) to suppress this")
                {Foreground = ConsoleColor.DarkYellow, Background = ConsoleColor.Black}
            .WriteLine();
        return true;
    }

    private void DisplayHelp()
    {
        new ConsoleString("The following commands are available:")
                {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
            .WriteLine();
        foreach (var command in _commands)
        {
            new ConsoleString(string.Concat(
                        "    ",
                        string.Join(
                            "\r\n    ",
                            SplitAfterCharacters(command.Pattern, 80))))
                    {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
                .WriteLine();
            new ConsoleString(string.Concat(
                "        ",
                string.Join(
                    "\r\n        ",
                    SplitAfterCharacters(command.Description, 80))))
                    {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
                .WriteLine();
            Console.WriteLine();
        }
        
        if (_unmatchedCommand is not null)
        {
            new ConsoleString("The following is done when nothing could be matched:")
                    {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
                .WriteLine();
            new ConsoleString(string.Concat(
                        "    ",
                        string.Join(
                            "\r\n    ",
                            SplitAfterCharacters(_unmatchedCommand.Pattern, 80))))
                    {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
                .WriteLine();
            new ConsoleString(string.Concat(
                        "        ",
                        string.Join(
                            "\r\n        ",
                            SplitAfterCharacters(_unmatchedCommand.Description, 80))))
                    {Foreground = ConsoleColor.Green, Background = ConsoleColor.Black}
                .WriteLine();
            Console.WriteLine();
        }
    }

    private IEnumerable<string> SplitAfterCharacters(string input, int count)
    {
        var lastBreak = 0;
        var spacing = 0;
        var start = 0;
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i].IsWhiteSpace())
                lastBreak = i;
            else if (input[i].IsPunctuation())
                lastBreak = i;

            spacing++;
            if (spacing != count)
                continue;
            if (lastBreak == start)
            {
                yield return input[start..i];
                var skip = input.Skip(lastBreak).TakeWhile(char.IsWhiteSpace).Count();
                start = i + skip;
                lastBreak = i + skip;

                spacing = 0;
            }
            else
            {
                yield return input[start..lastBreak];
                var skip = input.Skip(lastBreak).TakeWhile(char.IsWhiteSpace).Count();
                start = lastBreak + skip;
                spacing = (lastBreak - start) + skip;
            }
        }

        yield return input[start..];
    }

    private static string[] SmartSplit(string line)
    {
        static IEnumerable<string> Actual(string line)
        {
            var start = 0;
            var quoted = false;
            var quoteUsed = '\0';
            for (var i = 0; i < line.Length; i++)
            {
                if (quoted)
                {
                    if (line[i] != quoteUsed)
                        continue;
                    quoted = false;
                    yield return line[start..(i - 1)].Replace(new string(quoteUsed, 2), quoteUsed.ToString());
                }
                else
                {
                    if (line[i].IsWhiteSpace())
                    {
                        if (start == i)
                            start++;
                        yield return line[start..i];
                        start = i + 1;
                    }
                    else switch (line[i])
                    {
                        case '\'':
                            quoteUsed = '\'';
                            quoted = true;
                            start = i + 1;
                            break;
                        case '"':
                            quoteUsed = '"';
                            quoted = true;
                            start = i + 1;
                            break;
                    }
                }
            }

            if (quoted)
            {
                yield return line[start..line.Length].Replace(new string(quoteUsed, 2), quoteUsed.ToString());
            }
            else
            {
                yield return line[start..line.Length];
            }
        }

        return Actual(line).Where((q) => !q.IsNullOrWhiteSpace()).ToArray();
    }
}