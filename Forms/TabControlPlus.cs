using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Adds Ctrl + Tab support.
  /// Also does not capture Ctrl + PgUp and Ctrl + PgDn.
  /// </summary>
  public class TabControlPlus : TabControl
  {
    private readonly LinkedList<TabPage> _tabOrder = new LinkedList<TabPage>();

    public TabControlPlus()
    {
      ControlAdded += (s, e) =>
      {
        if (!(e.Control is TabPage tabPage))
          return;
        _tabOrder.AddFirst(tabPage);
      };

      ControlRemoved += (s, e) =>
      {
        if (!(e.Control is TabPage tabPage))
          return;
        _tabOrder.Remove(tabPage);
        if (tabPage == SelectedTab && _tabOrder.Any())
          SelectedTab = _tabOrder.First.Value;
      };

      Selected += (s, e) =>
      {
        if (e.TabPage == null)
          return;
        _tabOrder.Remove(e.TabPage);
        _tabOrder.AddFirst(e.TabPage);
      };
    }

    public TabPage GetLastTabPageInOrder()
    {
      return _tabOrder.Count == 0 ? null : _tabOrder.Last.Value;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys key)
    {
      if (key != (Keys.Tab | Keys.Control) || _tabOrder.Count < 2)
        return base.ProcessCmdKey(ref msg, key);
      SelectedTab = _tabOrder.First.Next.Value;
      return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      // We do not want to capture these two hotkeys.
      if (e.KeyData != (Keys.Control | Keys.PageUp) && e.KeyData != (Keys.Control | Keys.PageDown))
        base.OnKeyDown(e);
    }
  }
}
