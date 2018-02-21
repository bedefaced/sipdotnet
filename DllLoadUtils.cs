using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace sipdotnet
{
    /// <summary>
    /// Cross-platform native loading utils.
    /// By http://dimitry-i.blogspot.ru/2013/01/mononet-how-to-dynamically-load-native.html
    /// </summary>

    public class DllLoadUtils
    {

#if (WINDOWS)
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary (string fileName);

        [DllImport("kernel32.dll")]
        private static extern int FreeLibrary (IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress (IntPtr handle, string procedureName);

        [DllImport("msvcrt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsprintf (IntPtr buffer, string format, IntPtr args);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int _vscprintf (string format, IntPtr args);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VaListWindows
        {
            private IntPtr Pointer;
        }
#else
        const int RTLD_NOW = 2;

        [DllImport("libdl.so")]
        private static extern IntPtr dlopen (String fileName, int flags);

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym (IntPtr handle, String symbol);

        [DllImport("libdl.so")]
        private static extern int dlclose (IntPtr handle);

        [DllImport("libdl.so")]
        private static extern IntPtr dlerror ();

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsprintf(IntPtr buffer, [In][MarshalAs(UnmanagedType.LPStr)] string format, IntPtr args);

        [DllImport("libc", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int vsnprintf(IntPtr buffer, UIntPtr size, [In][MarshalAs(UnmanagedType.LPStr)] string format, IntPtr args);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct VaListLinuxX64
        {
            private UInt32 gp_offset;
            private UInt32 fp_offset;
            private IntPtr overflow_arg_area;
            private IntPtr reg_save_area;
        }
#endif


        public static IntPtr DoLoadLibrary (string fileName)
        {
#if (WINDOWS)
            return LoadLibrary(fileName);
#else
            return dlopen(fileName, RTLD_NOW);
#endif
        }

        public static void DoFreeLibrary (IntPtr handle)
        {
#if (WINDOWS)
            FreeLibrary(handle);
#else
            dlclose(handle);
#endif
        }

        public static IntPtr DoGetProcAddress (IntPtr dllHandle, string name)
        {
#if (WINDOWS)
            return GetProcAddress(dllHandle, name);
#else
            // clear previous errors if any
            dlerror();
            var res = dlsym(dllHandle, name);
            var errPtr = dlerror();
            if (errPtr != IntPtr.Zero)
            {
                throw new Exception("dlsym: " + Marshal.PtrToStringAnsi(errPtr));
            }
            return res;
#endif
        }

        public static string ProcessVAlist(string format, IntPtr args)
        {
#if (WINDOWS)
            int byteLength = _vscprintf(format, args) + 1;
            IntPtr buffer = Marshal.AllocHGlobal(byteLength);

            try
            {
                vsprintf(buffer, format, args);

                return Marshal.PtrToStringAnsi(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
#else
            bool is64 = System.Environment.Is64BitOperatingSystem;

            if (is64)
            {
                var listStructure = Marshal.PtrToStructure(args, typeof(VaListLinuxX64));
                int byteLength = 0;
                IntPtr listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(listStructure));

                try
                {
                    Marshal.StructureToPtr(listStructure, listPointer, false);
                    byteLength = vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, listPointer) + 1;
                }
                finally
                {
                    Marshal.FreeHGlobal(listPointer);
                }

                IntPtr buffer = Marshal.AllocHGlobal(byteLength);
                try
                {
                    listPointer = Marshal.AllocHGlobal(Marshal.SizeOf(listStructure));
                    try
                    {
                        Marshal.StructureToPtr(listStructure, listPointer, false);
                        vsprintf(buffer, format, listPointer);
                        return Marshal.PtrToStringAnsi(buffer);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(listPointer);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            else
            {
                int byteLength = vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, args) + 1;
                IntPtr buffer = Marshal.AllocHGlobal(byteLength);
                try
                {
                    vsprintf(buffer, format, args);
                    return Marshal.PtrToStringAnsi(buffer);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
#endif
        }
    }
    
}
