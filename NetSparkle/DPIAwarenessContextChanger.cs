using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AppLimit.NetSparkle
{
    public class DpiAwarenessContextChanger : IDisposable
    {
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_UNAWARE = new(-1);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new(-2);
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE = new(-3);
        private readonly IntPtr _oldContext;


        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);

        /// <summary>
        /// Set the DPI awareness for the current thread to the provided value.
        /// </summary>
        /// <param name="dpiContext">The new DPI_AWARENESS_CONTEXT for the current thread. This context includes the <see cref="DPI_AWARENESS"/> value.</param>
        /// <returns>The old DPI_AWARENESS_CONTEXT for the thread. If the <paramref name="dpiContext"/> is invalid, the thread will not be updated and the return value will be NULL. You can use this value to restore the old DPI_AWARENESS_CONTEXT after overriding it with a predefined value.</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        public DpiAwarenessContextChanger()
        {
            _oldContext = ChangeContextTo(DPI_AWARENESS_CONTEXT_UNAWARE);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        IntPtr SetContext(IntPtr newContext)
        {
            return SetThreadDpiAwarenessContext(newContext);
        }

        private IntPtr ChangeContextTo(IntPtr dpiAwarenessContextUnaware)
        {
            try
            {
                return SetContext(dpiAwarenessContextUnaware);
            }
            catch (Exception)
            {
                return IntPtr.Zero;

            }
        }

        public void Dispose()
        {
            ChangeContextTo(_oldContext);
        }
    }
}