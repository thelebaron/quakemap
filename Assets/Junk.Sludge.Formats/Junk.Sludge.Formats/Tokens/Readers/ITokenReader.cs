using System.IO;

namespace Junk.Sludge.Formats.Tokens.Readers
{
    public interface ITokenReader
    {
        /// <summary>
        /// Read a token. Returns null if no token is valid at this point.
        /// </summary>
        Token Read(char start, TextReader reader);
    }
}
