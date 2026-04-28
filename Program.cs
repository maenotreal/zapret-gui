using System.Security.Principal;

namespace ZapretGUI;

static class Program
{
    [STAThread]
    static void Main()
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                "Приложение требует прав администратора.\nПожалуйста, запустите от имени администратора.",
                "Требуются права администратора",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
