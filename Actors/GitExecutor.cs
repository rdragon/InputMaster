using System;
using System.Threading.Tasks;

namespace InputMaster.Actors
{
  public class GitExecutor
  {
    private readonly SimpleGitExecutor _simpleGitExecutor;

    public GitExecutor(string directory)
    {
      _simpleGitExecutor = new SimpleGitExecutor(directory);
    }

    public async Task<GitStatusFlags> GetStatus()
    {
      var result = await _simpleGitExecutor.Execute("status");
      var flags = GitStatusFlags.None;
      if (result.StandardOutput.Contains("Changes not staged for commit:") || result.StandardOutput.Contains("Untracked files:"))
        flags |= GitStatusFlags.WorkingTreeDirty;
      if (result.StandardOutput.Contains("Changes to be committed:"))
        flags |= GitStatusFlags.IndexDirty;
      if (result.StandardOutput.Contains("Your branch is ahead of "))
        flags |= GitStatusFlags.AwaitingPush;
      if (result.StandardOutput.Contains("Your branch is up-to-date with "))
        flags |= GitStatusFlags.UpToDate;
      return flags;
    }

    public async Task AddAll()
    {
      await _simpleGitExecutor.Execute("add -A");
    }

    public async Task Commit(string message)
    {
      message = message.Replace('\'', '"');
      await _simpleGitExecutor.Execute($"commit -m\"{message}\"");
    }

    public async Task Push()
    {
      await _simpleGitExecutor.Execute("push");
    }
  }
}
