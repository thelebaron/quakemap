using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace BSP
{
    public static partial class Bsp
    {
        private static List<brush_t> mapbrushes;
        private static int           nummapbrushes;

        static         int            num_entities;
        private static entity_t       mapent;
        private static List<entity_t> entities = new List<entity_t>();
        private static string         token;
        private static bool           unget;
        private static int            scriptline;
        private static string         script_p;
        private static int            script_index;

        // Constants
        private const int MAXTOKEN         = 128;
        private const int MAX_MAP_ENTITIES = 8192; // Adjust if needed

        // Additional constants needed
        private const int   MAX_KEY         = 32;
        private const int   MAX_VALUE       = 1024;
        private const float EQUAL_EPSILON   = 0.001f;
        private const int   MAX_MAP_TEXINFO = 8192;
        private const int   TEX_SPECIAL     = 1; // Special texture flag

        /// <summary>
        /// Initializes token parsing from a data string
        /// </summary>
        public static void StartTokenParsing(string data)
        {
            scriptline   = 1;
            script_p     = data;
            script_index = 0;
            unget        = false;
        }

        /// <summary>
        /// Marks the current token to be returned on the next GetToken call
        /// </summary>
        public static void UngetToken()
        {
            unget = true;
        }

        /// <summary>
        /// Parses an entity from the token stream
        /// </summary>
        /// <returns>True if an entity was parsed, false if no more entities</returns>
        public static bool ParseEntity()
        {
            // Check for end of entities
            if (!GetToken(true))
                return false;

            // First token should be "{"
            if (token != "{")
                Error($"ParseEntity: {{ not found");

            // Check if we've exceeded the maximum number of entities
            if (num_entities >= MAX_MAP_ENTITIES)
                Error($"num_entities == MAX_MAP_ENTITIES");

            // Setup the entity
            mapent                 = new entity_t();
            entities[num_entities] = mapent;
            num_entities++;

            // Parse until we find the closing brace
            do
            {
                if (!GetToken(true))
                    Error("ParseEntity: EOF without closing brace");

                if (token == "}")
                    break;

                if (token == "{")
                    ParseBrush();
                else
                    ParseEpair();
            } while (true);

            // Extract the origin vector
            GetVectorForKey(mapent, "origin", ref mapent.origin);
            return true;
        }

        /// <summary>
        /// Gets the next token from the input stream
        /// </summary>
        /// <param name="crossline">If true, continues across newlines</param>
        /// <returns>True if a token was read, false if end of stream</returns>
        private static bool GetToken(bool crossline)
        {
            char c;
            int  tokenIndex;

            if (unget)
            {
                unget = false;
                return true;
            }

            // Skip whitespace
            while (script_index < script_p.Length)
            {
                c = script_p[script_index];
                if (c <= ' ')
                {
                    if (c == '\n')
                    {
                        scriptline++;
                        if (!crossline)
                            Error($"Line {scriptline} is incomplete");
                    }

                    script_index++;
                }
                else
                {
                    // Check for comments
                    if (c == '/' && script_index + 1 < script_p.Length && script_p[script_index + 1] == '/')
                    {
                        if (!crossline)
                            Error($"Line {scriptline} is incomplete");

                        // Skip to end of line
                        while (script_index < script_p.Length && script_p[script_index] != '\n')
                            script_index++;

                        if (script_index == script_p.Length)
                        {
                            if (!crossline)
                                Error($"Line {scriptline} is incomplete");
                            return false;
                        }

                        scriptline++;
                        script_index++;
                    }
                    else
                    {
                        break; // Found non-whitespace
                    }
                }
            }

            // Check for end of script
            if (script_index >= script_p.Length)
                return false;

            tokenIndex = 0;

            // Handle quoted strings
            if (script_p[script_index] == '"')
            {
                script_index++; // Skip the quote
                while (script_index < script_p.Length && script_p[script_index] != '"')
                {
                    if (script_index == script_p.Length)
                        Error("EOF inside quoted token");

                    if (tokenIndex >= MAXTOKEN - 1)
                        Error($"Token too large on line {scriptline}");

                    token += script_p[script_index++];
                }

                if (script_index < script_p.Length)
                    script_index++; // Skip closing quote
            }
            else
            {
                // Parse regular token
                token = "";
                while (script_index < script_p.Length && script_p[script_index] > ' ')
                {
                    if (tokenIndex >= MAXTOKEN - 1)
                        Error($"Token too large on line {scriptline}");

                    token += script_p[script_index++];
                }
            }

            return true;
        }

        /// <summary>
        /// Parses a key-value pair and adds it to the current entity
        /// </summary>
        private static void ParseEpair()
        {
            epair_t e = new epair_t();

            if (token.Length >= MAX_KEY - 1)
                Error("ParseEpair: token too long");

            e.key = token;
            GetToken(false);

            if (token.Length >= MAX_VALUE - 1)
                Error("ParseEpair: token too long");

            e.value       = token;
            e.next        = mapent.epairs;
            mapent.epairs = e;
        }

        /// <summary>
        /// Gets a vector value for a key from an entity
        /// </summary>
        private static void GetVectorForKey(entity_t ent, string key, ref float3 vec)
        {
            string val = ValueForKey(ent, key);
            if (string.IsNullOrEmpty(val))
                return;

            string[] parts = val.Split(' ');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                {
                    vec.x = x;
                    vec.y = y;
                    vec.z = z;
                }
            }
        }

        /// <summary>
        /// Gets a string value for a key from an entity
        /// </summary>
        private static string ValueForKey(entity_t ent, string key)
        {
            for (epair_t ep = ent.epairs; ep != null; ep = ep.next)
            {
                if (ep.key == key)
                    return ep.value;
            }

            return "";
        }

        /// <summary>
        /// Parses a brush definition and adds it to the current entity
        /// </summary>
        private static void ParseBrush()
        {
            mbrush_t b = new mbrush_t();

            // Add brush to entity
            b.next         = mapent.brushes;
            mapent.brushes = b;

            do
            {
                if (!GetToken(true))
                    break;
                if (token == "}")
                    break;

                // Read the three point plane definition
                float3[] planepts = new float3[3];
                for (int i = 0; i < 3; i++)
                {
                    if (i != 0)
                        GetToken(true);
                    if (token != "(")
                        Error("ParseBrush: expected '('");

                    for (int j = 0; j < 3; j++)
                    {
                        GetToken(false);
                        planepts[i][j] = float.Parse(token);
                    }

                    GetToken(false);
                    if (token != ")")
                        Error("ParseBrush: expected ')'");
                }

                // Read the texture definition
                GetToken(false);
                string textureName = token;
                int    miptexNum   = FindMiptex(textureName);

                // Read texture parameters
                float[] shift = new float[2];
                float[] scale = new float[2];

                GetToken(false);
                shift[0] = float.Parse(token);
                GetToken(false);
                shift[1] = float.Parse(token);
                GetToken(false);
                float rotate = float.Parse(token);
                GetToken(false);
                scale[0] = float.Parse(token);
                GetToken(false);
                scale[1] = float.Parse(token);

                // Check if this plane already exists on the brush
                bool isDuplicate = false;
                for (mface_t f2 = b.faces; f2 != null; f2 = f2.next)
                {
                    bool allPointsOnPlane = true;
                    for (int i = 0; i < 3; i++)
                    {
                        float d = math.dot(planepts[i], f2.plane.normal) - f2.plane.dist;
                        if (d < -ON_EPSILON || d > ON_EPSILON)
                        {
                            allPointsOnPlane = false;
                            break;
                        }
                    }

                    if (allPointsOnPlane)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (isDuplicate)
                {
                    Console.WriteLine("WARNING: brush with duplicate plane");
                    continue;
                }

                // Create a new face for this brush
                mface_t f = new mface_t();
                f.next  = b.faces;
                b.faces = f;

                // Calculate the plane from the three points
                float3 t1 = planepts[0] - planepts[1];
                float3 t2 = planepts[2] - planepts[1];
                float3 t3 = planepts[1];

                // Calculate normal using cross product
                f.plane.normal = math.normalize(math.cross(t1, t2));

                // Check for degenerate plane
                if (math.length(f.plane.normal) < EQUAL_EPSILON)
                {
                    Console.WriteLine("WARNING: brush plane with no normal");
                    b.faces = f.next;
                    continue;
                }

                // Calculate plane distance
                f.plane.dist = math.dot(t3, f.plane.normal);

                // Calculate texture vectors
                float3[] vecs = new float3[2];
                TextureAxisFromPlane(f.plane, out vecs[0], out vecs[1]);

                // Apply scaling
                if (math.abs(scale[0]) < EQUAL_EPSILON) scale[0] = 1;
                if (math.abs(scale[1]) < EQUAL_EPSILON) scale[1] = 1;

                // Apply rotation
                float sinv, cosv;
                if (math.abs(rotate) < EQUAL_EPSILON)
                {
                    sinv = 0;
                    cosv = 1;
                }
                else if (math.abs(rotate - 90) < EQUAL_EPSILON)
                {
                    sinv = 1;
                    cosv = 0;
                }
                else if (math.abs(rotate - 180) < EQUAL_EPSILON)
                {
                    sinv = 0;
                    cosv = -1;
                }
                else if (math.abs(rotate - 270) < EQUAL_EPSILON)
                {
                    sinv = -1;
                    cosv = 0;
                }
                else
                {
                    float ang = rotate * math.PI / 180.0f;
                    sinv = math.sin(ang);
                    cosv = math.cos(ang);
                }

                // Determine which components to use for texture axes
                int sv = 0, tv = 0;
                if (math.abs(vecs[0].x)      > EQUAL_EPSILON) sv = 0;
                else if (math.abs(vecs[0].y) > EQUAL_EPSILON) sv = 1;
                else sv                                          = 2;

                if (math.abs(vecs[1].x)      > EQUAL_EPSILON) tv = 0;
                else if (math.abs(vecs[1].y) > EQUAL_EPSILON) tv = 1;
                else tv                                          = 2;

                // Apply rotation to texture axes
                for (int i = 0; i < 2; i++)
                {
                    float ns = cosv * vecs[i][sv] - sinv * vecs[i][tv];
                    float nt = sinv * vecs[i][sv] + cosv * vecs[i][tv];
                    vecs[i][sv] = ns;
                    vecs[i][tv] = nt;
                }

                // Create texinfo structure
                texinfo_t tx = new texinfo_t();

                // Scale and assign texture vectors
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        //tx.vecs[i][j] = vecs[i][j] / scale[i];
                    }
                }

                // Apply shifts
                //tx.vecs[0][3] = shift[0];
                //tx.vecs[1][3] = shift[1];
                tx.miptex = miptexNum;

                // Store texinfo index in face
                f.texinfo = FindTexinfo(tx);

                // Set plane type (optional but useful)
                f.plane.type = PlaneTypeForNormal(f.plane.normal);
            } while (true);
        }

        /// <summary>
        /// Calculates the texture axis vectors for a given plane
        /// </summary>
        private static void TextureAxisFromPlane(plane_t plane, out float3 xv, out float3 yv)
        {
            // Initialize output vectors
            xv = new float3();
            yv = new float3();

            // Find the dominant axis of the normal
            float ax = math.abs(plane.normal.x);
            float ay = math.abs(plane.normal.y);
            float az = math.abs(plane.normal.z);

            // Based on dominant axis, choose appropriate texture axes
            if (az >= ax && az >= ay)
            {
                // Floor/ceiling
                if (plane.normal.z >= 0)
                {
                    // Floor
                    xv.x = 1;
                    xv.y = 0;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = -1;
                    yv.z = 0;
                }
                else
                {
                    // Ceiling
                    xv.x = 1;
                    xv.y = 0;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = -1;
                    yv.z = 0;
                }
            }
            else if (ax >= ay && ax >= az)
            {
                // Left/right wall
                if (plane.normal.x >= 0)
                {
                    // Right wall
                    xv.x = 0;
                    xv.y = 1;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = 0;
                    yv.z = -1;
                }
                else
                {
                    // Left wall
                    xv.x = 0;
                    xv.y = 1;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = 0;
                    yv.z = -1;
                }
            }
            else
            {
                // Front/back wall
                if (plane.normal.y >= 0)
                {
                    // Back wall
                    xv.x = 1;
                    xv.y = 0;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = 0;
                    yv.z = -1;
                }
                else
                {
                    // Front wall
                    xv.x = 1;
                    xv.y = 0;
                    xv.z = 0;
                    yv.x = 0;
                    yv.y = 0;
                    yv.z = -1;
                }
            }

            // Remove any projection of texture vectors along the normal
            float dot = math.dot(xv, plane.normal);
            xv -= dot * plane.normal;
            xv =  math.normalize(xv);

            dot =  math.dot(yv, plane.normal);
            yv  -= dot * plane.normal;
            yv  =  math.normalize(yv);
        }

        //unsure about these, havent checked qbsp code to verify

        public class texinfo_t
        {
            public float[,]  vecs; // Equivalent to float vecs[2][4]
            public int       flags;
            public string    name; // Equivalent to char name[32]
            public int       miptex;
            public texinfo_t next; // Equivalent to texinfo_s *next

            /*public texinfo_t(float[,] vecs, int flags, string name, int miptex, texinfo_t next)
            {
                this.vecs   = vecs;
                this.flags  = flags;
                this.name   = name;
                this.miptex = miptex;
                this.next   = next;
            }*/
        }

        private static int             numtexinfo;
        private static int             nummiptex;
        private static List<string>    miptex;
        private static List<texinfo_t> texinfo;

        /// <summary>
        /// Finds or adds a miptex to the global table
        /// </summary>
        private static int FindMiptex(string name)
        {
            // This would need to be implemented based on your texture system
            // For now, this is a placeholder implementation
            for (int i = 0; i < nummiptex; i++)
            {
                if (string.Equals(name, miptex[i], StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            if (nummiptex >= MAX_MAP_TEXINFO)
                Error("nummiptex == MAX_MAP_TEXINFO");

            miptex[nummiptex] = name;
            return nummiptex++;
        }

        /// <summary>
        /// Finds or adds a texinfo to the global table
        /// </summary>
        private static int FindTexinfo(texinfo_t t)
        {
            // This would need to be implemented based on your texture system
            // For now, this is a placeholder implementation that should be expanded

            // Check for special flag
            string texName = miptex[t.miptex];
            if (texName.StartsWith("*") || texName.StartsWith("sky", StringComparison.OrdinalIgnoreCase))
                t.flags |= TEX_SPECIAL;

            // Check for existing texinfo match
            for (int i = 0; i < numtexinfo; i++)
            {
                texinfo_t tex = texinfo[i];

                if (t.miptex != tex.miptex || t.flags != tex.flags)
                    continue;

                bool match = true;
                for (int j = 0; j < 8; j++)
                {
                    //if (math.abs(t.vecs[j/4][j%4] - tex.vecs[j/4][j%4]) > EQUAL_EPSILON)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return i;
            }

            // Allocate new texinfo
            if (numtexinfo >= MAX_MAP_TEXINFO)
                Error("numtexinfo == MAX_MAP_TEXINFO");

            texinfo[numtexinfo] = t;
            return numtexinfo++;
        }

        /// <summary>
        /// Loads a map file and parses all entities and brushes
        /// </summary>
        /// <param name="filename">Path to the .map file</param>
        public static void LoadMapFile(string filename)
        {
            Console.WriteLine($"--- LoadMapFile ---");
            Console.WriteLine($"{filename}");

            // Reset counters and data structures
            num_entities  = 0;
            nummapbrushes = 0;
            nummiptex     = 0;
            numtexinfo    = 0;

            // Clear out existing data
            entities   = new List<entity_t>();
            mapbrushes = new List<brush_t>();
            miptex     = new List<string>();
            texinfo    = new List<texinfo_t>();
            //Array.Clear(entities, 0, entities.Count);
            //Array.Clear(mapbrushes, 0, mapbrushes.Length);
            //Array.Clear(miptex, 0, miptex.Count);
            //Array.Clear(texinfo, 0, texinfo.Count);

            try
            {
                // Load the entire file into memory
                string mapData = System.IO.File.ReadAllText(filename);

                // Initialize parser
                StartTokenParsing(mapData);

                // Parse all entities 
                while (ParseEntity())
                {
                    // Entity parsing stores directly in the entities array
                }

                // Output summary
                Console.WriteLine($"{nummapbrushes} brushes");
                Console.WriteLine($"{num_entities} entities");
                Console.WriteLine($"{nummiptex} miptex");
                Console.WriteLine($"{numtexinfo} texinfo");
            }
            catch (System.IO.FileNotFoundException)
            {
                Error($"Couldn't open {filename}");
            }
            catch (System.IO.IOException ex)
            {
                Error($"Error reading {filename}: {ex.Message}");
            }
        }

        // Helper method to check if two points are approximately equal

        // Helper method to get a vector perpendicular to the given normal

        // Check if the resulting polygon is convex
    }
}