using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Hasseware.Activities
{
    internal sealed class FileCopyHelper
    {
        private static class NativeMethod
        {
            [SuppressUnmanagedCodeSecurity]
            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName,
                CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref bool pbCancel, int dwCopyFlags);
        }

        public static void CopyFile(FileInfo source, FileInfo destination)
        {
            CopyFile(source, destination, Statements.FileCopyOptions.None);
        }

        public static void CopyFile(FileInfo source, FileInfo destination, Statements.FileCopyOptions options)
        {
            CopyFile(source, destination, options, null);
        }

        public static void CopyFile(FileInfo source, FileInfo destination,
            Statements.FileCopyOptions options, CopyFileHandler callback)
        {
            CopyFile(source, destination, options, callback, null);
        }

        public static void CopyFile(FileInfo source, FileInfo destination,
            Statements.FileCopyOptions options, CopyFileHandler callback, object state)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            new FileIOPermission(FileIOPermissionAccess.Read, source.FullName).Demand();
            new FileIOPermission(FileIOPermissionAccess.Write, destination.FullName).Demand();

            CopyProgressRoutine cpr = (callback == null) ? null : new CopyProgressRoutine(
                new CopyProgressData(source, destination, callback, state).CallbackHandler);

            bool cancel = false;
            if (!NativeMethod.CopyFileEx(source.FullName, destination.FullName, cpr, IntPtr.Zero, ref cancel, (int)options))
                throw new IOException(new Win32Exception().Message);
        }

        private class CopyProgressData
        {
            private FileInfo _source = null;
            private FileInfo _destination = null;
            private CopyFileHandler _callback = null;
            private object _state = null;

            public CopyProgressData(FileInfo source, FileInfo destination, CopyFileHandler callback, object state)
            {
                this._source = source;
                this._destination = destination;
                this._callback = callback;
                this._state = state;
            }

            public int CallbackHandler(long totalFileSize, long totalBytesTransferred, long streamSize,
                long streamBytesTransferred, int streamNumber, int callbackReason, IntPtr sourceFile, IntPtr destinationFile, IntPtr data)
            {
                return (int)this._callback(_source, _destination, _state, totalFileSize, totalBytesTransferred);
            }
        }

        private delegate int CopyProgressRoutine(long totalFileSize, long totalBytesTransferred, long streamSize,
            long streamBytesTransferred, int streamNumber, int callbackReason, IntPtr sourceFile, IntPtr destinationFile, IntPtr data);
    }

    internal delegate CopyFileAction CopyFileHandler(FileInfo source, FileInfo destination, object state, long totalFileSize, long totalBytesTransferred);

    internal enum CopyFileAction
    {
        Continue = 0,
        Cancel = 1,
        Stop = 2,
        Quiet = 3
    }
}