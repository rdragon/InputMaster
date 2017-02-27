using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputMaster.Hooks
{
  class ComboRelay : IComboHook
  {
    /// <summary>
    /// All possible target hooks. Each time an event needs to be handled the first active hook in this list is chosen to handle the event.
    /// </summary>
    private List<IComboHook> TargetHooks;
    private IComboHook CurrentHook;

    public Combo CurrentCombo { get; private set; }
    public bool Active { get { return true; } }

    public ComboRelay(params IComboHook[] targetHooks)
    {
      TargetHooks = targetHooks.ToList();
    }

    public void Handle(ComboArgs e)
    {
      CurrentCombo = e.Combo;
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
      StringBuilder sb = new StringBuilder();
      foreach (var hook in TargetHooks)
      {
        sb.Append(hook.GetTestStateInfo());
      }
      return sb.ToString();
    }
  }
}
