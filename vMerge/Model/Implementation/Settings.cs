using alexbegh.Utility.Helpers.Logging;
using alexbegh.Utility.Helpers.WeakReference;
using alexbegh.Utility.SerializationHelpers;
using alexbegh.vMerge.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace alexbegh.vMerge.Model.Implementation
{
    /// <summary>
    /// This class provides the settings for the package.
    /// See SetSettings, FetchSettings, Load/SaveConfiguration
    /// </summary>
    [Serializable]
    public class Settings : ISettings
    {
        #region Constructor
        /// <summary>
        /// Constructs an instance
        /// </summary>
        public Settings()
        {
            SerializedSettings = new SerializableDictionary<string, object>();
            Lock = new object();
            ChangeListeners = new Dictionary<string, WeakReferenceList<ISettingsChangeListener>>();
            Serializer.RegisterAssemblyTypes();
        }
        #endregion

        #region Private Properties
        /// <summary>
        /// The lock object
        /// </summary>
        private object Lock
        {
            get;
            set;
        }

        /// <summary>
        /// Remembers the dirty state
        /// </summary>
        private bool IsDirty
        {
            get;
            set;
        }

        /// <summary>
        /// The serialized settings
        /// </summary>
        public SerializableDictionary<string, object> SerializedSettings
        {
            get;
            set;
        }

        /// <summary>
        /// The timer for auto-saving
        /// </summary>
        private Timer AutoSaveTimer
        {
            get;
            set;
        }

        /// <summary>
        /// True while the timer method is executing
        /// </summary>
        private static int TimerIsExecuting;

        /// <summary>
        /// List of change listeners
        /// </summary>
        [XmlIgnore]
        private Dictionary<string, WeakReferenceList<ISettingsChangeListener>> ChangeListeners
        {
            get;
            set;
        }
        #endregion

        #region Public Operations
        /// <summary>
        /// Sets the settings for a given key
        /// </summary>
        /// <param name="key">The key to set</param>
        /// <param name="data">The data</param>
        public void SetSettings(string key, object data)
        {
            lock (Lock)
            {
                IsDirty = true;
                SerializedSettings[key] = data;
            }
            WeakReferenceList<ISettingsChangeListener> changeListeners = null;
            if (ChangeListeners.TryGetValue(key, out changeListeners))
            {
                var notify = changeListeners.CompactAndReturn();
                foreach (var item in notify)
                    item.SettingsChanged(key, data);
            }
            if (ChangeListeners.TryGetValue("", out changeListeners))
            {
                var notify = changeListeners.CompactAndReturn();
                foreach (var item in notify)
                    item.SettingsChanged(key, data);
            }
        }

        /// <summary>
        /// Fetches settings for a given key, returning null if no setting was found
        /// </summary>
        /// <typeparam name="T_Item">The type to cast the result to</typeparam>
        /// <param name="key">The key to fetch the settings for</param>
        /// <returns>null if not found, the object otherwise</returns>
        public T_Item FetchSettings<T_Item>(string key)
        {
            lock (Lock)
            {
                if (!SerializedSettings.ContainsKey(key))
                    return default(T_Item);
                return (T_Item)SerializedSettings[key];
            }
        }

        /// <summary>
        /// Checks if a specific key exists in the settings
        /// </summary>
        /// <param name="key">Key to check</param>
        /// <returns>true if existing</returns>
        public bool CheckSettingsExist(string key)
        {
            lock (Lock)
            {
                return SerializedSettings.ContainsKey(key);
            }
        }

        /// <summary>
        /// Loads the settings from a given source path
        /// </summary>
        /// <param name="source">Source file name</param>
        public void LoadSettings(string name)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "vMerge",
                name + ".qvmset");

            if (!File.Exists(path))
                return;

            try
            {
                lock (Lock)
                {
                    SerializableDictionary<string, object> serializedSettings;
                    Serializer.XmlDeserialize(path, out serializedSettings);
                    SerializedSettings = serializedSettings;
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        /// <summary>
        /// Saves the settings to a given destination path
        /// </summary>
        /// <param name="destination">Destination path</param>
        public void SaveSettings(string name)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vMerge", name + ".qvmset");
            string pathBak = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vMerge", name + ".bak.qvmset");
            lock (Lock)
            {
                if (SerializedSettings == null)
                    return;

                if (File.Exists(path))
                    File.Copy(path, pathBak, true);

                try
                {
                    Serializer.XmlSerialize(SerializedSettings, path);
                    //Serializer.JsonSerialize<SerializableDictionary<string, object>>(SerializedSettings, path);
                }
                catch (Exception ex)
                {
                    
                    SimpleLogger.Log(SimpleLogLevel.Error, ex.ToString());
                }
            }
        }

        /// <summary>
        /// Returns all available setting files
        /// </summary>
        /// <returns>List of setting files</returns>
        public IEnumerable<string> GetAvailableSettings()
        {
            foreach (var file in Directory.EnumerateFiles(
                                    Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "vMerge"),
                                    "*.qvmset", SearchOption.TopDirectoryOnly))
            {
                yield return Path.GetFileNameWithoutExtension(file);
            }
        }

        public void AddChangeListener(string key, ISettingsChangeListener listener)
        {
            if (key == null)
                key = "";
            WeakReferenceList<ISettingsChangeListener> existing = null;
            if (!ChangeListeners.TryGetValue(key, out existing))
            {
                ChangeListeners[key] = existing = new WeakReferenceList<ISettingsChangeListener>();
            }
            existing.Add(listener);
        }

        /// <summary>
        /// Sets the state to dirty
        /// </summary>
        public void SetDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Activates auto-saving to a certain location with a specified delay
        /// </summary>
        /// <param name="path">The target path</param>
        /// <param name="milliseconds">The delay in milliseconds</param>
        public void SetAutoSave(string name, int milliseconds)
        {
            if (AutoSaveTimer != null)
            {
                AutoSaveTimer.Dispose();
            }
            AutoSaveTimer = new Timer(
                (o) =>
                {
                    if (Interlocked.CompareExchange(ref TimerIsExecuting, 1, 0) == 0)
                    {
                        try
                        {
                            if (IsDirty)
                            {
                                try
                                {
                                    SaveSettings(name);
                                    IsDirty = false;
                                }
                                catch (Exception ex)
                                {
                                    SimpleLogger.Log(SimpleLogLevel.Error,ex.ToString());
                                }
                            }
                        }
                        finally
                        {
                            TimerIsExecuting = 0;
                        }
                    }
                }, null,
                    TimeSpan.FromMilliseconds(0),
                    TimeSpan.FromMilliseconds(milliseconds));
        }
        #endregion
    }
}
