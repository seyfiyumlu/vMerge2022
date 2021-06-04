using alexbegh.Utility.Commands;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.Managers.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.Utility.Helpers.ViewModel
{
    /// <summary>
    /// ViewModel for MessageBoxView
    /// </summary>
    public class MessageBoxViewModel : NotifyPropertyChangedImpl, IViewModelIsFinishable
    {
        /// <summary>
        /// The default set of buttons
        /// </summary>
        [Flags]
        public enum MessageBoxButtons : uint
        {
            /// <summary>
            /// Don't provide a default set of buttons
            /// </summary>
            None = 0,

            /// <summary>
            /// Provide an OK button
            /// </summary>
            OK = 1,

            /// <summary>
            /// Provide a cancel button
            /// </summary>
            Cancel = 2,

            /// <summary>
            /// Provide a "not now" button
            /// </summary>
            Later = 4
        }

        /// <summary>
        /// MessageBoxButton, can be specified externally (see ToggleButtons, ConfirmButtons, OptionButtons)
        /// </summary>
        public class MessageBoxButton : NotifyPropertyChangedImpl
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="text">Button text</param>
            public MessageBoxButton(string text) : base(typeof(MessageBoxButton)) { Text = text; }

            /// <summary>
            /// Button text
            /// </summary>
            public string Text { get { return _text; } set { Set(ref _text, value); } }
            private string _text;

            /// <summary>
            /// Button is checked?
            /// </summary>
            public bool IsChecked { get { return _isChecked; } set { Set(ref _isChecked, value); } }
            private bool _isChecked;

            /// <summary>
            /// Button is a cancel button?
            /// </summary>
            public bool IsCancel { get { return _isCancel; } set { Set(ref _isCancel, value); } }
            private bool _isCancel;

            /// <summary>
            /// Button is a cancel button?
            /// </summary>
            public bool IsDefault { get { return _isDefault; } set { Set(ref _isDefault, value); } }
            private bool _isDefault;
        }

        /// <summary>
        /// Message box caption
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { Set(ref _caption, value); }
        }
        private string _caption;

        /// <summary>
        /// Message box descriptive text
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { Set(ref _description, value); }
        }
        private string _description;

        /// <summary>
        /// Buttons which are displayed as check boxes
        /// </summary>
        public List<MessageBoxButton> ToggleButtons
        {
            get { return _toggleButtons; }
        }
        private List<MessageBoxButton> _toggleButtons;

        /// <summary>
        /// Buttons which close the message box upon clicking
        /// </summary>
        public List<MessageBoxButton> ConfirmButtons
        {
            get { return _confirmButtons; }
        }
        private List<MessageBoxButton> _confirmButtons;

        /// <summary>
        /// Buttons which are checked exclusively (displayed as radio buttons)
        /// </summary>
        public List<MessageBoxButton> OptionButtons
        {
            get { return _optionButtons; }
        }
        private List<MessageBoxButton> _optionButtons;

        /// <summary>
        /// A button has been clicked
        /// </summary>
        public RelayCommand ButtonClickedCommand
        {
            get { return _buttonClickedCommand; }
            set { Set(ref _buttonClickedCommand, value); }
        }
        private RelayCommand _buttonClickedCommand;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="caption">The caption</param>
        /// <param name="description">The description</param>
        /// <param name="buttonSelection">The default button selection</param>
        public MessageBoxViewModel(string caption, string description, MessageBoxButtons buttonSelection = MessageBoxButtons.OK | MessageBoxButtons.Cancel)
            : base(typeof(MessageBoxViewModel))
        {
            _toggleButtons = new List<MessageBoxButton>();
            _confirmButtons = new List<MessageBoxButton>();
            _optionButtons = new List<MessageBoxButton>();

            ButtonClickedCommand = new RelayCommand((o) => ButtonClicked(o as MessageBoxButton), (o) => CanButtonBeClicked(o as MessageBoxButton));

            if (buttonSelection.HasFlag(MessageBoxButtons.OK))
                ConfirmButtons.Add(new MessageBoxButton("OK"));
            if (buttonSelection.HasFlag(MessageBoxButtons.Later))
                ConfirmButtons.Add(new MessageBoxButton("Not now"));
            if (buttonSelection.HasFlag(MessageBoxButtons.Cancel))
                ConfirmButtons.Add(new MessageBoxButton("Cancel"));

            if (ConfirmButtons.Any())
            {
                if (!ConfirmButtons.Any(button => button.IsDefault))
                    ConfirmButtons.First().IsDefault = true;
                if (!ConfirmButtons.Any(button => button.IsCancel))
                    ConfirmButtons.Last().IsCancel = true;
            }

            Caption = caption;
            Description = description;
        }

        private void ButtonClicked(MessageBoxButton button)
        {
            button.IsChecked = true;
            RaiseFinished(true);
        }

        private bool CanButtonBeClicked(MessageBoxButton button)
        {
            return true;
        }

        /// <summary>
        /// Raise ViewModelFinished event
        /// </summary>
        /// <param name="success">Finished successfully?</param>
        public void RaiseFinished(bool success)
        {
            if (Finished != null)
                Finished(this, new ViewModelFinishedEventArgs(success));
        }

        /// <summary>
        /// View model is finished
        /// </summary>
        public event EventHandler<ViewModelFinishedEventArgs> Finished;
    }
}
