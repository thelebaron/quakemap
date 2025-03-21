using System;
using System.Collections.Generic;
using UnityEngine;

namespace Junk.Sludge.Formats.Map.Objects
{
    
    [Serializable]
    public abstract class MapObject
    {
        public List<MapObject> Children;
        public List<int>       Visgroups;
        public Color           Color;

        protected MapObject()
        {
            Children = new List<MapObject>();
            Visgroups = new List<int>();
            Color = Color.white;
        }

        public List<MapObject> FindAll()
        {
            return Find(x => true);
        }

        public List<MapObject> Find(Predicate<MapObject> matcher)
        {
            var list = new List<MapObject>();
            FindRecursive(list, matcher);
            return list;
        }

        private void FindRecursive(ICollection<MapObject> items, Predicate<MapObject> matcher)
        {
            var thisMatch = matcher(this);
            if (thisMatch)
            {
                items.Add(this);
            }
            foreach (var mo in Children)
            {
                mo.FindRecursive(items, matcher);
            }
        }
    }
}