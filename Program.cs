using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace BitCraftTimer
{
    internal static class Program
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Initialize logging
                File.WriteAllText(LogPath, $"[{DateTime.Now}] Application starting...\n");

                // Set up global exception handlers
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (sender, e) => 
                {
                    LogError($"UI Thread Exception: {e.Exception}");
                    HandleException(e.Exception);
                };
                
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => 
                {
                    if (e.ExceptionObject is Exception ex)
                    {
                        LogError($"Unhandled Exception: {ex}");
                        HandleException(ex);
                    }
                };

                Log("Initializing application configuration...");
                ApplicationConfiguration.Initialize();
                
                Log("Creating main form...");
                var mainForm = new Form1();
                
                Log("Running application...");
                Application.Run(mainForm);
                
                Log("Application exited normally.");
            }
            catch (Exception ex)
            {
                LogError($"Fatal error in Main: {ex}");
                HandleException(ex);
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now}] {message}\n");
                Debug.WriteLine($"[BitCraftTimer] {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BitCraftTimer] Failed to log message: {ex}");
            }
        }

        private static void LogError(string error)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now}] [ERROR] {error}\n");
                Debug.WriteLine($"[BitCraftTimer] [ERROR] {error}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BitCraftTimer] Failed to log error: {ex}");
            }
        }

        private static void HandleException(Exception ex)
        {
            string errorMessage = $"An unhandled exception occurred:\n\n{ex}\n\n{ex.StackTrace}";
            LogError(errorMessage);
            
            try
            {
                // Show error to user
                MessageBox.Show("An error occurred. Please check the log file for details.", 
                    "Application Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
            catch (Exception uiEx)
            {
                LogError($"Failed to show error dialog: {uiEx}");
            }
        }
    }
}
