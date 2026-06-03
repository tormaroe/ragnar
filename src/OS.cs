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

            bool pid = refinements.Contains("pid");
            bool wait = (refinements.Contains("wait") || refinements.Contains("output")) && !pid;
            bool captureOutput = refinements.Contains("output") && !pid;

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

            if (pid)
            {
                return new Integer(process.Id);
            }

            if (wait)
            {
                if (captureOutput)
                {
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    System.Threading.Tasks.Task.WaitAll(outputTask, errorTask);
                    string output = outputTask.Result;
                    string error = errorTask.Result;
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        return new Text(output + "\n" + error);
                    }
                    return new Text(output);
                }
                
                process.WaitForExit();
                return new Integer(process.ExitCode);
            }

            return new Word("none");
        }, 1).WithTitle("Executes an external shell command.").WithRefinements("shell", "wait", "output", "pid"));

        ctx.Set("home", new Native((args, refinements, context, interpreter, isTail) =>
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new File(homeDir);
        }, 0).WithTitle("Returns the current user's home directory."));

        ctx.Set("proc-status", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is not Integer intVal) throw new Exception("proc-status expects an integer pid.");
            int pid = (int)intVal.Number;
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(pid);
                return new Logic(!process.HasExited);
            }
            catch
            {
                return new Logic(false);
            }
        }, 1).WithTitle("Checks if the process with the given PID is currently alive."));

        ctx.Set("proc-kill", new Native((args, refinements, context, interpreter, isTail) =>
        {
            if (args[0] is not Integer intVal) throw new Exception("proc-kill expects an integer pid.");
            int pid = (int)intVal.Number;
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(pid);
                process.Kill(true);
                process.WaitForExit();
                return new Logic(true);
            }
            catch
            {
                return new Logic(false);
            }
        }, 1).WithTitle("Terminates the process with the given PID and all its children."));
    }
}