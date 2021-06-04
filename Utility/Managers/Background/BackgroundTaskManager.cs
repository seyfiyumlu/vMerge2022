using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Managers.View;
using alexbegh.Utility.UserControls.LoadingProgress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace alexbegh.Utility.Managers.Background
{
    /// <summary>
    /// The BackgroundTaskManager class provides the following features:
    ///     - Serves as a task pool with a given key
    ///     - Only one task with a certain key will run simultaneously
    ///     - Keeps progress indicators in the UI up to date (see LoadingProgressControl)
    /// </summary>
    public sealed class BackgroundTaskManager : IDisposable
    {
        #region Private Fields
        /// <summary>
        /// The UI threads synchronization context
        /// </summary>
        private SynchronizationContext _uiContext;

        /// <summary>
        /// The UI threads' id
        /// </summary>
        private int _uiThreadId;

        /// <summary>
        /// The monitor task which updates the calling UI threads with the 
        /// collected progress information once per 100msec
        /// </summary>
        private Task _progressMonitor;

        /// <summary>
        /// The locker for accessing the "Tasks" property
        /// </summary>
        private object _locker = new object();

        /// <summary>
        /// The locker for starting a new task
        /// </summary>
        private object _startLock = new object();

        /// <summary>
        /// Queue of items to post to the main process
        /// </summary>
        private Queue<Func<bool>> _delayedPostActions;

        /// <summary>
        /// The View manager
        /// </summary>
        private ViewManager _viewManager;

        /// <summary>
        /// The list of discardable task keys
        /// </summary>
        private List<string> _discardableTaskKeys;

        /// <summary>
        /// The private task factory
        /// </summary>
        private TaskFactory _taskFactory;

        /// <summary>
        /// True if all tasks are being shut down
        /// </summary>
        private volatile bool _cancelMode;

        /// <summary>
        /// True if cancel dialog is already running
        /// </summary>
        private volatile bool _cancelDialogActive;
        #endregion

        #region Private Properties
        /// <summary>
        /// The currently active tasks
        /// </summary>
        private Dictionary<string, BackgroundTask> Tasks
        {
            get;
            set;
        }

        private static BackgroundTaskManager Instance
        {
            get;
            set;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Checks periodically if UI updates are requested.
        /// Executes the DelayedPost queue.b
        /// </summary>
        private void ProgressMonitorTask()
        {
            // Initialize local variables
            var tasks = new List<BackgroundTask>();
            var updates = new Dictionary<SynchronizationContext, List<Tuple<ProgressData, IEnumerable<LoadingProgressViewModel>>>>();
            while (true)
            {
                Thread.Sleep(100);
                tasks.Clear();
                updates.Clear();

                // Collect all tasks which have the LoadingProgressViewModel property set
                lock (_locker)
                {
                    while (_delayedPostActions.Count > 0)
                    {
                        var item = _delayedPostActions.Dequeue();
                        _uiContext.Post((o) =>
                        {
                            if (!item())
                                DelayedPost(item);
                        }, null);
                    }
                    tasks.AddRange(Tasks.Values.Where(item => item.LoadingProgressViewModels.Count > 0));
                }

                // For all of those: update the UI
                foreach (var task in tasks)
                {
                    // Check if anything changed since last time
                    if (task.TrackProgress.HasChangedSinceLastGetForCurrentThread())
                    {
                        // Add this task to be updated for its calling SynchronizationContext
                        if (!updates.ContainsKey(task.Ctx))
                            updates[task.Ctx] = new List<Tuple<ProgressData, IEnumerable<LoadingProgressViewModel>>>();

                        updates[task.Ctx].Add(
                            new Tuple<ProgressData, IEnumerable<LoadingProgressViewModel>>(
                                task.TrackProgress.GetCurrentProgress(),
                                task.LoadingProgressViewModels.AsEnumerable()
                                ));
                    }
                }

                // Anything to update?
                if (updates.Count > 0)
                {
                    // Yes, iterate all distinct SynchronizationContexts
                    foreach (var item in updates)
                    {
                        SynchronizationContext ctx = item.Key;

                        // And post back the updated progress information
                        ctx.Post(
                            (o) =>
                            {
                                foreach (var data in item.Value)
                                {
                                    foreach (var vm in data.Item2)
                                    {
                                        vm.ProgressInfo = data.Item1.ProgressInfo;
                                        vm.CurrentProgress = data.Item1.CurrentProgress;
                                        vm.MaxProgress = data.Item1.MaxProgress;
                                    }
                                }
                            }, null
                        );
                    }
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs the BackgroundTaskManager (and the progress monitor task)
        /// </summary>
        public BackgroundTaskManager(ViewManager viewManager)
        {
            if (Instance != null)
                throw new InvalidOperationException("This class must only be instantiated once.");
            Instance = this;
            Tasks = new Dictionary<string, BackgroundTask>();
            _uiContext = SynchronizationContext.Current;
            if (_uiContext==null)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            _delayedPostActions = new Queue<Func<bool>>();
            _viewManager = viewManager;
            _discardableTaskKeys = new List<string>();
            _taskFactory = new System.Threading.Tasks.TaskFactory(TaskScheduler.Default);

            // Create the progress monitor task
            _progressMonitor = new Task(ProgressMonitorTask);
            _progressMonitor.Start();
        }
        #endregion

        #region Internal Operations
        /// <summary>
        /// Called when a BackgroundTask has finished
        /// </summary>
        /// <param name="key">The BackgroundTasks key</param>
        /// <param name="task">The BackgroundTask</param>
        internal bool InformStopped(string key, BackgroundTask task)
        {
            lock (_locker)
            {
                if (Tasks.ContainsKey(key) == false)
                    return false;

                BackgroundTask test = Tasks[key];
                if (test != task)
                    return false;

                Tasks.Remove(key);
            }
            if (task.LoadingProgressViewModels.Any(item => item.IsLoading))
            {
                task.SendBack(
                    () =>
                    {
                        foreach (var vm in task.LoadingProgressViewModels)
                            vm.IsLoading = false;
                    });
            }
            return true;
        }

        /// <summary>
        /// Returns the instance of the BackgroundTaskManager (if any)
        /// </summary>
        /// <returns>The instance</returns>
        internal static BackgroundTaskManager GetInstance()
        {
            return Instance;
        }
        #endregion

        #region Internal Properties
        /// <summary>
        /// Returns the internal task factory
        /// </summary>
        internal TaskFactory TaskFactory
        {
            get
            {
                return _taskFactory;
            }
        }
        #endregion

        #region Public Operations
        /// <summary>
        ///  Registers the provided task given its key as discardable, which means
        ///  the "Cancel" operation will succeed without waiting for completion.
        ///  This relies on the task not firing any events after having received a
        ///  OperationCancelledException!
        /// </summary>
        /// <param name="key">The task key</param>
        public void RegisterDiscardable(string key)
        {
            lock (_locker)
            {
                if (_discardableTaskKeys.Contains(key))
                    throw new ArgumentException("Key '" + key + "' already registered as discardable", key);
                _discardableTaskKeys.Add(key);
            }
        }

        /// <summary>
        /// Cancels a running background task. Does nothing if the given task doesn't run anymore.
        /// Performs synchronously.
        /// </summary>
        /// <param name="key">The tasks identifier</param>
        public async void Cancel(string key)
        {
            BackgroundTask running;

            lock (_locker)
            {
                Tasks.TryGetValue(key, out running);
            }
            if (running != null)
            {
                running.Cancelled.Cancel();
                try
                {
                    await running.Task;
                }
                catch (TaskCanceledException)
                {
                    InformStopped(key, running);
                }
            }
        }

        /// <summary>
        /// Cancels all running tasks.
        /// </summary>
        public void CancelAll()
        {
            if (_cancelMode)
                throw new InvalidOperationException();

            try
            {
                lock (_locker)
                {
                    _cancelMode = true;
                }
                Application.Current.MainWindow.IsEnabled = false;
                bool continueWaiting = true;
                var taskKeys = new Dictionary<string, BackgroundTask>();
                SimpleLogger.Log(SimpleLogLevel.Info, "Cancelling all tasks");
                while (continueWaiting)
                {
                    System.Windows.Forms.Application.DoEvents();
                    foreach(var t in Tasks)
                        taskKeys[t.Key] = t.Value;
                    var tasks = Tasks.Values.Select(task => task.Task).Where(task => !task.IsCompleted && !task.IsCanceled).ToArray();
                    if (tasks.Any())
                    {
                        try
                        {
                            continueWaiting = Task.WaitAll(tasks, 100);
                        }
                        catch(AggregateException exceptions)
                        {
                            SimpleLogger.Log(exceptions, false);
                        }
                    }
                    else
                        continueWaiting = false;
                }
                foreach (var taskKey in taskKeys)
                    InformStopped(taskKey.Key, taskKey.Value);
            }
            finally
            {
                Application.Current.MainWindow.IsEnabled = true;
                _cancelMode = false;
            }
        }

        /// <summary>
        /// Starts a new task; cancels an already running task with the same key, if any
        /// </summary>
        /// <param name="key">The tasks key</param>
        /// <param name="model">The LoadingProgressViewModel, may be null</param>
        /// <param name="TaskAction">The task to start</param>
        /// <param name="cancelRunning">true if a running task should be cancelled, false if the LoadingProgressViewModel should attach to the running task</param>
        public bool Start(string key, LoadingProgressViewModel model, Action<BackgroundTask> TaskAction, bool cancelRunning = true)
        {
            BackgroundTask running;

            if (_cancelMode)
                return false;

            if (model == null)
                model = new LoadingProgressViewModel();

            while (true)
            {
                bool isDiscardable = false;
                lock (_locker)
                {
                    isDiscardable = _discardableTaskKeys.Contains(key);
                    Tasks.TryGetValue(key, out running);
                    if (running != null && !cancelRunning)
                    {
                        if (!running.LoadingProgressViewModels.Contains(model))
                        {
                            model.IsLoading = true;
                            running.LoadingProgressViewModels.Add(model);
                            return false;
                        }
                    }
                }
                if (running != null)
                {
                    running.Cancelled.Cancel();
                    if (isDiscardable)
                    {
                        InformStopped(key, running);
                    }
                    else
                    {
                        try
                        {
                            Application.Current.MainWindow.IsEnabled = false;
                            while (!running.Task.Wait(100))
                            {
                                System.Windows.Forms.Application.DoEvents();
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            InformStopped(key, running);
                        }
                        catch (AggregateException ex)
                        {
                            bool doThrow = true;
                            if (ex.InnerExceptions.Count == 1)
                            {
                                if (ex.InnerExceptions[0].GetType() == typeof(TaskCanceledException))
                                {
                                    InformStopped(key, running);
                                    doThrow = false;
                                }
                            }
                            if (doThrow)
                                throw;
                        }
                        finally
                        {
                            Application.Current.MainWindow.IsEnabled = true;
                        }
                    }
                }

                lock (_startLock)
                {
                    lock(_locker)
                    {
                        if (Tasks.TryGetValue(key, out running))
                            continue;
                    }

                    SynchronizationContext ctx = _uiContext;
                    BackgroundTask newTask
                        = new BackgroundTask(this, model, key, ctx);
                    Tasks[key] = newTask;

                    newTask.Task = TaskFactory.StartNew(
                            () =>
                            {
                                bool posted = false;
                                try
                                {
                                    ctx.Post(
                                        (o) => 
                                        {
                                            if (!posted)
                                            {
                                                posted = true;
                                                model.IsLoading = true;
                                            }
                                        }, null);
                                    TaskAction(newTask);
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    posted = true;
                                    InformStopped(key, newTask);
                                }
                            },
                            newTask.Cancelled.Token, 
                            TaskCreationOptions.None, TaskScheduler.Default);
                }
                break;
            }
            return true;
        }

        /// <summary>
        /// Runs a specified action and shows a dialog with a cancel button, if the action takes longer than 100msec
        /// </summary>
        /// <typeparam name="T_Result">The type of the result</typeparam>
        /// <param name="action">The action</param>
        /// <param name="caption">The caption, may be null</param>
        /// <param name="description">The description, may be null</param>
        /// <param name="externalProgress">Used when integrated in "outer" context</param>
        /// <returns>The result</returns>
        public T_Result RunWithCancelDialog<T_Result>(Func<TrackProgressParameters, T_Result> action, string caption, string description, TrackProgressParameters externalProgress = null)
        {
            if (_cancelMode)
                return default(T_Result);

            if (_cancelDialogActive)
            {
                return action(new TrackProgressParameters(TrackProgressMode.PerformWithIncrementsAndSetMaximum));
            }
            try
            {
                _cancelDialogActive = true;

                if (externalProgress != null)
                {
                    try
                    {
                        return action(externalProgress);
                    }
                    catch (OperationCanceledException)
                    {
                        return default(T_Result);
                    }
                }

                T_Result returnValue = default(T_Result);
                if (Thread.CurrentThread.ManagedThreadId != _uiThreadId)
                {
                    _uiContext.Send(
                        (o) =>
                        {
                            returnValue = RunWithCancelDialog(action, caption, description);
                        }, null);
                    return returnValue;
                }

                Task task = null;
                T_Result taskResult = default(T_Result);
                try
                {
                    var cts = new CancellationTokenSource();
                    var trackProgressParams = new TrackProgressParameters(TrackProgressMode.PerformWithIncrementsAndSetMaximum, cts.Token);
                    task = new Task(() => taskResult = action(trackProgressParams));
                    task.Start();
                    task.Wait(250);
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        if (!_cancelMode && !task.IsCanceled && !cts.IsCancellationRequested && task.Exception != null)
                            throw task.Exception;

                        return default(T_Result);
                    }
                    else if (task.IsCompleted)
                    {
                        return taskResult;
                    }

                    var wnd = new Windows.WaitForBackgroundActionDialog(trackProgressParams.TrackProgress, cts, task);
                    if (caption != null)
                    {
                        wnd.WindowStyle = System.Windows.WindowStyle.ToolWindow;
                        wnd.Title = caption;
                    }
                    if (description != null)
                    {
                        wnd.Description.Text = description;
                    }
                    if (_viewManager != null)
                    {
                        _viewManager.RaiseBeforeModalDialog(wnd);
                        _viewManager.RaiseWindowCreated(wnd);
                    }
                    try
                    {
                        wnd.ShowDialog();
                    }
                    finally
                    {
                        if (_viewManager != null)
                        {
                            _viewManager.RaiseAfterModalDialog(wnd);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    if (task != null && task.IsCompleted && !task.IsFaulted)
                    {
                        returnValue = taskResult;
                    }
                }
                if (task != null && task.Exception != null)
                {
                    if (task.Exception.InnerExceptions.Any(ex => ex.GetType() != typeof(OperationCanceledException)))
                        throw task.Exception;
                }
                return returnValue;
            }
            finally
            {
                _cancelDialogActive = false;
            }
        }

        /// <summary>
        /// Runs a specified action and shows a dialog with a cancel button, if the action takes longer than 100msec
        /// </summary>
        /// <typeparam name="T_Result">The type of the result</typeparam>
        /// <param name="action">The action</param>
        /// <param name="externalProgress">Used when integrated in "outer" context</param>
        /// <returns>The result</returns>
        public T_Result RunWithCancelDialog<T_Result>(Func<TrackProgressParameters, T_Result> action, TrackProgressParameters externalProgress = null)
        {
            return RunWithCancelDialog(action, null, null, externalProgress);
        }

        /// <summary>
        /// Runs a specified action and shows a dialog with a cancel button, if the action takes longer than 100msec
        /// </summary>
        /// <param name="action">The action</param>
        /// <param name="description">The description, may be null</param>
        /// <param name="externalProgress">Used when integrated in "outer" context</param>
        /// <returns>true if successful</returns>
        public bool RunWithCancelDialog(Action<TrackProgressParameters> action, string description, TrackProgressParameters externalProgress = null)
        {
            return RunWithCancelDialog((parameters) => { action(parameters); return true; }, null, description, externalProgress);
        }

        /// <summary>
        /// Runs a specified action and shows a dialog with a cancel button, if the action takes longer than 100msec
        /// </summary>
        /// <param name="action">The action</param>
        /// <param name="caption">The caption, may be null</param>
        /// <param name="description">The description, may be null</param>
        /// <param name="externalProgress">Used when integrated in "outer" context</param>
        /// <returns>true if successful</returns>
        public bool RunWithCancelDialog(Action<TrackProgressParameters> action, string caption, string description, TrackProgressParameters externalProgress = null)
        {
            return RunWithCancelDialog((parameters) => { action(parameters); return true; }, caption, description, externalProgress);
        }

        /// <summary>
        /// Runs a specified action and shows a dialog with a cancel button, if the action takes longer than 100msec
        /// </summary>
        /// <param name="action">The action</param>
        /// <param name="externalProgress">Used when integrated in "outer" context</param>
        /// <returns>true if successful</returns>
        public bool RunWithCancelDialog(Action<TrackProgressParameters> action, TrackProgressParameters externalProgress = null)
        {
            return RunWithCancelDialog((parameters) => { action(parameters); return true; }, externalProgress);
        }

        /// <summary>
        /// Enqueues a given action. Will be executed the next time the UI updating thread
        /// runs.
        /// </summary>
        /// <param name="action">The action to post to the UI thread</param>
        public void DelayedPost(Func<bool> action)
        {
            if (_cancelMode)
                return;

            lock (_locker)
            {
                _delayedPostActions.Enqueue(action);
            }
        }

        /// <summary>
        /// Posts an action to the UI thread immediately.
        /// </summary>
        /// <param name="action">The action to post to the UI thread</param>
        public void Post(Func<bool> action)
        {
            if (_cancelMode)
                return;

            _uiContext.Post(
                (o) =>
                {
                    if (action() == false)
                        DelayedPost(action);
                }, null);
        }

        /// <summary>
        /// Sends an action to the UI thread immediately.
        /// </summary>
        /// <param name="action">The action to send to the UI thread</param>
        public void Send(Func<bool> action)
        {
            if (_cancelMode)
                return;

            _uiContext.Send(
                (o) =>
                {
                    if (action() == false)
                        DelayedPost(action);
                }, null);
        }

        internal static void DelayedPostIfPossible(Func<bool> action)
        {
            if (Instance != null)
                Instance.DelayedPost(action);
            else
                SynchronizationContext.Current.Post((o) =>
                    {
                        if (!action())
                            DelayedPostIfPossible(action);
                    }, null);
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// Disposes of the object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_progressMonitor != null)
            {
                _progressMonitor.Dispose();
                _progressMonitor = null;
            }
        }
        #endregion
    }
}
