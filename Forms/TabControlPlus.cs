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
    private readonly LinkedList<TabPage> TabOrder = new LinkedList<TabPage>();

    public TabControlPlus()
    {
      ControlAdded += (s, e) =>
      {
        if (!(e.Control is TabPage tabPage))
          return;
        TabOrder.AddFirst(tabPage);
      };

      ControlRemoved += (s, e) =>
      {
        if (!(e.Control is TabPage tabPage))
          return;
        TabOrder.Remove(tabPage);
        if (tabPage == SelectedTab && TabOrder.Any())
          SelectedTab = TabOrder.First.Value;
      };

      Selected += (s, e) =>
      {
        if (e.TabPage == null)
          return;
        TabOrder.Remove(e.TabPage);
        TabOrder.AddFirst(e.TabPage);
      };
    }

    public TabPage GetLastTabPageInOrder()
    {
      return TabOrder.Count == 0 ? null : TabOrder.Last.Value;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys key)
    {
      if (key != (Keys.Tab | Keys.Control) || TabOrder.Count < 2)
        return base.ProcessCmdKey(ref msg, key);
      SelectedTab = TabOrder.First.Next.Value;
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
