using Mono.Unix;
using OpenQA.Selenium;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Globals
{
    public static class ExitSignal
    {
        public interface IExitSignal
        {
            event EventHandler Exit;
        }

        public class WinExitSignal : IExitSignal
        {
            public event EventHandler Exit;

            private static IWebDriver Driver;

            [DllImport("Kernel32")]
            public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

            // A delegate type to be used as the handler routine for SetConsoleCtrlHandler.
            public delegate bool HandlerRoutine(CtrlTypes CtrlType);

            // An enumerated type for the control messages sent to the handler routine.
            public enum CtrlTypes
            {
                CTRL_C_EVENT = 0,
                CTRL_BREAK_EVENT,
                CTRL_CLOSE_EVENT,
                CTRL_LOGOFF_EVENT = 5,
                CTRL_SHUTDOWN_EVENT
            }

            /// <summary>
            /// Need this as a member variable to avoid it being garbage collected.
            /// </summary>
            private readonly HandlerRoutine m_hr;

            public WinExitSignal(IWebDriver driver)
            {
                Driver = driver;

                m_hr = new HandlerRoutine(ConsoleCtrlCheck);

                SetConsoleCtrlHandler(m_hr, true);

            }

            /// <summary>
            /// Handle the ctrl types
            /// </summary>
            /// <param name="ctrlType"></param>
            /// <returns></returns>
            private bool ConsoleCtrlCheck(CtrlTypes ctrlType)
            {
                switch (ctrlType)
                {
                    case CtrlTypes.CTRL_C_EVENT:
                    case CtrlTypes.CTRL_BREAK_EVENT:
                    case CtrlTypes.CTRL_CLOSE_EVENT:
                    case CtrlTypes.CTRL_LOGOFF_EVENT:
                    case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                        if (Driver != null)
                        {
                            Driver.Quit();
                        }
                        Exit?.Invoke(this, EventArgs.Empty);
                        break;
                    default:
                        break;
                }

                return true;
            }
        }

        public class UnixExitSignal : IExitSignal
        {
            public event EventHandler Exit;

            private static IWebDriver Driver;

            public readonly UnixSignal[] Signals = new UnixSignal[]
            {
                new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
                new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
                new UnixSignal(Mono.Unix.Native.Signum.SIGUSR1)
            };

            public UnixExitSignal(IWebDriver driver)
            {
                Driver = driver;

                Task.Factory.StartNew(() =>
                {
                    // Blocking call to wait for any kill signal
                    int index = UnixSignal.WaitAny(Signals, -1);

                    if (Driver != null)
                    {
                        Driver.Quit();
                    }

                    Exit?.Invoke(null, EventArgs.Empty);
                });
            }
        }

        public static IExitSignal Signal;

        public static void InitSignal(IWebDriver driver)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Signal = new WinExitSignal(driver);
            }
            else
            {
                Signal = new UnixExitSignal(driver);
            }
        }
    }
}
