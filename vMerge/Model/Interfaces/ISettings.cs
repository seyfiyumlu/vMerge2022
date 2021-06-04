using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace alexbegh.vMerge.Model.Interfaces
{
    public interface ISettingsChangeListener
    {
        void SettingsChanged(string key, object data);
    }

    public interface ISettings
    {
        void SetSettings(string key, object data);
        T_Item FetchSettings<T_Item>(string key);
        bool CheckSettingsExist(string key);

        void LoadSettings(string source);
        void SaveSettings(string destination);
        IEnumerable<string> GetAvailableSettings();

        void AddChangeListener(string key, ISettingsChangeListener listener);

        void SetDirty();
        void SetAutoSave(string path, int milliseconds);
    }
}
