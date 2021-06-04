using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace alexbegh.Utility.Managers.View
{
    /// <summary>
    ///  Arguments for the ViewModelFinished event
    /// </summary>
    public class ViewModelFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// Successfully finished?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">ViewModel finished successfully?</param>
        public ViewModelFinishedEventArgs(bool success)
        {
            Success = success;
        }
    }

    /// <summary>
    /// This interface states that a given viewmodel is finishable.
    /// It requires an event "Finished" raised by the viewmodel
    /// when done.
    /// </summary>
    public interface IViewModelIsFinishable
    {
        /// <summary>
        /// The event raised when the viewmodel is in a finished state
        /// </summary>
        event EventHandler<ViewModelFinishedEventArgs> Finished;

        /// <summary>
        /// Raise the finished event
        /// </summary>
        /// <param name="success">ViewModel finished successfully?</param>
        void RaiseFinished(bool success);
    }

    /// <summary>
    /// A class returned by the ViewManagers CreateViewFor method.
    /// Provides access to the underlying WPF View and
    /// provides a way to attach an action when the ViewModel behind
    /// the view is finished (or when the view has been closed).
    /// </summary>
    public class AbstractView
    {
        #region Private Fields
        /// <summary>
        /// Is the "Finished" event attached?
        /// </summary>
        private bool _isAttached;

        /// <summary>
        /// true if the window has already been closed
        /// </summary>
        private bool _isClosed;

        /// <summary>
        /// The event being raised when the view/viewmodel is finished
        /// </summary>
        private event EventHandler _finished;
        #endregion

        #region Public Properties
        /// <summary>
        /// The underlying WPF view
        /// </summary>
        public ContentControl View { get; private set; }

        /// <summary>
        /// Does this view/viewmodel pair support the "Finished" event?
        /// </summary>
        public bool SupportsFinishEvent
        {
            get
            {
                if (View is Window)
                {
                    return true;
                }
                if (View.DataContext is IViewModelIsFinishable)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// The event being raised when the view/viewmodel is finished
        /// Only supported when either the View is a Window or the ViewModel
        /// supports IViewModelIsFinishable.
        /// </summary>
        public event EventHandler Finished
        {
            add
            {
                CheckAttached();
                _finished += value;
            }
            remove
            {
                _finished -= value;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Make sure we are attached to the Finished event of the underlying
        /// view/viewmodel.
        /// </summary>
        private void CheckAttached()
        {
            if (_isAttached)
                return;

            bool foundHandler = false;
            if (View is Window)
            {
                (View as Window).Closed += RaiseFinished;
                foundHandler = true;
            }
            if (View.DataContext is IViewModelIsFinishable)
            {
                (View.DataContext as IViewModelIsFinishable).Finished += RaiseFinished;
                foundHandler = true;
            }
            if (foundHandler)
            {
                _isAttached = true;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Raise the "Finished" event
        /// </summary>
        public void RaiseFinished(object sender, EventArgs args)
        {
            if (_isClosed)
                return;

            _isClosed = true;
            if (sender is Window)
            {
                ((Window)sender).Closed -= RaiseFinished;
            }
            if (View is Window)
            {
                (View as Window).Close();
            }
            if (sender is IViewModelIsFinishable)
            {
                ((IViewModelIsFinishable)sender).Finished -= RaiseFinished;
            }
            if (_finished != null)
                _finished(View.DataContext, null);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs an instance
        /// </summary>
        /// <param name="view">The view to contain</param>
        public AbstractView(ContentControl view)
        {
            View = view;
            CheckAttached();
        }
        #endregion
    }
}
