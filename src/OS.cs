namespace Ragnar;

public class OS
{
    public static void AddOsFunctions(Context ctx)
    {
        ctx.Set("call", new Native((args, refinements, context, interpreter, isTail) => {
            if (args[0] is Text cmd)
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe", // Or /bin/bash on Linux/macOS
                    Arguments = $"/c {cmd.Content}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(processInfo);
                
                // Handle the /wait refinement
                if (refinements.Contains("wait"))
                {
                    process?.WaitForExit();
                    return new Integer(process?.ExitCode ?? 0);
                }

                return new Word("none");
            }
            throw new Exception("call requires a string command.");
        }, 1).WithTitle("Executes an external shell command."));
    }
}