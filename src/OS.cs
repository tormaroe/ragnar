namespace Ragnar;

public class OS
{
    private static List<string> ParseCommandLine(string commandLine)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }
        return args;
    }

    public static void AddOsFunctions(Context ctx)
    {
        ctx.Set("call", new Native((args, refinements, context, interpreter, isTail) =>
        {
            List<string> cmdArgs = new List<string>();
            string? execPath = null;
            bool useShell = refinements.Contains("shell");

            if (args[0] is Block b)
            {
                if (b.Children.Count == 0) throw new Exception("call expects a non-empty block.");
                execPath = b.Children[0].ToUserString();
                for (int i = 1; i < b.Children.Count; i++)
                {
                    cmdArgs.Add(b.Children[i].ToUserString());
                }
            }
            else
            {
                string rawCmd = args[0].ToUserString();
                if (useShell)
                {
                    bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                    execPath = isWindows ? "cmd.exe" : "/bin/sh";
                    cmdArgs.Add(isWindows ? "/c" : "-c");
                    cmdArgs.Add(rawCmd);
                }
                else
                {
                    var parsed = ParseCommandLine(rawCmd);
                    if (parsed.Count == 0) throw new Exception("call command is empty.");
                    execPath = parsed[0];
                    cmdArgs.AddRange(parsed.Skip(1));
                }
            }

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = execPath,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var a in cmdArgs)
            {
                processInfo.ArgumentList.Add(a);
            }

            bool wait = refinements.Contains("wait") || refinements.Contains("output");
            bool captureOutput = refinements.Contains("output");

            if (captureOutput)
            {
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
            }
            else
            {
                processInfo.RedirectStandardOutput = false;
                processInfo.RedirectStandardError = false;
            }

            var process = System.Diagnostics.Process.Start(processInfo);
            if (process == null) throw new Exception($"Failed to start process: {execPath}");

            if (wait)
            {
                process.WaitForExit();
                if (captureOutput)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        return new Text(output + "\n" + error);
                    }
                    return new Text(output);
                }
                return new Integer(process.ExitCode);
            }

            return new Word("none");
        }, 1).WithTitle("Executes an external shell command."));

        ctx.Set("home", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new File(homeDir);
        }, 0).WithTitle("Returns the current user's home directory."));
    }
}