using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using alexbegh.vMerge.ViewModel.Wrappers;
using System;
using alexbegh.Utility.Helpers.Logging;

namespace alexbegh.vMerge.ViewModel.WorkItems
{
    class WorkItemPropertyAccessor
        : alexbegh.Utility.UserControls.FieldMapperGrid.IPropertyAccessor<TfsWorkItemWrapper>
    {
        public IEnumerable<string> AllFieldsOf(TfsWorkItemWrapper source)
        {
            yield return "IsSelected";
            foreach (Field field in source.TfsWorkItem.WorkItem.Fields)
            {
                yield return field.Name;
            }
        }

        public Type GetType(TfsWorkItemWrapper source, string fieldName)
        {
            switch (fieldName)
            {
                case "IsSelected":
                    return typeof(bool);
                case "HasWarning":
                    return typeof(bool);
                case "WarningText":
                    return typeof(string);
                case "IsHighlighted":
                    return typeof(bool);
                case "FontWeight":
                    return typeof(string);
                default:
                    if (source.TfsWorkItem.WorkItem.Fields.Contains(fieldName))
                    {
                        switch (source.TfsWorkItem.WorkItem.Fields[fieldName].FieldDefinition.FieldType)
                        {
                            case FieldType.Boolean:
                                return typeof(bool);
                            case FieldType.DateTime:
                                return typeof(DateTime);
                            case FieldType.Double:
                                return typeof(double);
                            case FieldType.Guid:
                                return typeof(Guid);
                            case FieldType.History:
                                return typeof(MultiLineString);
                            case FieldType.Html:
                                return typeof(MultiLineString);
                            case FieldType.Integer:
                                return typeof(int);
                            case FieldType.Internal:
                                return typeof(string);
                            case FieldType.PlainText:
                                return typeof(string);
                            case FieldType.String:
                                return typeof(string);
                            case FieldType.TreePath:
                                return typeof(string);
                            default:
                                SimpleLogger.Log(SimpleLogLevel.Info, "Unknown field type found: {0}/{1}", source.TfsWorkItem.WorkItem.Fields[fieldName].FieldDefinition.FieldType, fieldName);
                                return typeof(string);
                        }
                    }
                    else
                        return null;
            }
        }

        public static string StripHTML(string HTMLText)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            return WebUtility.HtmlDecode(reg.Replace(HTMLText, ""));
        }

        public object GetValue(TfsWorkItemWrapper source, string fieldName)
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
                default:
                    if (source.TfsWorkItem.WorkItem.Fields.Contains(fieldName))
                    {
                        var value = source.TfsWorkItem.WorkItem.Fields[fieldName].Value;
                        switch (source.TfsWorkItem.WorkItem.Fields[fieldName].FieldDefinition.FieldType)
                        {
                            case FieldType.History:
                                return new MultiLineString(StripHTML((string)value));
                            case FieldType.PlainText:
                                return new MultiLineString((string)value);
                            case FieldType.Html:
                                return new MultiLineString(StripHTML((string)value));
                            case FieldType.String:
                                return (string)value;
                            default:
                                if (value is string)
                                    return StripHTML((string)value);
                                else
                                    return value;
                        }
                    }
                    else
                        return null;
            }
        }

        public void SetValue(TfsWorkItemWrapper source, string fieldName, object value)
        {
            switch (fieldName)
            {
                case "IsSelected":
                    source.IsSelected = (bool)value;
                    break;
            }
        }
    }
}
