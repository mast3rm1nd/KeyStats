using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace KeyStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static List<KeysStats> overallStats = new List<KeysStats>();
        static List<KeysStats> dayStats = new List<KeysStats>();

        Thread dailyStatsRefreshingThread;// = new Thread(new ThreadStart(RefreshDailyStats));

        public MainWindow()
        {           
            InitializeComponent();

            LoadStatistics();

            _proc_kbd = HookCallback_Kbd;
            _hookID_Kbd = SetHook_Kbd(_proc_kbd);

            _proc_mouse = HookCallback_Mouse;
            _hookID_Mouse = SetHook_Mouse(_proc_mouse);

            dailyStatsRefreshingThread = new Thread(new ThreadStart(RefreshDailyStats));
            dailyStatsRefreshingThread.IsBackground = true;
            dailyStatsRefreshingThread.Start();
        }




        IntPtr HookCallback_Kbd(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)260)//для учёта альта
            {
                int vkCode = Marshal.ReadInt32(lParam);

                interceptedKey = ((Keys)vkCode).ToString();

                AddKeyToBothStats(interceptedKey);
                PrintStatistics();
                //Log_TextBox.Text += interceptedKey + Environment.NewLine;                              
            }

            //if (nCode >= 0 &&
            //MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            //{
            //    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            //    //Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
            //    Log_TextBox.Text += hookStruct.pt.x + ", " + hookStruct.pt.y + Environment.NewLine;
            //}

            return CallNextHookEx(_hookID_Kbd, nCode, wParam, lParam);
        }



        private IntPtr HookCallback_Mouse(
        int nCode, IntPtr wParam, IntPtr lParam)
        {
            //if (nCode >= 0 &&
            //    MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            //{
            //    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            //    //Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
            //    //Log_TextBox.Text += hookStruct.pt.x + ", " + hookStruct.pt.y + Environment.NewLine;
            //    Log_TextBox.Text += "LMB" + Environment.NewLine;
            //}

            //if (nCode >= 0 &&
            //    MouseMessages.WM_RBUTTONDOWN== (MouseMessages)wParam)
            //{
            //    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
            //    //Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
            //    //Log_TextBox.Text += hookStruct.pt.x + ", " + hookStruct.pt.y + Environment.NewLine;
            //    Log_TextBox.Text += "RMB" + Environment.NewLine;
            //}
            if (nCode >= 0)
            {
                var button = "";
                switch ((MouseMessages)wParam)
                {
                    case MouseMessages.WM_LBUTTONDOWN: button = "LMB"; break;
                    case MouseMessages.WM_RBUTTONDOWN: button = "RMB"; break;
                }

                if (button != "")
                {
                    AddKeyToBothStats(button);
                    PrintStatistics();
                }
                    //Log_TextBox.Text += msg + Environment.NewLine;
            }

            return CallNextHookEx(_hookID_Mouse, nCode, wParam, lParam);
        }



        void PrintStatistics()
        {
            DayStats_TextBox.Text = "";
            foreach (KeysStats stats in dayStats)
            {
                DayStats_TextBox.Text += String.Format("{0}: {1}", stats.Key, stats.TimesPressed) + Environment.NewLine;
            }

            OverallStats_TextBox.Text = "";
            foreach (KeysStats stats in overallStats)
            {
                OverallStats_TextBox.Text += String.Format("{0}: {1}", stats.Key, stats.TimesPressed) + Environment.NewLine;
            }
        }


        string StatsFileName = "За всё время.txt";
        void LoadStatistics()
        {
            if (!File.Exists(StatsFileName))
                return;

            var statsArr = File.ReadLines(StatsFileName);

            foreach(var stats in statsArr)
            {
                var splitted = stats.Split(' ');

                overallStats.Add(new KeysStats { Key = splitted[0], TimesPressed = uint.Parse(splitted[1]) });
            }

            PrintStatistics();
        }

        void SaveStatistics()
        {
            var toSave = "";

            foreach(KeysStats stats in overallStats)
            {
                toSave += String.Format("{0} {1}", stats.Key, stats.TimesPressed) + Environment.NewLine;
            }

            File.WriteAllText(StatsFileName, toSave);
        }


        void AddKeyToBothStats(string key)
        {
            //indexes
            var indexInDaily = dayStats.FindIndex(x => x.Key == key);
            var indexInOverall = overallStats.FindIndex(x => x.Key == key);


            //adding\incrementing
            if (indexInDaily == -1)
                dayStats.Add(new KeysStats { Key = key, TimesPressed = 1 });
            else
                dayStats[indexInDaily].TimesPressed++;

            dayStats = dayStats.OrderByDescending(x => x.TimesPressed).ToList();


            //adding\incrementing
            if (indexInOverall == -1)
                overallStats.Add(new KeysStats { Key = key, TimesPressed = 1 });
            else
                overallStats[indexInOverall].TimesPressed++;

            overallStats = overallStats.OrderByDescending(x => x.TimesPressed).ToList();
        }

        void SaveDailyStats()
        {
            var fileName = DateTime.Now.ToString("dd.MM.yyyy") + ".txt";
            File.WriteAllText(fileName, DayStats_TextBox.Text);
        }


        void RefreshDailyStats()
        {
            while(true)
            {
                if(DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second == 0)
                {
                    SaveDailyStats();
                    dayStats.Clear();
                    DayStats_TextBox.Text = "";
                    Thread.Sleep(1000 * 60 * 60 * 24 - 300);
                }

                Thread.Sleep(300);
            }
        }

        public class KeysStats
        {
            public string Key { get; set; }
            public uint TimesPressed { get; set; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        static string interceptedKey = "";
        static int clicksPerKey = 1;
        static int clicks_Delay = 1;

        private const int WH_MOUSE_LL = 14;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        //private static LowLevelKeyboardProc _proc = HookCallback;
        private static LowLevelKeyboardProc _proc_kbd;
        private static LowLevelMouseProc _proc_mouse;
        private static IntPtr _hookID_Kbd = IntPtr.Zero;
        private static IntPtr _hookID_Mouse = IntPtr.Zero;

        private static IntPtr SetHook_Kbd(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }


        private static IntPtr SetHook_Mouse(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);






        

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }


        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveStatistics();
        }
    }
}
