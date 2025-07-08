using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LorealAvaloniaUI.Services
{
    public static class DeElevatedProcessLauncher
    {
        // --- P/Invoke Structures ---

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_PROCESS_INFO
        {
            public int SessionId;
            public int ProcessId;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pProcessName;
            public IntPtr pUserSid; // SID of the user who owns the process
        }

        // --- Windows API Imports ---

        // Kernel32.dll functions
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern int WTSGetActiveConsoleSessionId();

        // Advapi32.dll functions
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            int dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int ImpersonationLevel,
            int TokenType,
            out IntPtr phNewToken);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        // Userenv.dll functions (for environment block)
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

        // Wtsapi32.dll functions (for enumerating processes in sessions)
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSEnumerateProcesses(
            IntPtr hServer,
            int Reserved,
            int Version,
            out IntPtr ppProcessInfo,
            out int pCount);

        [DllImport("Wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        // --- Constants ---
        private const int TOKEN_DUPLICATE = 0x0002;
        private const int TOKEN_QUERY = 0x0008;
        private const int TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const int SecurityImpersonation = 2; // Equivalent to SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation
        private const int TokenPrimary = 1; // Equivalent to TOKEN_TYPE.TokenPrimary

        // Creation Flags for CreateProcessAsUser
        private const int NORMAL_PRIORITY_CLASS = 0x00000020;
        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int CREATE_NO_WINDOW = 0x08000000; // Do not create a console window for the new process

        // Process Access Rights
        private const int PROCESS_QUERY_INFORMATION = 0x0400; // Required to open a process and query its information

        /// <summary>
        /// Launches a process as the currently logged-in user, even if the calling process is elevated.
        /// This method does not capture standard output or error directly.
        /// </summary>
        /// <param name="applicationPath">The full path to the executable (e.g., "C:\\Windows\\System32\\powershell.exe").</param>
        /// <param name="arguments">The arguments to pass to the executable.</param>
        /// <returns>True if the process was launched successfully, false otherwise.</returns>
        public static bool LaunchProcessAsCurrentUser(string applicationPath, string arguments)
        {
            IntPtr hToken = IntPtr.Zero; // Handle to the explorer.exe's process token
            IntPtr hPrimaryToken = IntPtr.Zero; // Duplicated primary token for CreateProcessAsUser
            IntPtr lpEnvironment = IntPtr.Zero; // Environment block for the new process
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION(); // Receives info about the new process

            // Declare these variables here so they are accessible in the finally block
            IntPtr explorerProcessHandle = IntPtr.Zero;
            int explorerProcessId = -1;

            try
            {
                // 1. Get the active console session ID
                // This ensures we target the explorer.exe of the currently logged-in user.
                int currentSessionId = WTSGetActiveConsoleSessionId();
                if (currentSessionId == 0xFFFFFFFF) // 0xFFFFFFFF indicates no active console session
                {
                    Debug.WriteLine("WTSGetActiveConsoleSessionId failed or no active console session.");
                    return false;
                }

                // 2. Enumerate processes to find explorer.exe in the current user's session
                IntPtr ppProcessInfo = IntPtr.Zero;
                int count = 0;

                if (!WTSEnumerateProcesses(IntPtr.Zero, 0, 1, out ppProcessInfo, out count))
                {
                    Debug.WriteLine($"WTSEnumerateProcesses failed. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                WTS_PROCESS_INFO[] processInfos = new WTS_PROCESS_INFO[count];
                IntPtr currentPtr = ppProcessInfo;
                for (int i = 0; i < count; i++)
                {
                    processInfos[i] = (WTS_PROCESS_INFO)Marshal.PtrToStructure(currentPtr, typeof(WTS_PROCESS_INFO));
                    currentPtr = (IntPtr)(currentPtr.ToInt64() + Marshal.SizeOf(typeof(WTS_PROCESS_INFO)));
                }
                WTSFreeMemory(ppProcessInfo); // Free the memory allocated by WTSEnumerateProcesses

                // Get the SID of the current user running the elevated application.
                // We want to launch the new process under this same user's non-elevated context.
                string currentUserSid = WindowsIdentity.GetCurrent().User.Value;

                foreach (var pInfo in processInfos)
                {
                    // Check if it's "explorer.exe", in the active console session, and belongs to the current user SID.
                    if (pInfo.pProcessName.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase) && pInfo.SessionId == currentSessionId)
                    {
                        try
                        {
                            // Compare SIDs to ensure it's the correct user's explorer.exe
                            SecurityIdentifier sid = new SecurityIdentifier(pInfo.pUserSid);
                            if (sid.Value == currentUserSid)
                            {
                                explorerProcessId = pInfo.ProcessId;
                                // Open a handle to the explorer.exe process with query information access.
                                explorerProcessHandle = OpenProcess(PROCESS_QUERY_INFORMATION, false, explorerProcessId);
                                if (explorerProcessHandle != IntPtr.Zero)
                                {
                                    Debug.WriteLine($"Found explorer.exe with PID {explorerProcessId} for current user.");
                                    break; // Found it, exit loop
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing explorer.exe info for PID {pInfo.ProcessId}: {ex.Message}");
                        }
                    }
                }

                if (explorerProcessHandle == IntPtr.Zero || explorerProcessId == -1)
                {
                    Debug.WriteLine("Could not find a suitable explorer.exe process for the current user and session.");
                    return false;
                }

                // 3. Open the process token of the found explorer.exe process
                if (!OpenProcessToken(explorerProcessHandle, TOKEN_DUPLICATE | TOKEN_QUERY, out hToken))
                {
                    Debug.WriteLine($"OpenProcessToken failed. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 4. Duplicate the token to create a primary token
                // This new token will be used to create the new process.
                if (!DuplicateTokenEx(
                    hToken,
                    TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY, // Desired access for the new token
                    IntPtr.Zero, // Default security attributes
                    SecurityImpersonation, // Impersonation level
                    TokenPrimary, // Token type (primary token for process creation)
                    out hPrimaryToken))
                {
                    Debug.WriteLine($"DuplicateTokenEx failed. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 5. Create an environment block for the new process (optional, but good practice)
                // This ensures the new process has proper environment variables.
                if (!CreateEnvironmentBlock(out lpEnvironment, hPrimaryToken, false))
                {
                    Debug.WriteLine($"CreateEnvironmentBlock failed. Error: {Marshal.GetLastWin32Error()}");
                    lpEnvironment = IntPtr.Zero; // Ensure it's zero if creation failed
                }

                // 6. Set up STARTUPINFO structure
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si); // Size of the structure
                si.lpDesktop = "winsta0\\default"; // Important for GUI applications to appear on the user's desktop
                si.dwFlags = (int)0x00000001; // STARTF_USESHOWWINDOW (to specify wShowWindow)
                si.wShowWindow = (short)1; // SW_SHOWNORMAL (show the window normally) or 0 for SW_HIDE (hidden)

                // Combine creation flags: normal priority, create Unicode environment, and no console window.
                int creationFlags = NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT | CREATE_NO_WINDOW;

                // 7. Create the process as the user
                string commandLine = $"\"{applicationPath}\" {arguments}";
                Debug.WriteLine($"Attempting to launch de-elevated: {commandLine}");

                if (!CreateProcessAsUser(
                    hPrimaryToken,
                    null, // lpApplicationName is null if lpCommandLine contains the full path
                    commandLine, // lpCommandLine contains the full path and arguments
                    IntPtr.Zero, // lpProcessAttributes (default)
                    IntPtr.Zero, // lpThreadAttributes (default)
                    false,       // bInheritHandles (set to false as we don't need handle inheritance for stdout/stderr here)
                    creationFlags,
                    lpEnvironment,
                    null, // lpCurrentDirectory (null uses current directory of calling process)
                    ref si, // STARTUPINFO structure
                    out pi)) // PROCESS_INFORMATION structure
                {
                    Debug.WriteLine($"CreateProcessAsUser failed. Error: {Marshal.GetLastWin32Error()}");
                    return false;
                }

                Debug.WriteLine($"Process launched successfully with PID: {pi.dwProcessId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in LaunchProcessAsCurrentUser: {ex.Message}");
                return false;
            }
            finally
            {
                // Always close handles to prevent resource leaks
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (hPrimaryToken != IntPtr.Zero) CloseHandle(hPrimaryToken);
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                if (lpEnvironment != IntPtr.Zero) DestroyEnvironmentBlock(lpEnvironment);
                if (explorerProcessHandle != IntPtr.Zero) CloseHandle(explorerProcessHandle);
            }
        }
    }
}
