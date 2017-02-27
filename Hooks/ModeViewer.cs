using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputMaster.Hooks
{
  class ModeViewer
  {
    private bool Visible;
    private string Text;

    public void Hide()
    {
      if (Visible)
      {
        Text = "";
        Env.Notifier.SetPersistentText("");
        Visible = false;
      }
    }

    public void ToggleVisibility(string text)
    {
      if (Visible)
      {
        Hide();
      }
      else
      {
        Visible = true;
        UpdateText(text);
      }
    }

    public void UpdateText(string text)
    {
      if (Visible)
      {
        if (text != Text)
        {
          Text = Helper.ForbidNull(text, nameof(text));
          Env.Notifier.SetPersistentText(Text);
        }
      }
    }
  }
}
