using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputMaster.Hooks
{
  internal class ComboRelay : IComboHook
  {
    /// <summary>
    /// All possible target hooks. Each time an event needs to be handled the first active hook in this list is chosen to handle the event.
    /// </summary>
    private readonly List<IComboHook> TargetHooks;
    private IComboHook CurrentHook;

    public bool Active => true;

    public ComboRelay(params IComboHook[] targetHooks)
    {
      TargetHooks = targetHooks.ToList();
    }

    public void Handle(ComboArgs e)
    {
      var newHook = TargetHooks.FirstOrDefault(z => z.Active);
      if (newHook != CurrentHook)
      {
        if (CurrentHook != null && CurrentHook.Active)
        {
          CurrentHook.Reset();
        }
        CurrentHook = newHook;
      }
      CurrentHook?.Handle(e);
    }

    public void Reset()
    {
      foreach (var hook in TargetHooks)
      {
        hook.Reset();
      }
    }

    public string GetTestStateInfo()
    {
      var sb = new StringBuilder();
      foreach (var hook in TargetHooks)
      {
        sb.Append(hook.GetTestStateInfo());
      }
      return sb.ToString();
    }
  }
}
