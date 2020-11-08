using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DigitalLifeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // https://stackoverflow.com/questions/97283/how-can-i-determine-the-name-of-the-currently-focused-process-in-c-sharp

        #region Delegate and imports from pinvoke.net

        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
                hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion

        // Constants from WinUser.h
        private const uint EventSystemForeground = 3;
        private const uint WindowsEventOutOfContext = 0;

        // Need to ensure delegate is not collected while we're using it,
        // storing it in a class field is simplest way to do this.
        private static readonly WinEventDelegate TrackerProcessDelegate = WinEventProc;

        static void WinEventProc(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            _logBox.AppendText($"Foreground changed to {hwnd.ToInt32():x8}\n");
            GetForegroundProcessName();
            _logBox.AppendText("\n");

        }
        static void GetForegroundProcessName()
        {
            var windowHandler = GetForegroundWindow();
            GetWindowThreadProcessId(windowHandler, out var pid);

            var foregroundProcess = Process.GetProcesses()
                .FirstOrDefault(p => p.Id == pid);

            if (foregroundProcess is null)
            {
                _logBox.AppendText("Unknown\n");
                return;
            }

            _logBox.AppendText($"Pid is: {pid}\n");
            _logBox.AppendText($"Process name is {foregroundProcess.ProcessName}\n");
            // Use product name to group apps (i.e. Windows Apps, Microsoft Office Apps)
            _logBox.AppendText($"Application product name is {foregroundProcess.MainModule?.FileVersionInfo.ProductName}\n");
            _logBox.AppendText($"Application internal name is {foregroundProcess.MainModule?.FileVersionInfo.InternalName}\n");
            // File Description always contains correct App Name — use to display app name
            _logBox.AppendText($"Application description is {foregroundProcess.MainModule?.FileVersionInfo.FileDescription}\n");
            _logBox.AppendText($"Application caption is {foregroundProcess.MainWindowTitle}\n");
        }

        private IntPtr _eventHookInstance;
        private static TextBox _logBox;

        public MainWindow()
        {
            InitializeComponent();
            _logBox = LogBox;
        }
        private void StartTracking_OnClick(object sender, RoutedEventArgs e)
        {
            _eventHookInstance = SetWinEventHook(EventSystemForeground, EventSystemForeground,
                IntPtr.Zero, TrackerProcessDelegate, 0, 0, WindowsEventOutOfContext);
        }

        private void StopTracking_OnClick(object sender, RoutedEventArgs e)
        {
            UnhookWinEvent(_eventHookInstance);
        }
    }
}
