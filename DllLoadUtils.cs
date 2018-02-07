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
    }
    
}
