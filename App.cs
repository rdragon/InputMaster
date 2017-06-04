using System;
using System.Windows.Forms;

namespace InputMaster
{
  internal class App : IApp
  {
    public App()
    {
      Application.ApplicationExit += OnExit;
    }

    public event Action Exiting = delegate { };

    private void OnExit(object sender, EventArgs e)
    {
      Exiting();
      Application.ApplicationExit -= OnExit;
    }
  }
}
