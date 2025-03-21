using System.Collections.Generic;
using Junk.Sludge.Formats.Map.Formats;

namespace Junk.Sludge.Formats.Map
{
    public static class MapFormatFactory
    {
        private static readonly List<IMapFormat> _formats;

        static MapFormatFactory()
        {
            _formats = new List<IMapFormat>
            {
                new QuakeMapFormat()
            };
        }

        public static void Register(IMapFormat loader)
        {
            _formats.Add(loader);
        }
    }
}
