using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NobleTitlesPlus
{
    internal class Log
    {
        private const string BeginMultiLine = @"=======================================================================================================================\";
        private const string EndMultiLine = @"=======================================================================================================================/";

        public readonly string Module;
        public readonly string LogDir;
        public readonly string LogFile;
        public readonly string LogPath;

        protected TextWriter Writer { get; set; }
        protected bool LastWasMultiline { get; set; } = false;

        public void Print(string line)
        {
            if (Writer is null)
                return;

            LastWasMultiline = false;
            Writer.WriteLine(line);
            Writer.Flush();
        }
        public void Print(string line, LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Error:
                    line = $">> [ERROR] {line}";
                    break;
                case LogCategory.Warning:
                    line = $">> [WARNING] {line}";
                    break;
                case LogCategory.Info:
                    line = $">> [INFO] {line}";
                    break;
                case LogCategory.Debug:
                    line = $">> [DEBUG] {line}";
                    break;
                default:
                    break;
            }
            Print(line);
        }
        public void Print(List<string> lines)
        {
            if (Writer is null || lines.Count == 0)
                return;

            if (lines.Count == 1)
            {
                Print(lines[0]);
                return;
            }

            if (!LastWasMultiline)
                Writer.WriteLine(BeginMultiLine);

            LastWasMultiline = true;

            foreach (string line in lines)
                Writer.WriteLine(line);

            Writer.WriteLine(EndMultiLine);
            Writer.Flush();
        }
        public void Print(List<string> lines, LogCategory category)
        {
            string prefix = category switch
            {
                LogCategory.Error => ">> [ERROR] ",
                LogCategory.Warning => ">> [WARNING] ",
                LogCategory.Info => ">> [INFO] ",
                LogCategory.Debug => ">> [DEBUG] ",
                _ => ""
            };
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = prefix + lines[i];
            }
            Print(lines);
        }
        public Log(bool truncate = false, string? logName = null)
        {
            string userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Mount and Blade II Bannerlord");

            Module = GetType().FullName;
            LogDir = Path.Combine(userDir, "Logs");
            LogFile = logName is null ? $"{GetType().Namespace}.log" : $"{GetType().Namespace}.{logName}.log";
            LogPath = Path.Combine(LogDir, LogFile);

            Directory.CreateDirectory(LogDir);
            bool existed = File.Exists(LogPath);

            try
            {
                // Give it a 64KiB buffer so that it will essentially never block on interim WriteLine calls:
                Writer = TextWriter.Synchronized(new StreamWriter(LogPath, !truncate, Encoding.UTF8, 1 << 16));
            }
            catch (Exception e)
            {
                Console.WriteLine($"================================  EXCEPTION  ================================");
                Console.WriteLine($"{Module}: Failed to create StreamWriter!");
                Console.WriteLine($"Path: {LogPath}");
                Console.WriteLine($"Truncate: {truncate}");
                Console.WriteLine($"Preexisting Path: {existed}");
                Console.WriteLine($"Exception Information:");
                Console.WriteLine($"{e}");
                Console.WriteLine($"=============================================================================");
                throw;
            }

            Writer.NewLine = "\n";

            var msg = new List<string>
            {
                $"{Module} created at: {DateTimeOffset.Now:yyyy/MM/dd H:mm zzz}",
            };

            if (existed && !truncate)
            {
                Writer.WriteLine();
                Writer.WriteLine();
                msg.Add("NOTE: Any prior log messages in this file may have no relation to this session.");
            }

            Print(msg);
        }

        ~Log()
        {
            try
            {
                Writer.Dispose();
            }
            catch (Exception)
            {
                // at least we tried.
            }
        }
    }
    public enum LogCategory
    {
        None,
        Debug,
        Info,
        Warning,
        Error
    }
}
