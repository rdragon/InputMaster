using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace InputMaster.Forms
{
  /// <summary>
  /// Adds Ctrl + Tab support.
  /// Also does not capture Ctrl + PgUp and Ctrl + PgDn.
  /// </summary>
  class TabControlPlus : TabControl
  {
    private LinkedList<TabPage> TabOrder = new LinkedList<TabPage>();

    public TabControlPlus()
    {
      ControlAdded += (s, e) =>
      {
        if (e.Control is TabPage)
        {
          TabOrder.AddFirst(e.Control as TabPage);
        }
      };

      ControlRemoved += (s, e) =>
      {
        if (e.Control is TabPage)
        {
          var tabPage = e.Control as TabPage;
          TabOrder.Remove(tabPage);
          if (tabPage == SelectedTab && TabOrder.Any())
          {
            SelectedTab = TabOrder.First.Value;
          }
        }
      };

      Selected += (s, e) =>
      {
        if (e.TabPage != null)
        {
          TabOrder.Remove(e.TabPage);
          TabOrder.AddFirst(e.TabPage);
        }
      };
    }

    public TabPage GetLastTabPageInOrder()
    {
      return TabOrder.Count == 0 ? null : TabOrder.Last.Value;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys key)
    {
      if (key == (Keys.Tab | Keys.Control) && TabOrder.Count > 1)
      {
        SelectedTab = TabOrder.First.Next.Value;
        return true;
      }
      else
      {
        return base.ProcessCmdKey(ref msg, key);
      }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      // We do not want to capture these two hotkeys.
      if (e.KeyData != (Keys.Control | Keys.PageUp) && e.KeyData != (Keys.Control | Keys.PageDown))
      {
        base.OnKeyDown(e);
      }
    }
  }
}
