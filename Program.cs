using RedragonFizzMacro.Infrastructure.Storage;
using RedragonFizzMacro.UI.Forms;

namespace RedragonFizzMacro;

internal static class Program
{
    private const string AppMutexName = "ReRainbowFizz_SingleInstance_Mutex";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, AppMutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "O ReRainbowFizz já está em execução na barra de tarefas (ícone vermelho do dragão).",
                "ReRainbowFizz",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        bool startMinimized = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
        var config = ConfigService.Load();

        Application.Run(new MainForm(config, startMinimized));
    }
}
