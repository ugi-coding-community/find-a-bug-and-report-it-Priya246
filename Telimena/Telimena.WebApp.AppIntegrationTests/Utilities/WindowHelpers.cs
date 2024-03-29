﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DotNetLittleHelpers;
using TestStack.White;
using TestStack.White.Factory;
using TestStack.White.UIItems.WindowItems;

namespace Telimena.WebApp.AppIntegrationTests.Utilities
{
    public class WindowHelpers
    {
        public static async Task<Window> WaitForWindowAsync(Expression<Predicate<string>> match, TimeSpan timeout, string errorMessage = "")
        {
            Window win = null;
            Stopwatch timeoutWatch = Stopwatch.StartNew();
            while (true)
            {
                await Task.Delay(50).ConfigureAwait(false);

                Process[] allProcesses = Process.GetProcesses().Where(x => !string.IsNullOrEmpty(x.MainWindowTitle)).ToArray();
                var compiled = match.Compile();

                foreach (Process allProcess in allProcesses)
                {
                    if (!compiled.Invoke(allProcess.MainWindowTitle))
                    {
                        continue;
                    }

                    Application app = TestStack.White.Application.Attach(allProcess);
                    win = app.Find(compiled, InitializeOption.NoCache);
                    if (win != null)
                    {
                        return win;
                    }
                }
                if (timeoutWatch.Elapsed > timeout)
                {
                    string expBody = ((LambdaExpression)match).Body.ToString();
                    throw new InvalidOperationException($"Failed to find window by expression on Title: {expBody}. " +
                                                        $"Available processes {DisplayProcessesInfo(allProcesses)}. Error: {errorMessage}");

                }
            }
        }

        private static string DisplayProcessesInfo(IEnumerable<Process> processes)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (Process process in processes)
            {
                try
                {
                    sb.AppendLine(process.GetPropertyInfoString(nameof(Process.MainWindowTitle), nameof(Process.HasExited), nameof(Process.ProcessName)));
                }
                catch
                {
                    sb.AppendLine("Cannot get process info");
                }
            }

            return sb.ToString();
        }

        public static async Task<Window> WaitForMessageBoxAsync(Expression<Predicate<string>> match, string title, TimeSpan timeout, string errorMessage = "")
        {

            Window win = null;
            Stopwatch timeoutWatch = Stopwatch.StartNew();
            while (true)
            {
                await Task.Delay(50).ConfigureAwait(false);

                Process[] allProcesses = Process.GetProcesses().Where(x => !string.IsNullOrEmpty(x.MainWindowTitle)).ToArray();
                var compiled = match.Compile();

                List<Process> matchinApps = allProcesses.Where(x => compiled.Invoke(x.MainWindowTitle)).ToList();

                foreach (Process appProcess in matchinApps)
                {
                    Application app = TestStack.White.Application.Attach(appProcess);
                    win = app.Find(compiled, InitializeOption.NoCache);
                    if (win != null)
                    {
                        try
                        {
                            Window msgBox = win.MessageBox(title);
                            if (msgBox != null)
                            {
                                return msgBox;
                            }
                        }
                        catch (Exception) { }

                    }
                }
                if (timeoutWatch.Elapsed > timeout)
                {
                    string expBody = ((LambdaExpression)match).Body.ToString();

                    throw new InvalidOperationException($"Failed to find window by expression on Title: {expBody}. Available processes {DisplayProcessesInfo(matchinApps)}. Error: {errorMessage}");
                }
            }
        }


        public static async Task<Window> WaitForMessageBoxAsync(Window parent, string title, TimeSpan timeout, string errorMessage = "")
        {
            Window win = null;
            Stopwatch timeoutWatch = Stopwatch.StartNew();
            while (win == null)
            {
                await Task.Delay(50).ConfigureAwait(false);
                try
                {
                    win = parent.MessageBox(title);
                }
                catch (Exception)
                {
                    //
                }

                if (timeoutWatch.Elapsed > timeout)
                {
                    throw new InvalidOperationException($"Failed to find MessageBox {errorMessage}. Parent window title: {parent.Title}, IsActive: {parent.IsCurrentlyActive}");
                }
            }

            return win;
        }
    }
}
