using System.Collections.Generic;
using Sledge.Formats.Map.Objects;
using UnityEngine;

namespace QuakeMapVisualization
{
    public class QuakeEntityComponent : MonoBehaviour
    {
        public string                     ClassName  { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }
        
        public void Initialize(Entity entity)
        {
            ClassName  = entity.ClassName;
            Properties = new Dictionary<string, string>(entity.Properties);
        }
        
        // Add helper methods as needed
        public string GetProperty(string key, string defaultValue = "")
        {
            return Properties.TryGetValue(key, out string value) ? value : defaultValue;
        }
    }
}