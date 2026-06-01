using System;
using System.Collections.Generic;

namespace Ragnar;

public enum EvaluationType
{
    File,
    Expression
}

public record EvaluationTarget(EvaluationType Type, string Value);

public class CommandLineOptions
{
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool NoBanner { get; set; }
    public bool NoConfig { get; set; }
    public bool NoRepl { get; set; }
    public bool ScriptMode { get; set; }
    public string? ScriptPath { get; set; }
    public List<string> ScriptArgs { get; } = new();
    public List<EvaluationTarget> Targets { get; } = new();
    public List<string> Errors { get; } = new();

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();
        int i = 0;
        while (i < args.Length)
        {
            string arg = args[i];
            
            if (arg.StartsWith('-') && arg != "-" && arg != "--")
            {
                if (arg == "-h" || arg == "--help")
                {
                    options.ShowHelp = true;
                    i++;
                }
                else if (arg == "-v" || arg == "--version")
                {
                    options.ShowVersion = true;
                    i++;
                }
                else if (arg == "--no-banner")
                {
                    options.NoBanner = true;
                    i++;
                }
                else if (arg == "--no-config")
                {
                    options.NoConfig = true;
                    i++;
                }
                else if (arg == "--no-repl")
                {
                    options.NoRepl = true;
                    i++;
                }
                else if (arg == "-f" || arg == "--file")
                {
                    if (i + 1 < args.Length)
                    {
                        options.Targets.Add(new EvaluationTarget(EvaluationType.File, args[i + 1]));
                        i += 2;
                    }
                    else
                    {
                        options.Errors.Add("Missing value for option -f/--file");
                        i++;
                    }
                }
                else if (arg == "-e" || arg == "--eval")
                {
                    if (i + 1 < args.Length)
                    {
                        options.Targets.Add(new EvaluationTarget(EvaluationType.Expression, args[i + 1]));
                        i += 2;
                    }
                    else
                    {
                        options.Errors.Add("Missing value for option -e/--eval");
                        i++;
                    }
                }
                else
                {
                    options.Errors.Add($"Unrecognized argument: {arg}");
                    i++;
                }
            }
            else
            {
                // First positional or "--" starts script execution mode
                if (arg == "--")
                {
                    i++; // consume "--"
                    if (i < args.Length)
                    {
                        options.ScriptPath = args[i];
                        options.ScriptMode = true;
                        options.NoRepl = true;
                        options.NoBanner = true;
                        i++;
                    }
                }
                else
                {
                    options.ScriptPath = arg;
                    options.ScriptMode = true;
                    options.NoRepl = true;
                    options.NoBanner = true;
                    i++;
                }

                // Consume all remaining parameters as arguments for the script
                while (i < args.Length)
                {
                    options.ScriptArgs.Add(args[i]);
                    i++;
                }
            }
        }
        return options;
    }
}
