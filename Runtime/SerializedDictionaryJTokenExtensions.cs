using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace UnityEssentials
{
    /// <summary>
    /// PlayerPrefs-like helpers for <see cref="SerializedDictionary{TKey,TValue}"/> when used as
    /// <c>SerializedDictionary&lt;string, JToken&gt;</c>.
    /// </summary>
    public static class SerializedDictionaryJTokenExtensions
    {
        public static bool HasKey(this SerializedDictionary<string, JToken> dict, string key) =>
            dict != null && !string.IsNullOrWhiteSpace(key) && dict.ContainsKey(key);

        public static void DeleteKey(this SerializedDictionary<string, JToken> dict, string key)
        {
            if (dict == null) return;
            if (string.IsNullOrWhiteSpace(key)) return;
            dict.Remove(key);
        }

        public static void DeleteAll(this SerializedDictionary<string, JToken> dict) => dict?.Clear();

        public static void SetString(this SerializedDictionary<string, JToken> dict, string key, string value) => dict.Set(key, value);
        public static void SetInt(this SerializedDictionary<string, JToken> dict, string key, int value) => dict.Set(key, value);
        public static void SetFloat(this SerializedDictionary<string, JToken> dict, string key, float value) => dict.Set(key, value);
        public static void SetBool(this SerializedDictionary<string, JToken> dict, string key, bool value) => dict.Set(key, value);

        public static string GetString(this SerializedDictionary<string, JToken> dict, string key, string defaultValue = "") => dict.Get(key, defaultValue);
        public static int GetInt(this SerializedDictionary<string, JToken> dict, string key, int defaultValue = 0) => dict.Get(key, defaultValue);
        public static float GetFloat(this SerializedDictionary<string, JToken> dict, string key, float defaultValue = 0f) => dict.Get(key, defaultValue);
        public static bool GetBool(this SerializedDictionary<string, JToken> dict, string key, bool defaultValue = false) => dict.Get(key, defaultValue);

        public static void Set<T>(this SerializedDictionary<string, JToken> dict, string key, T value)
        {
            if (dict == null) return;
            if (string.IsNullOrWhiteSpace(key)) return;

            if (value == null)
            {
                dict.DeleteKey(key);
                return;
            }

            dict[key] = value is JToken jt ? jt : JToken.FromObject(value);
        }

        public static T Get<T>(this SerializedDictionary<string, JToken> dict, string key, T defaultValue = default)
        {
            if (dict == null) return defaultValue;
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;
            if (!dict.TryGetValue(key, out var token) || token == null) return defaultValue;

            try
            {
                if (token is JValue jv)
                {
                    var raw = jv.Value;
                    if (raw is T t) return t;

                    if (raw is IConvertible && typeof(T) != typeof(object))
                        return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);

                    if (typeof(T).IsEnum)
                    {
                        if (raw is string s && Enum.TryParse(typeof(T), s, ignoreCase: true, out var e))
                            return (T)e;
                        if (raw is IConvertible)
                            return (T)Enum.ToObject(typeof(T), raw);
                    }
                }

                return token.ToObject<T>();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
