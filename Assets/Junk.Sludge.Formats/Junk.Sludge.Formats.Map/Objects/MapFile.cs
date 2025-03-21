using System;
using System.Collections.Generic;
using Junk.Sludge.Formats.Valve;
using Unity.Mathematics;

namespace Junk.Sludge.Formats.Map.Objects
{
    [Serializable]
    public class MapFile
    {
        public Worldspawn               Worldspawn;
        public List<Visgroup>           Visgroups;
        public List<Path>               Paths;
        public List<Camera>             Cameras;
        public List<SerialisedObject>   AdditionalObjects;
        public (float3 min, float3 max) CordonBounds;
        public List<BackgroundImage>    BackgroundImages;

        public MapFile()
        {
            Worldspawn = new Worldspawn();
            Visgroups = new List<Visgroup>();
            Paths = new List<Path>();
            Cameras = new List<Camera>();
            AdditionalObjects = new List<SerialisedObject>();
            CordonBounds = (float3.zero, float3.zero);
            BackgroundImages = new List<BackgroundImage>();
        }
    }
}