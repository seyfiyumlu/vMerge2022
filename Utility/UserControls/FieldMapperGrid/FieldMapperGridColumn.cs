using alexbegh.Utility.Helpers.ExtensionMethods;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.SerializationHelpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace alexbegh.Utility.UserControls.FieldMapperGrid
{
    /// <summary>
    /// This class encapsulates a column configuration of a FieldMapperGridViewModel
    /// </summary>
    [Serializable]
    [RegisterForSerialization]
    [DebuggerDisplay("Header = {_header}, Visible = {_visible}")]
    public class FieldMapperGridColumn : NotifyPropertyChangedImpl
    {
        #region Static Constructor
        static FieldMapperGridColumn()
        {
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs this instance
        /// </summary>
        public FieldMapperGridColumn()
            : base(typeof(FieldMapperGridColumn))
        {
            _type = typeof(string);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The column header
        /// </summary>
        [XmlIgnore]
        public IFieldMapperGridViewModel Parent { get; set; }

        /// <summary>
        /// The column header
        /// </summary>
        [NonSerialized]
        private string _header;

        /// <summary>
        /// The column header property
        /// </summary>
        public string Header
        {
            get { return _header; }
            set { Set(ref _header, value); }
        }

        /// <summary>
        /// The column field (used as a parameter to the property accessor, <see cref="IPropertyAccessor{T}"/>
        /// </summary>
        [NonSerialized]
        private string _column;

        /// <summary>
        /// The column field property
        /// </summary>
        public string Column
        {
            get { return _column; }
            set { Set(ref _column, value); }
        }

        /// <summary>
        /// The column width
        /// </summary>
        [NonSerialized]
        private double _width;

        /// <summary>
        /// The column width property
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { Set(ref _width, value, () => { if (Parent!=null) Parent.RaiseColumnSettingsChanged(); }); }
        }

        /// <summary>
        /// Is this column selected?
        /// </summary>
        [NonSerialized]
        private bool _visible;

        /// <summary>
        /// The visible property
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set
            {
                Set(ref _visible, value);
            }
        }

        /// <summary>
        /// The type of the column's content
        /// </summary>
        [NonSerialized]
        private Type _type;

        /// <summary>
        /// The type of the column's content
        /// </summary>
        [XmlIgnore]
        public Type Type
        {
            get { return _type; }
            set { Set(ref _type, value); }
        }

        /// <summary>
        /// Just used for serialization
        /// </summary>
        [XmlElement("Type")]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public string AssemblyQualifiedTypeName
        {
            get { return Type == null ? null : Type.AssemblyQualifiedName; }
            set { Type = string.IsNullOrEmpty(value) ? null : TypeExtensions.GetTypeEx(value); }
        }

        /// <summary>
        /// Internal view-related properties
        /// </summary>
        [NonSerialized]
        private object _internalProperties;

        /// <summary>
        /// Other view-related properties
        /// </summary>
        public object InternalProperties
        {
            get { return _internalProperties; }
            set { Set(ref _internalProperties, value); }
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Returns the has code; forwards to the header string
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return Header.GetHashCode();
        }
        #endregion
    }

}
