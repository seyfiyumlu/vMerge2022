using Microsoft.TeamFoundation.VersionControl.Client;
using alexbegh.Utility.Helpers.NotifyPropertyChanged;
using alexbegh.Utility.UserControls.FieldMapperGrid;
using alexbegh.vMerge.ViewModel.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace alexbegh.vMerge.ViewModel.Changesets
{
    class ReflectedPublicPropertyAccessor<T_Type> : IPropertyAccessor<T_Type>
    {
        private static Dictionary<string, PropertyInfo> _propInfos;
        private static Dictionary<string, PropertyInfo> PropInfos
        {
            get
            {
                if( _propInfos!=null )
                    return _propInfos;

                Type t = typeof(T_Type);
                _propInfos = new Dictionary<string, PropertyInfo>(
                    t.GetProperties()
                    .Where(item => 
                        item.CanRead && item.GetGetMethod().IsPublic 
                        && (item.GetGetMethod().ReturnType.IsValueType
                            || item.GetGetMethod().ReturnType==typeof(string)))
                    .GroupBy(item => item.Name).Select(item => item.First())
                    .ToDictionary(
                    item => item.Name,
                    item => item));
                return _propInfos;
            }
        }

        public IEnumerable<string> AllFieldsOf(T_Type source)
        {
            return PropInfos.Keys.AsEnumerable();
        }

        public Type GetType(T_Type source, string fieldName)
        {
            return PropInfos[fieldName].GetGetMethod().ReturnType;
        }

        public object GetValue(T_Type source, string fieldName)
        {
            return
                PropInfos[fieldName]
                .GetGetMethod().Invoke(source, null);
        }

        public void SetValue(T_Type source, string fieldName, object value)
        {
            //throw new NotImplementedException();
        }
    }

    class ChangesetPropertyAccessor : NotifyPropertyChangedImpl, IPropertyAccessor<TfsChangesetWrapper>
    {
        private ReflectedPublicPropertyAccessor<Changeset> accessor = new ReflectedPublicPropertyAccessor<Changeset>();

        public ChangesetPropertyAccessor()
            : base(typeof(ChangesetPropertyAccessor))
        {
        }

        public IEnumerable<string> AllFieldsOf(TfsChangesetWrapper source)
        {
            yield return "IsSelected";
            foreach (var item in accessor.AllFieldsOf(source.TfsChangeset.Changeset))
                yield return item;
        }

        public Type GetType(TfsChangesetWrapper source, string fieldName)
        {
            switch (fieldName)
            {
                case "IsSelected":
                    return typeof(bool);
                case "IsHighlighted":
                    return typeof(bool);
                case "HasWarning":
                    return typeof(bool);
                case "WarningText":
                    return typeof(string);
                case "FontWeight":
                    return typeof(string);
                case "Comment":
                    return typeof(MultiLineString);
                default:
                    return accessor.GetType(source.TfsChangeset.Changeset, fieldName);
            }
        }

        public object GetValue(TfsChangesetWrapper source, string fieldName)
        {
            switch (fieldName)
            {
                case "IsSelected":
                    return source.IsSelected;
                case "IsHighlighted":
                    return source.IsHighlighted;
                case "HasWarning":
                    return source.HasWarning;
                case "WarningText":
                    return source.WarningText;
                case "FontWeight":
                    return source.FontWeight;
                case "Comment":
                    return new MultiLineString(source.TfsChangeset.Changeset.Comment);
                default:
                    return accessor.GetValue(source.TfsChangeset.Changeset, fieldName);
            }
        }

        public void SetValue(TfsChangesetWrapper source, string fieldName, object value)
        {
            switch (fieldName)
            {
                case "IsSelected":
                    if (source.IsSelected != (bool)value)
                    {
                        source.IsSelected = (bool)value;
                        RaisePropertyChanged("IsSelected");
                    }
                    break;
            }
        }
    }
}
