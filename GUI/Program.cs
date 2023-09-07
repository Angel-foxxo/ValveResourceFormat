using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

namespace GUI
{
    class Program : WindowsFormsApplicationBase
    {
        public static MainForm Instance { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        internal static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }

        public Program()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.ThreadException += OnThreadException;

            // Set invariant culture so we have consistent localization (e.g. dots do not get encoded as commas)
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            IsSingleInstance = true;
            EnableVisualStyles = true;
            Instance = new MainForm();
            MainForm = Instance;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Instance.HandleArgs(e.CommandLine);

            return true;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e)
        {
            //base.OnStartupNextInstance(e);

            Instance.Invoke(() =>
            {
                if (e.BringToForeground)
                {
                    if (Instance.WindowState == FormWindowState.Minimized)
                    {
                        Instance.WindowState = FormWindowState.Normal;
                    }

                    Instance.Activate();
                    Instance.BringToFront();
                }

                Instance.HandleArgs(e.CommandLine);
            });
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowError(e.Exception);
        }

        private static void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs ex)
        {
            ShowError((Exception)ex.ExceptionObject);
        }

        private static void ShowError(Exception exception)
        {
            Console.Error.WriteLine(exception);

            MessageBox.Show(
                $"{exception.Message}{Environment.NewLine}{Environment.NewLine}See console for more information.{Environment.NewLine}{Environment.NewLine}Try using latest unstable build to see if the issue persists.{Environment.NewLine}Source 2 Viewer Version: {Application.ProductVersion[..16]}",
                $"Unhandled exception: {exception.GetType()}",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
