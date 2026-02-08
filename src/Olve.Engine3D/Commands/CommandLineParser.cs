using System.Text;

namespace Olve.Engine3D.Commands;

internal static class CommandLineParser
{
    public static Result<(string verb, Dictionary<string, string> args)> ParseVerbAndArgs(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return new ResultProblem("Command is empty");

        var verbEnd = command.IndexOf(' ');
        if (verbEnd == 0)
            return new ResultProblem("Invalid command");

        string verb;
        string rest;
        if (verbEnd == -1)
        {
            verb = command.Trim();
            rest = string.Empty;
        }
        else
        {
            verb = command[..verbEnd];
            rest = command[(verbEnd + 1)..];
        }

        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int i = 0, len = rest.Length;
        while (i < len)
        {
            while (i < len && char.IsWhiteSpace(rest[i])) i++;
            if (i >= len) break;

            var keyStart = i;
            while (i < len && rest[i] != '=' && !char.IsWhiteSpace(rest[i])) i++;
            var key = rest[keyStart..i];
            if (string.IsNullOrEmpty(key))
                return new ResultProblem($"Invalid argument at position {keyStart}");

            while (i < len && char.IsWhiteSpace(rest[i])) i++;
            if (i >= len || rest[i] != '=')
                return new ResultProblem($"Missing '=' after argument '{key}'");
            i++;

            while (i < len && char.IsWhiteSpace(rest[i])) i++;
            if (i >= len)
                return new ResultProblem($"Missing value for argument '{key}'");

            string value;
            var c0 = rest[i];
            if (c0 == '\'' || c0 == '"' || c0 == '`')
            {
                var quote = c0;
                i++;
                var sb = new StringBuilder();
                while (i < len && rest[i] != quote)
                {
                    if (rest[i] == '\\' && i + 1 < len && rest[i + 1] == quote)
                    {
                        sb.Append(quote);
                        i += 2;
                    }
                    else
                    {
                        sb.Append(rest[i]);
                        i++;
                    }
                }
                if (i >= len)
                    return new ResultProblem($"Unterminated quoted value for '{key}'");
                i++;
                value = sb.ToString();
            }
            else
            {
                var valStart = i;
                while (i < len && !char.IsWhiteSpace(rest[i])) i++;
                value = rest[valStart..i];
            }

            args[key] = value;
        }

        return Result.Success((verb, args));
    }
}
