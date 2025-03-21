﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;


namespace Junk.Sludge.Formats.Map.Objects
{
    
    [Serializable]
    public class Entity : MapObject
    {
        public string                             ClassName;
        public int                                SpawnFlags;
        public List<KeyValuePair<string, string>> SortedProperties;
        public IDictionary<string, string>        Properties;

        public Entity()
        {
            SortedProperties = new List<KeyValuePair<string, string>>();
            Properties = new SortedKeyValueDictionaryWrapper(SortedProperties);
        }

        public int GetIntProperty(string key, int defaultValue) => GetProperty(key, defaultValue);
        public float GetFloatProperty(string key, float defaultValue) => GetProperty(key, defaultValue);
        public decimal GetDecimalProperty(string key, decimal defaultValue) => GetProperty(key, defaultValue);
        public string GetStringProperty(string key, string defaultValue) => GetProperty(key, defaultValue);

        public float3 GetVectorProperty(string key, float3 defaultValue)
        {
            if (!Properties.ContainsKey(key)) return defaultValue;

            var value = Properties[key];
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            var spl = value.Split(' ');
            if (spl.Length != 3) return defaultValue;

            if (!float.TryParse(spl[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return defaultValue;
            if (!float.TryParse(spl[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return defaultValue;
            if (!float.TryParse(spl[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) return defaultValue;

            return new float3(x, y, z);
        }

        public float4 GetVector4Property(string key, float4 defaultValue)
        {
            if (!Properties.ContainsKey(key)) return defaultValue;

            var value = Properties[key];
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            var spl = value.Split(' ');
            if (spl.Length != 4) return defaultValue;

            if (!float.TryParse(spl[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)) return defaultValue;
            if (!float.TryParse(spl[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) return defaultValue;
            if (!float.TryParse(spl[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) return defaultValue;
            if (!float.TryParse(spl[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var w)) return defaultValue;

            return new float4(x, y, z, w);
        }

        public T GetProperty<T>(string key, T defaultValue)
        {
            if (!Properties.ContainsKey(key)) return defaultValue;

            var value = Properties[key];
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}