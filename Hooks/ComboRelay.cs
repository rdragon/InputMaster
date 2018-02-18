using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InputMaster.Hooks
{
  public class ComboRelay : IComboHook
  {
    /// <summary>
    /// All possible target hooks. Each time an event needs to be handled the first active hook in this list is chosen to handle the event.
    /// </summary>
    private readonly List<IComboHook> _targetHooks;
    private IComboHook _currentHook;

    public bool Active => true;

    public ComboRelay(params IComboHook[] targetHooks)
    {
      _targetHooks = targetHooks.ToList();
    }

    public void Handle(ComboArgs e)
    {
      var newHook = _targetHooks.FirstOrDefault(z => z.Active);
      if (newHook != _currentHook)
      {
        if (_currentHook != null && _currentHook.Active)
          _currentHook.Reset();
        _currentHook = newHook;
      }
      _currentHook?.Handle(e);
    }

    public void Reset()
    {
      foreach (var hook in _targetHooks)
        hook.Reset();
    }

    public string GetTestStateInfo()
    {
      var sb = new StringBuilder();
      foreach (var hook in _targetHooks)
        sb.Append(hook.GetTestStateInfo());
      return sb.ToString();
    }
  }
}
