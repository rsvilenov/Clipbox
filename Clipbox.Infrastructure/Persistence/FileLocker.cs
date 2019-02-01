using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Clipbox.Infrastructure.Persistence
{
    public class FileLocker : IDisposable
    {
        private readonly string _lockFilePath;
        
        private bool _disposed;

        private bool _leaveLockFileOnDispose;

        private FileStream _lockFileStream;

        private const int TIMEOUT_INCREMENT_MS = 50;

        private static readonly TimeSpan LOCK_VALIDITY_SECONDS = TimeSpan.FromSeconds(100);

        /// <summary>
        /// This constructor is here only for testing purposes
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="lifeSpan"></param>
        /// <param name="leaveLockFileOnDispose"></param>
        internal FileLocker(string filePath, int timeoutMilliseconds, TimeSpan lifeSpan, bool leaveLockFileOnDispose)
            : this (filePath, timeoutMilliseconds, lifeSpan)
        {
            _leaveLockFileOnDispose = leaveLockFileOnDispose;
        }

        public FileLocker(string filePath, int timeoutMilliseconds = 0 /* 0 => wait indefinitely */)
            : this(filePath, timeoutMilliseconds, LOCK_VALIDITY_SECONDS)
        { }

        public FileLocker(string filePath, TimeSpan lifeSpan)
            : this(filePath, 0, lifeSpan)
        { }

        private FileLocker(string filePath, int timeoutMilliseconds, TimeSpan lifeSpan)
        {
            _lockFilePath = string.Concat(filePath, ".lock");

            long totalWaitingMs = 0;
            while (!TryAquireLock(lifeSpan))
            {
                Thread.Sleep(TIMEOUT_INCREMENT_MS);
                totalWaitingMs += TIMEOUT_INCREMENT_MS;
                if (timeoutMilliseconds != 0 && totalWaitingMs > timeoutMilliseconds)
                {
                    throw new TimeoutException("Timeout expired.");
                }
            }
        }
        
        private bool TryAquireLock(TimeSpan lockLifeSpan)
        {
            bool lockAquired = false;

            if (_lockFileStream != null)
            {
                // If the stream is not null and is not disposed
                // (FileLocker.Dispose() sets the stream to null),
                // this means that we already own the lock.
                lockAquired = true;
            }
            else if (CheckCanAquireLock())
            {
                lockAquired = AquireLock(lockLifeSpan);
            }

            return lockAquired;
        }

        private bool CheckCanAquireLock()
        {
            bool canAquireLock = false;

            if (File.Exists(_lockFilePath))
            {
                try
                {
                    using (var stream = File.Open(_lockFilePath,
                        FileMode.Open,
                        FileAccess.Read,
                        // the file may already have been opened with
                        // FileShare.Write, so we cannot just use FileShare.Read here,
                        // as the file has already been locked for writing. We also
                        // allow deletion of the file, in order to handle the case when
                        // FileLocker.Dispose() gets called at the time of this call. We should
                        // allow deletion of the file at all cases and the consequences here are
                        // handled by the exception handling below.
                        FileShare.ReadWrite | FileShare.Delete))
                    using (var streamReader = new StreamReader(stream))
                    {
                        string lockDateTimeStr = streamReader.ReadLine();
                        string lifeSpanStoredStr = streamReader.ReadLine();

                        // if we are reading a file which is being written at the same time (should never be true, but anyway...)
                        if (string.IsNullOrWhiteSpace(lockDateTimeStr) || !lockDateTimeStr.All(c => char.IsDigit(c)))
                        {
                            canAquireLock = false;
                        }
                        else
                        {
                            long ticksStored = long.Parse(lockDateTimeStr);
                            long lifeSpanStored = int.Parse(lifeSpanStoredStr);
                            canAquireLock = (DateTime.Now - new DateTime(ticksStored)).TotalMilliseconds > lifeSpanStored;
                        }
                    }
                }
                // despite our check above, we may run into a race condition here, so we catch the exception as well
                catch (FileNotFoundException)
                {
                    canAquireLock = true;
                }
                catch (UnauthorizedAccessException)
                {
                    canAquireLock = false;
                }
                // check if someone has already locked the file
                catch (IOException ioe)
                {
                    // https://stackoverflow.com/questions/1304/how-to-check-for-file-lock
                    var errorCode = Marshal.GetHRForException(ioe) & ((1 << 16) - 1);
                    canAquireLock = errorCode != 32 && errorCode != 33;
                }
                // well, this shouldn't happen
                catch (Exception ex) // TODO: recovery?
                {
                    throw new FileLoadException("FIX ASAP.", ex);
                }
            }
            else
            {
                canAquireLock = true;
            }

            return canAquireLock;
        }

        private bool AquireLock(TimeSpan lockLifeSpan)
        {
            bool lockAquired;

            try
            {
                // hold the file open the file for reading
                _lockFileStream = File.Open(_lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

                using (StreamWriter lockWriter = new StreamWriter(_lockFileStream,
                    System.Text.Encoding.ASCII,
                    bufferSize: 1024,
                    // Leave the stream open, so that the file remains locked 
                    // until FileLocker.Dispose() gets called.
                    leaveOpen: true))
                {
                    lockWriter.WriteLine(DateTime.Now.Ticks);
                    lockWriter.WriteLine(lockLifeSpan.TotalMilliseconds);
                }
                lockAquired = true;
            }
            // check if someone has already locked the file
            catch (UnauthorizedAccessException)
            {
                lockAquired = false;
            }
            catch (IOException ioe)
            {
                var errorCode = Marshal.GetHRForException(ioe) & ((1 << 16) - 1);
                lockAquired = errorCode != 32 && errorCode != 33;
            }
            catch (Exception ex)
            {
                throw new FileLoadException("Creating lock file failed.", ex);
            }

            return lockAquired;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_lockFileStream != null)
            {
                _lockFileStream.Dispose();
                if (!_leaveLockFileOnDispose)
                {
                    File.Delete(_lockFilePath);
                }
                // signal that the file is not
                // being held anymore
                _lockFileStream = null;
            }

            _disposed = true;

        }

        /// <summary>
        /// TODO: do I really need this?
        /// </summary>
        public class FileLockerException : Exception
        {
            public FileLockerException()
            {
            }

            public FileLockerException(string message)
                : base(message)
            {
            }

            public FileLockerException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}
