﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sledge.Formats.Tokens;
using Sledge.Formats.Tokens.Readers;

namespace Sledge.Formats.Valve
{
    /// <summary>
    /// Handles serialisation of objects using Valve's common definition format.
    /// </summary>
    public class SerialisedObjectFormatter
    {
        /// <summary>
        /// Singleton instance of a <see cref="SerialisedObjectFormatter"/>.
        /// </summary>
        public static readonly SerialisedObjectFormatter Instance = new SerialisedObjectFormatter();

        /// <summary>
        /// Serialise an array of objects
        /// </summary>
        /// <param name="serializationStream">The stream to serialise into</param>
        /// <param name="objects">The objects to serialise</param>
        public void Serialize(Stream serializationStream, params SerialisedObject[] objects)
        {
            Serialize(serializationStream, objects.AsEnumerable());
        }

        /// <summary>
        /// Serialise an array of objects
        /// </summary>
        /// <param name="serializationStream">The stream to serialise into</param>
        /// <param name="objects">The objects to serialise</param>
        public void Serialize(Stream serializationStream, IEnumerable<SerialisedObject> objects)
        {
            using (var writer = new StreamWriter(serializationStream, Encoding.UTF8, 1024, true))
            {
                foreach (var obj in objects.Where(x => x != null))
                {
                    Print(obj, writer);
                }
            }
        }

        /// <summary>
        /// Deserialise an array of objects from a stream
        /// </summary>
        /// <param name="serializationStream">The stream to deserialise from</param>
        /// <returns>The deserialised objects</returns>
        public IEnumerable<SerialisedObject> Deserialize(Stream serializationStream)
        {
            using (var reader = new StreamReader(serializationStream, Encoding.UTF8, true, 1024, true))
            {
                return Parse(reader).ToList();
            }
        }

        #region Printer

        /// <summary>
        /// Ensure a string doesn't exceed a length limit.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="limit">The length limit</param>
        /// <returns>The string, truncated to the limit if it was exceeded</returns>
        private static string LengthLimit(string str, int limit)
        {
            return str.Length >= limit ? str.Substring(0, limit - 1) : str;
        }

        /// <summary>
        /// Print the structure to a stream
        /// </summary>
        /// <param name="obj">The object to print</param>
        /// <param name="tw">The output stream to write to</param>
        /// <param name="tabs">The number of tabs to indent this value to</param>
        public static void Print(SerialisedObject obj, TextWriter tw, int tabs = 0)
        {
            var preTabStr = new string(' ', tabs * 4);
            var postTabStr = new string(' ', (tabs + 1) * 4);
            tw.Write(preTabStr);
            tw.WriteLine(obj.Name);
            tw.Write(preTabStr);
            tw.WriteLine("{");
            foreach (var kv in obj.Properties)
            {
                tw.Write(postTabStr);
                tw.Write('"');
                tw.Write(LengthLimit(kv.Key, 1024));
                tw.Write('"');
                tw.Write(' ');
                tw.Write('"');
                tw.Write(LengthLimit((kv.Value ?? "").Replace('"', '`'), 1024));
                tw.Write('"');
                tw.WriteLine();
            }
            foreach (var child in obj.Children)
            {
                Print(child, tw, tabs + 1);
            }
            tw.Write(preTabStr);
            tw.WriteLine("}");
        }

        #endregion

        #region Parser

        private static readonly char[] Symbols = {
            Tokens.Symbols.OpenBrace,
            Tokens.Symbols.CloseBrace
        };

        private static readonly Tokeniser Tokeniser;

        static SerialisedObjectFormatter()
        {
            Tokeniser = new Tokeniser(
                new SingleLineCommentTokenReader(),
                new StringTokenReader(),
                new SymbolTokenReader(Symbols),
                new NameTokenReader(IsValidNameCharacter, IsValidNameCharacter)
            );
        }

        private static bool IsValidNameCharacter(char c)
        {
            return c != '"' && c != '{' && c != '}' && !char.IsWhiteSpace(c) && !char.IsControl(c);
        }

        /// <summary>
        /// Parse a structure from a stream
        /// </summary>
        /// <param name="reader">The TextReader to parse from</param>
        /// <returns>The parsed structure</returns>
        public static IEnumerable<SerialisedObject> Parse(TextReader reader)
        {
            SerialisedObject current = null;
            var stack = new Stack<SerialisedObject>();

            var tokens = Tokeniser.Tokenise(reader);
            using (var it = tokens.GetEnumerator())
            {
                while (it.MoveNext())
                {
                    var t = it.Current;
                    switch (t?.Type)
                    {
                        case TokenType.Invalid:
                            throw new TokenParsingException(t, $"Invalid token:` {t.Value}");
                        case TokenType.Symbol:
                            if (t.Symbol == Tokens.Symbols.OpenBrace)
                            {
                                throw new TokenParsingException(t, "Structure must have a name");
                            }
                            else if (t.Symbol == Tokens.Symbols.CloseBrace)
                            {
                                if (current == null) throw new TokenParsingException(t, "No structure to close");
                                if (stack.Count == 0)
                                {
                                    yield return current;
                                    current = null;
                                }
                                else
                                {
                                    var prev = stack.Pop();
                                    prev.Children.Add(current);
                                    current = prev;
                                }

                                break;
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                        case TokenType.String:
                        case TokenType.Name:
                            if (!it.MoveNext() || it.Current == null)
                            {
                                throw new TokenParsingException(t, "Unexpected end of file");
                            }

                            if (it.Current.Type == TokenType.Symbol && it.Current.Symbol == Tokens.Symbols.OpenBrace)
                            {
                                var next = new SerialisedObject(t.Value);
                                if (current == null)
                                {
                                    current = next;
                                }
                                else
                                {
                                    stack.Push(current);
                                    current = next;
                                }

                                break;
                            }
                            else if (it.Current.Type == TokenType.String || it.Current.Type == TokenType.Name)
                            {
                                if (current == null) throw new TokenParsingException(t, "No structure to add key/values to");

                                var key = t.Value;
                                var value = it.Current.Value;
                                current.Properties.Add(new KeyValuePair<string, string>(key, value));

                                break;
                            }
                            else
                            {
                                throw new TokenParsingException(t, "Expected string value or open brace to follow string key");
                            }
                        case TokenType.End:
                            if (current != null) throw new TokenParsingException(t, "Unterminated structure at end of file");
                            yield break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
        #endregion
    }
}