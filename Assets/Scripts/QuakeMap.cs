using System.Collections.Generic;
using System.IO;
using Sledge.Formats.Map.Formats;
using Sledge.Formats.Map.Objects;
using UnityEngine;

[ExecuteAlways]
public class QuakeMap : MonoBehaviour
{
    public string   mapPath = "Assets/Maps/rotated.map";
    public string   outPath = "Assets/Maps/output.map";
    public MapFile  Map;
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

    public List<Solid> LoadSolids()
    {
        var list   = new List<Solid>();
        foreach (var child in Map.Worldspawn.Children)
        {
            if(child is Sledge.Formats.Map.Objects.Solid solid)
                list.Add(solid);
        }
        return list;
        //Tb.getMapfileData(list, material);

    }
}