namespace MCaddy.Util;

public class Logger(string name)
{
    public enum LogLevel
    {
        Error,
        Warning,
        Info,
        Debug
    }

    private const string ColorReset = "\e[0m";
    private const string Bold = "\e[39;49;1m";
    private static readonly string LongestLevelName = Enum.GetNames<LogLevel>().MaxBy(x => x.Length)!;

    private static readonly Dictionary<LogLevel, string> ColorMap = new()
    {
        [LogLevel.Error] = "\e[31m",
        [LogLevel.Warning] = "\e[33m",
        [LogLevel.Info] = "\e[34;49m",
        [LogLevel.Debug] = "\e[37;40m"
    };

    public LogLevel Level = LogLevel.Debug;

    public string Name = name;

    public void Log(string text, LogLevel level = LogLevel.Info)
    {
        if (Level < level) return;

        var plainPrefixString = $"{Enum.GetName(level)!.ToUpper().PadLeft(LongestLevelName.Length)} {Name}: ";
        var prefixString =
            $"{Bold}{ColorMap[level]}{Enum.GetName(level)!.ToUpper().PadLeft(LongestLevelName.Length)}{ColorReset} {Name}{ColorReset}: ";
        var newlinePadding = string.Concat(Enumerable.Repeat(" ", plainPrefixString.Length));
        var formattedText = text.ReplaceLineEndings($"\n{newlinePadding}");

        Console.WriteLine($"{prefixString}{formattedText}");
    }
}