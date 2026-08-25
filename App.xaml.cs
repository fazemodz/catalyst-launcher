using System.Windows;
using Catalyst_Launcher.Services;

namespace Catalyst_Launcher;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (UserSessionService.GetSession() == null)
        {
            var loginWindow = new LoginWindow();
            bool signedIn = loginWindow.ShowDialog() == true;

            if (!signedIn)
            {
                Shutdown();
                return;
            }

            if (loginWindow.LoggedInUser != null)
            {
                var u = loginWindow.LoggedInUser;
                UserSessionService.SaveSession(new SessionData(u.Username, u.Email, u.Token));
            }
        }

        ShutdownMode = ShutdownMode.OnLastWindowClose;
        new MainWindow().Show();
    }
}
