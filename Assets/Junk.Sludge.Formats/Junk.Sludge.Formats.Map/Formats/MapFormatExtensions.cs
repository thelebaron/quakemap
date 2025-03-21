using System.IO;
using Junk.Sludge.Formats.Map.Objects;

namespace Junk.Sludge.Formats.Map.Formats
{
    public static class MapFormatExtensions
    {
        public static MapFile ReadFromFile(this IMapFormat mapFormat, string fileName)
        {
            using (var fo = File.OpenRead(fileName))
            {
                return mapFormat.Read(fo);
            }
        }
    }
}
