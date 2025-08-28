#pragma warning disable CS0162
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SFramework.Core.Runtime
{
    public abstract class SFrameworkSettings<T>
    {
        private static T _instance;

        public static bool TryGetInstance(out T instance)
        {
#if UNITY_EDITOR

            var filePath = Path.Combine(Application.dataPath, $"SFramework/Settings/{typeof(T).Name}.json");
            
            // Ensure the directory exists
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, JsonUtility.ToJson(Activator.CreateInstance<T>(), true), Encoding.UTF8);
                UnityEditor.AssetDatabase.Refresh();
                instance = default;
                return false;
            }

            _instance = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
            instance = _instance;
            return true;
#else
  
instance = default;
return false;

#endif
        }
    }
}