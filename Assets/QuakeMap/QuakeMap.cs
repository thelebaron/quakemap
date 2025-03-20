using System;
using System.Collections.Generic;
using System.IO;
using ScriptsSandbox.QuakeMap;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;
using UnityEngine;

namespace DefaultNamespace
{
    [ExecuteAlways]
    public class QuakeMap : MonoBehaviour
    {
        public string  mapPath = "Assets/Maps/rotated.map";
        public string  outPath = "Assets/Maps/output.map";
        public MapFile Map;
        public Material material;

        private void OnEnable()
        {
            LoadMapFile();
        }

        [ContextMenu("Load")]
        public void LoadMapFile()
        {
            if (string.IsNullOrEmpty(mapPath))
                return;
                
            var quakeMapFormat = new QuakeMapFormat();
            using (var stream = File.OpenRead(mapPath))
            {
                Map = quakeMapFormat.Read(stream);
            }
            
            Debug.Log($"Loaded {mapPath} with {Map.Worldspawn.Children.Count} objects");
        }

        [ContextMenu("Save")]
        public void SaveMapFile()
        {
            // Save the map file
            var qmf = new QuakeMapFormat();
            using (var stream = File.Create(outPath))
            {
                qmf.Write(stream, Map, "");
            }
        }

        [ContextMenu("TB convert")]
        void DoTbStiff()
        {
            var list   = new List<Solid>();
            foreach (var child in Map.Worldspawn.Children)
            {
                if(child is Sledge.Formats.Map.Objects.Solid solid)
                    list.Add(solid);
            }
            Debug.Log($"Have {list.Count} solids");
            Tb.getMapfileData(list, material);

        }
    }
}