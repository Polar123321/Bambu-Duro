namespace ConsoleApp4;

internal static class Program
{
    private static readonly object _logLock = new();

#if NET8_0_WINDOWS
    [STAThread]
    public static void Main(string[] args)
    {
        // Garantir algum feedback quando rodar como app Windows (antes era WinExe, sem console).
        Console.Title = "ConsoleApp4";
        Console.WriteLine("Iniciando (net8.0-windows)...");

        ConsoleApp4.Properties.ApplicationConfiguration.Initialize();
        System.Windows.Forms.Application.SetUnhandledExceptionMode(System.Windows.Forms.UnhandledExceptionMode.CatchException);

        System.Windows.Forms.Application.ThreadException += (_, e) =>
        {
            LogUnhandled("UI thread", e.Exception);
            Console.Error.WriteLine($"[UI thread] {e.Exception}");
            System.Windows.Forms.MessageBox.Show(
                $"Erro inesperado (UI): {e.Exception.Message}",
                "Erro",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogUnhandled("AppDomain", ex);
                Console.Error.WriteLine($"[AppDomain] {ex}");
            }
            else
            {
                var ex = new Exception("Unknown unhandled exception.");
                LogUnhandled("AppDomain", ex);
                Console.Error.WriteLine($"[AppDomain] {ex}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUnhandled("TaskScheduler", e.Exception);
            Console.Error.WriteLine($"[TaskScheduler] {e.Exception}");
            e.SetObserved();
        };

        System.Windows.Forms.Application.Run(new MainWindow(args));
    }
#else
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogUnhandled("AppDomain", ex);
            }
            else
            {
                LogUnhandled("AppDomain", new Exception("Unknown unhandled exception."));
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUnhandled("TaskScheduler", e.Exception);
            e.SetObserved();
        };

        using var host = BotHost.Build(args);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await BotHost.StartAsync(host, cts.Token);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C
        }

        await BotHost.StopAsync(host);
    }
#endif

    private static void LogUnhandled(string source, Exception ex)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {ex}\n";
            lock (_logLock)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // Swallow logging failures to avoid secondary crashes.
        }
    }
}
