using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BorderlessWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            int left, top, right, bottom;

            public System.Drawing.Rectangle ToRectangle()
            {
                return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
            }
        }

        List<string> listProcesses = new List<string>();
        List<string> listTriggers = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            lstTriggers.ItemsSource = listTriggers;
            lstProcesses.ItemsSource = listProcesses;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(Properties.Settings.Default.processes))
            {
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(Properties.Settings.Default.processes)))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    listTriggers = formatter.Deserialize(stream) as List<string>;
                }
                lstTriggers.ItemsSource = null;
                lstTriggers.ItemsSource = listTriggers;
            }

            RefreshProcesses();

            Screen[] screens = Screen.AllScreens;
            comboMonitors.Items.Clear();
            for (int i = 1; i < screens.Length + 1; i++)
            {
                comboMonitors.Items.Add("Monitor " + i);
            }
            comboMonitors.SelectedIndex = 0;
        }

        private void RefreshProcesses()
        {
            Process[] pList = Process.GetProcesses();
            listProcesses.Clear();
            foreach (Process process in pList)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle) && !listTriggers.Contains(process.MainWindowTitle))
                {
                    listProcesses.Add(process.MainWindowTitle);
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstTriggers.SelectedItem != null)
            {
                listProcesses.Add(lstTriggers.SelectedItem.ToString());
                listTriggers.Remove(lstTriggers.SelectedItem.ToString());
                lstTriggers.ItemsSource = null;
                lstTriggers.ItemsSource = listTriggers;
                lstProcesses.ItemsSource = null;
                lstProcesses.ItemsSource = listProcesses;
                lstProcesses.SelectedIndex = lstProcesses.Items.Count - 1;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lstProcesses.SelectedItem != null)
            {
                listTriggers.Add(lstProcesses.SelectedItem.ToString());
                listProcesses.Remove(lstProcesses.SelectedItem.ToString());
                lstTriggers.ItemsSource = null;
                lstTriggers.ItemsSource = listTriggers;
                lstProcesses.ItemsSource = null;
                lstProcesses.ItemsSource = listProcesses;
                lstTriggers.SelectedIndex = lstTriggers.Items.Count - 1;
            }
        }

        private void btnTrigger_Click(object sender, RoutedEventArgs e)
        {
            Screen[] screens = Screen.AllScreens;
            IntPtr handle = FindWindow(null, textProcessName.Text);

            var clientRect = new RECT();
            var windowRect = new RECT();
            GetClientRect(handle, out clientRect);
            GetWindowRect(handle, out windowRect);

            int borderWidth = (windowRect.ToRectangle().Right - windowRect.ToRectangle().Left - clientRect.ToRectangle().Right) / 2;
            int titleHeight = (windowRect.ToRectangle().Bottom - windowRect.ToRectangle().Top - clientRect.ToRectangle().Bottom) - borderWidth;
            int clientSizeRight = clientRect.ToRectangle().Right;
            int clientSizeBottom = clientRect.ToRectangle().Bottom;

            try
            {
                const UInt32 SWP_SHOWWINDOW = 0x0040;

                int Width = screens[comboMonitors.SelectedIndex].WorkingArea.Width;
                int Height = screens[comboMonitors.SelectedIndex].WorkingArea.Height;
                SetWindowPos(handle, 0, -borderWidth + screens[comboMonitors.SelectedIndex].Bounds.Left, -titleHeight, (borderWidth * 2) + screens[comboMonitors.SelectedIndex].Bounds.Right - screens[comboMonitors.SelectedIndex].Bounds.Left, titleHeight + screens[comboMonitors.SelectedIndex].Bounds.Bottom - screens[comboMonitors.SelectedIndex].Bounds.Top + getTaskbarHeight() + borderWidth, SWP_SHOWWINDOW);
            }
            catch (Exception e2)
            {
                System.Windows.Forms.MessageBox.Show(e2.Message, "Is the window in Windowed Mode / You have the correct window selected?");
            }
        }

        public int getTaskbarHeight()
        {
            Screen[] screens = Screen.AllScreens;
            return screens[comboMonitors.SelectedIndex].Bounds.Height - screens[comboMonitors.SelectedIndex].WorkingArea.Height;
        }

        private void lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textProcessName.Text = (sender as System.Windows.Controls.ListBox).SelectedItem as string;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, listTriggers);
                stream.Position = 0;
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                Properties.Settings.Default.processes = Convert.ToBase64String(buffer);
                Properties.Settings.Default.Save();
            }
        }
    }
}
