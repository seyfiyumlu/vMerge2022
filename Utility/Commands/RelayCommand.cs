using System;
using System.Diagnostics;
using System.Windows.Input;
using alexbegh.Utility.Helpers.Logging;
using System.Runtime.CompilerServices;

namespace alexbegh.Utility.Commands
{
    /// <summary>
    /// This class provides the functionality to wrap
    /// an action delegate and a "is enabled" function.
    /// Use for easy binding of WPF commands to methods
    /// in a ViewModel.
    /// </summary>
    public class RelayCommand : ICommand 
    { 
        #region Fields 
        readonly Action<object> _execute; 
        readonly Predicate<object> _canExecute;
        readonly string _filePath;
        readonly int _lineNo;
        #endregion // Fields 

        #region Constructors 
        /// <summary>
        /// Constructs a RelayCommand with only an action
        /// </summary>
        /// <param name="execute">The action</param>
        /// <param name="filePath">The file name of the compile unit instancing this command</param>
        /// <param name="lineNo">The line no. within the compile unit instancing this command</param>
        public RelayCommand(Action<object> execute, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNo = -1) : this(execute, null, filePath, lineNo) 
        { 
        } 
        
        /// <summary>
        /// Constructs a RelayCommand with an action and "canExecute" function
        /// </summary>
        /// <param name="execute">The action</param>
        /// <param name="canExecute">The "canExecute" function</param>
        /// <param name="filePath">The file name of the compile unit instancing this command</param>
        /// <param name="lineNo">The line no. within the compile unit instancing this command</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNo = -1) 
        { 
            if (execute == null) 
                throw new ArgumentNullException("execute"); 
            _execute = execute; 
            _canExecute = canExecute;
            _filePath = filePath;
            _lineNo = lineNo;
        } 
        #endregion // Constructors
        
        #region ICommand Members 
        /// <summary>
        /// The CanExecute wrapper method
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <returns>true if enabled</returns>
        [DebuggerStepThrough] 
        public bool CanExecute(object parameter) 
        {
            return _canExecute == null 
                   ? true 
                   : _canExecute(parameter); 
        } 
        
        /// <summary>
        /// Event being called when CanExecute changed
        /// </summary>
        public event EventHandler CanExecuteChanged 
        { 
            add 
            { 
                CommandManager.RequerySuggested += value; 
            } 
            remove 
            { 
                CommandManager.RequerySuggested -= value; 
            } 
        } 
        
        /// <summary>
        /// The Execute wrapper method
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter) 
        {
            try
            {
                SimpleLogger.Checkpoint("Executing command {0}/{1}", _filePath, _lineNo);
                _execute(parameter);
            }
            catch (Exception ex)
            {
                SimpleLogger.Log(ex);
            }
        } 
        
        #endregion // ICommand Members 
    }
}
