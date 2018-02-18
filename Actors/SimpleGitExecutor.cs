using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class SimpleGitExecutor
  {
    private readonly string _directory;

    public SimpleGitExecutor(string directory)
    {
      Helper.RequireExistsDir(directory);
      _directory = directory;
    }

    public async Task<GitResponse> Execute(string arguments)
    {
      try
      {
        return await ExecuteInternal(arguments);
      }
      catch (Exception ex)
      {
        throw new WrappedException($"Error while executing 'git {arguments}'.", ex);
      }
    }

    private async Task<GitResponse> ExecuteInternal(string arguments)
    {
      // Thread-safe.
      return await Task.Run(() =>
      {
        var info = new ProcessStartInfo("git", arguments)
        {
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
          WorkingDirectory = _directory
        };
        using (var process = Process.Start(info))
        {
          Helper.ForbidNull(process);
          if (!process.WaitForExit((int)Env.Config.GitTimeout.TotalMilliseconds))
          {
            process.Kill();
            throw new TimeoutException($"Timeout after {(int)Env.Config.GitTimeout.TotalSeconds} seconds.");
          }
          if (process.ExitCode != 0)
            throw new GitException($"Git process returned with code {process.ExitCode}. Standard error:\n" +
              process.StandardError.ReadToEnd());
          return new GitResponse(process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
        }
      });
    }
  }
}
