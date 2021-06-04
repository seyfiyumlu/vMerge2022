using System;
using System.Threading;

namespace alexbegh.Utility.Helpers.Atomic
{
    /// <summary>
    /// This class provides light-weight fast access to its members
    /// by multiple threads which may be spread across multiple cores.
    /// Performs a lot faster than using lock on its members.
    /// A concrete class needs to derive from it.
    /// </summary>
    /// <example>
    /// struct MyData { int data1; string data2; }
    /// class ThreadSafeData : AtomicMutable&lt;MyData&gt; {
    ///     public int Data1 {
    ///         get { AcquireRead(); var res = Data.data1; ReleaseRead(); return res; }
    ///         set { AcquireWrite(); Data.data1 = value; ReleaseWrite(); }
    ///     }
    ///     public string Data2 {
    ///         get { AcquireRead(); var res = Data.data2; ReleaseRead(); return res; }
    ///         set { AcquireWrite(); Data.data2 = value; ReleaseWrite(); }
    ///     }
    ///  }
    /// </example>
    /// <typeparam name="T">The type to wrap</typeparam>
    public abstract class AtomicMutable<T> where T: struct
    {
        #region Private Fields
        /// <summary>
        /// The read lock counter
        /// </summary>
        private long _isReadLocked = 0;

        /// <summary>
        /// The write lock counter
        /// </summary>
        private long _isWriteLocked = 0;

        /// <summary>
        /// The change counter (thread-specific)
        /// </summary>
        [ThreadStatic]
        private long _changes = 0;

        /// <summary>
        /// The global change counter (not thread specific)
        /// </summary>
        private long _totalChanges = 0;
        #endregion

        #region Protected Fields
        /// <summary>
        /// The contained data member
        /// </summary>
        protected T Data;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an instance, initializing the data member
        /// </summary>
        protected AtomicMutable()
        {
            Data = new T();
        }
        #endregion

        #region Lock/Release Operations
        /// <summary>
        /// Acquire a read lock. Increments the read counter, waits until the write counter drops to zero
        /// </summary>
        protected void AcquireRead()
        {
            int count = 0;
            while (Interlocked.CompareExchange(ref _isWriteLocked, 1, 0) == 1)
            {
                if ((++count) > 1000)
                    Thread.Yield();
            }
            Interlocked.Increment(ref _isReadLocked);
            Interlocked.Decrement(ref _isWriteLocked);
        }

        /// <summary>
        /// Release a read lock
        /// </summary>
        protected void ReleaseRead()
        {
            Interlocked.Decrement(ref _isReadLocked);
        }

        /// <summary>
        /// Acquire a write lock. Sets the write counter to 1 as soon as it is 0, then waits until the read counter is 0
        /// </summary>
        protected void AcquireWrite()
        {
            int count = 0;
            while (Interlocked.CompareExchange(ref _isWriteLocked, 1, 0) == 1)
            {
                if ((++count) > 1000)
                    Thread.Yield();
            }
            while (Interlocked.Read(ref _isReadLocked) != 0)
            {
                if ((++count) > 1000)
                    Thread.Yield();
            }
        }

        /// <summary>
        /// Releases a write lock, increments the thread specific change counter
        /// </summary>
        protected void ReleaseWrite()
        {
            Interlocked.Increment(ref _totalChanges);
            Interlocked.Decrement(ref _isWriteLocked);
        }
        #endregion

        #region Public Operations
        /// <summary>
        /// Gets the contained data, resets the thread specific change counter
        /// </summary>
        /// <param name="data">The contained data</param>
        public void GetAndReset(ref T data)
        {
            try
            {
                AcquireRead();
                data = Data;
                _changes = Interlocked.Read(ref _totalChanges);
            }
            finally
            {
                ReleaseRead();
            }
        }

        /// <summary>
        /// Just gets the contained data.
        /// </summary>
        /// <param name="data">The contained data</param>
        public void Get(ref T data)
        {
            try
            {
                AcquireRead();
                data = Data;
            }
            finally
            {
                ReleaseRead();
            }
        }

        /// <summary>
        /// Resets the thread specific change info, <seealso cref="HasChangedSinceLastGetForCurrentThread"/>
        /// </summary>
        public void Reset()
        {
            _changes = Interlocked.Read(ref _totalChanges);
        }

        /// <summary>
        /// Sets the contained data, implicitly increments the thread specific change counter
        /// </summary>
        /// <param name="data"></param>
        public void Set(ref T data)
        {
            try
            {
                AcquireWrite();
                Data = data;
            }
            finally
            {
                ReleaseWrite();
            }
        }

        /// <summary>
        /// Checks if the given data has changed from the view of a certain thread, <seealso cref="GetAndReset"/>
        /// </summary>
        /// <returns>true if changes are available since last GetAndReset</returns>
        public bool HasChangedSinceLastGetForCurrentThread()
        {
            return (_changes != Interlocked.Read(ref _totalChanges));
        }
        #endregion
    }
}
