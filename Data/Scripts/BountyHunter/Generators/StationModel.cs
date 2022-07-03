using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace RazMods.Hunter
{
    [System.Serializable]
    public class StationModel
    {

        public const byte SPAWNMODE_PERP = 0;
        public const byte SPAWNMODE_FRONT = 1;
        public const byte SPAWNMODE_RAND = 2;

        public int ox, oy, ol;//Origin

        public StationTileType[,,] model;//StationModel data

        public StationTileType[] compatibleTypes;//Compatible type list for neighbor checking

        public StationTileType[] typesToCheck;//Tile types needing neighbor check if they are on the model

        public int width, height, depth;//Bounds

        public PGTrigger[] triggers;

        public Orientation orientation;//Current Orientation

        public string type;
        public float spawnChance;
        public byte spawnMode;

        public RandomTileGroups tileGroups;

        static int rix, riy;//Current rotation parameters
        static StationTileType[,,] buildData; //Current model data



        //StationModel with triggers and tile groups
        public StationModel(int ox, int oy, int ol, StationTileType[,,] model, StationTileType[] compatibleTypes, StationTileType[] types, PGTrigger[] trigs, string type, float sc, byte smode, RandomTileGroups tileGroups)
        {

            this.ox = ox;
            this.oy = oy;
            this.ol = ol;
            this.model = model;
            this.compatibleTypes = compatibleTypes;
            this.typesToCheck = types;
            this.triggers = trigs;
            this.spawnChance = sc;
            this.type = type;
            this.spawnMode = smode;
            this.tileGroups = tileGroups;

            depth = model.GetLength(2);
            height = model.GetLength(1);
            width = model.GetLength(0);
            orientation = Orientation.Northbound;

        }

        //StationModel without triggers or tile groups
        public StationModel(int ox, int oy, int ol, StationTileType[,,] model, StationTileType[] compatibleTypes, StationTileType[] types, string type, float sc, byte smode)
        {

            this.ox = ox;
            this.oy = oy;
            this.ol = ol;
            this.model = model;
            this.compatibleTypes = compatibleTypes;
            this.typesToCheck = types;
            this.spawnChance = sc;
            this.type = type;
            this.spawnMode = smode;

            depth = model.GetLength(2);
            height = model.GetLength(1);
            width = model.GetLength(0);
            orientation = Orientation.Northbound;

        }

        public StationModel() { }

        public StationModel(int ox, int oy, int ol, StationTileType[,,] model)
        {

            this.ox = ox;
            this.oy = oy;
            this.ol = ol;
            this.model = model;

            depth = model.GetLength(2);
            height = model.GetLength(1);
            width = model.GetLength(0);
            orientation = Orientation.Northbound;

        }

        public static StationModel copy(StationModel m)
        {
            StationModel newStationModel = new StationModel();
            newStationModel.ox = m.ox;
            newStationModel.oy = m.oy;
            newStationModel.ol = m.ol;

            newStationModel.tileGroups = new RandomTileGroups();

            newStationModel.tileGroups.groupCount = m.tileGroups.groupCount;
            newStationModel.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();

            foreach (RandomTileGroups.GroupParams p in m.tileGroups.groupParams)
                newStationModel.tileGroups.groupParams.Add(p);

            newStationModel.tileGroups.init = true;


            newStationModel.depth = m.model.GetLength(2);
            newStationModel.height = m.model.GetLength(1);
            newStationModel.width = m.model.GetLength(0);

            newStationModel.tileGroups.layout = new byte[newStationModel.width, newStationModel.height, newStationModel.depth];

            newStationModel.spawnChance = m.spawnChance;
            newStationModel.spawnMode = m.spawnMode;
            newStationModel.type = m.type;

            newStationModel.model = new StationTileType[newStationModel.width, newStationModel.height, newStationModel.depth];

            for (int i = 0; i < m.width; i++)
            {
                for (int j = 0; j < m.height; j++)
                {
                    for (int l = 0; l < m.depth; l++)
                    {
                        newStationModel.model[i, j, l] = m.model[i, j, l];
                        newStationModel.tileGroups.layout[i, j, l] = m.tileGroups.layout[i, j, l];
                    }
                }
            }

            newStationModel.orientation = Orientation.Northbound;
            if (m.compatibleTypes != null)
            {
                newStationModel.compatibleTypes = new StationTileType[m.compatibleTypes.Length];
                newStationModel.typesToCheck = new StationTileType[m.typesToCheck.Length];
                for (int i = 0; i < m.compatibleTypes.Length; i++)
                {
                    newStationModel.compatibleTypes[i] = m.compatibleTypes[i];
                }

                for (int j = 0; j < m.typesToCheck.Length; j++)
                {
                    newStationModel.typesToCheck[j] = m.typesToCheck[j];
                }
            }

            if (m.triggers != null)
            {
                newStationModel.triggers = m.triggers;
            }




            return newStationModel;
        }

        public static StationModel rotate(StationModel original, Orientation newOrientation)
        {

            StationModel rotated = StationModel.copy(original);
            List<PGTrigger> newTriggers = new List<PGTrigger>();

            switch (newOrientation)
            {
                case Orientation.Northbound:
                    break;
                case Orientation.Southbound:


                    for (int j = 0; j < original.height; j++)
                    {
                        for (int i = 0; i < original.width; i++)
                        {
                            for (int l = 0; l < original.depth; l++)
                            {



                                rix = (original.width - 1) - i;
                                riy = (original.height - 1) - j;

                                if (rix == original.ox && riy == original.oy)
                                {
                                    rotated.ox = i;
                                    rotated.oy = j;
                                }

                                //Rotate model layout
                                rotated.model[rix, riy, l] = original.model[i, j, l];

                                //Rotate tile group layout
                                rotated.tileGroups.layout[rix, riy, l] = original.tileGroups.layout[i, j, l];

                                //Rotate the PGTriggers on the model
                                if (original.triggers != null)
                                {
                                    foreach (PGTrigger t in original.triggers)
                                    {
                                        if (t.pos.X == i && t.pos.Y == j && t.pos.Z == l)
                                        {
                                            newTriggers.Add(new PGTrigger(new Vector3D(rix, riy, l), t.orientation, t.type, t.iexec));
                                        }
                                    }

                                    rotated.triggers = newTriggers.ToArray();
                                }

                            }
                        }
                    }


                    rotated.orientation = Orientation.Southbound;
                    break;

                case Orientation.Eastbound:

                    rotated.model = new StationTileType[original.height, original.width, original.depth];
                    rotated.tileGroups.layout = new byte[original.height, original.width, original.depth];

                    for (int j = 0; j < original.height; j++)
                    {
                        for (int i = 0; i < original.width; i++)
                        {
                            for (int l = 0; l < original.depth; l++)
                            {

                                rix = j;
                                riy = (original.width - 1) - i;


                                rotated.width = original.height;
                                rotated.height = original.width;

                                if (i == original.ox && j == original.oy)
                                {
                                    rotated.ox = rix;
                                    rotated.oy = riy;
                                }

                                //Rotate model layout
                                rotated.model[rix, riy, l] = original.model[i, j, l];

                                //Rotate tile group layout
                                rotated.tileGroups.layout[rix, riy, l] = original.tileGroups.layout[i, j, l];

                                //Rotate the PGTriggers on the model
                                if (original.triggers != null)
                                {
                                    foreach (PGTrigger t in original.triggers)
                                    {
                                        if (t.pos.X == i && t.pos.Y == j && t.pos.Z == l)
                                        {
                                            newTriggers.Add(new PGTrigger(new Vector3D(rix, riy, l), t.orientation, t.type, t.iexec));
                                        }
                                    }

                                    rotated.triggers = newTriggers.ToArray();
                                }

                            }
                        }
                    }
                    rotated.orientation = Orientation.Eastbound;
                    break;

                case Orientation.Westbound:
                    rotated.model = new StationTileType[original.height, original.width, original.depth];
                    rotated.tileGroups.layout = new byte[original.height, original.width, original.depth];

                    for (int i = 0; i < original.width; i++)
                    {
                        for (int j = 0; j < original.height; j++)
                        {
                            for (int l = 0; l < original.depth; l++)
                            {

                                rix = (original.height - 1) - j;
                                riy = i;


                                rotated.width = original.height;
                                rotated.height = original.width;

                                if (i == original.ox && j == original.oy)
                                {
                                    rotated.ox = rix;
                                    rotated.oy = riy;
                                }

                                //Rotate model layout
                                rotated.model[rix, riy, l] = original.model[i, j, l];

                                //Rotate tile group layout
                                rotated.tileGroups.layout[rix, riy, l] = original.tileGroups.layout[i, j, l];

                                //Rotate the PGTriggers on the model
                                if (original.triggers != null)
                                {
                                    foreach (PGTrigger t in original.triggers)
                                    {
                                        if (t.pos.X == i && t.pos.Y == j && t.pos.Z == l)
                                        {
                                            newTriggers.Add(new PGTrigger(new Vector3D(rix, riy, l), t.orientation, t.type, t.iexec));
                                        }
                                    }

                                    rotated.triggers = newTriggers.ToArray();
                                }

                            }
                        }
                    }
                    rotated.orientation = Orientation.Westbound;
                    break;


            }

            return rotated;
        }

        public static void ExportToFile(StationModel m, string path)
        {
            /*
            StreamWriter sw = new StreamWriter(path);

            sw.WriteLine("- StationModel Settings -");
            sw.WriteLine("Width =" + m.width);
            sw.WriteLine("Height =" + m.height);
            sw.WriteLine("Levels =" + m.depth);
            sw.WriteLine("OriginX =" + m.ox);
            sw.WriteLine("OriginY =" + m.oy);
            sw.WriteLine("OriginL =" + m.ol);
            sw.WriteLine("CompatibleTypes =" + m.compatibleTypes.Length);
            sw.WriteLine("TypesToCheck =" + m.typesToCheck.Length);
            sw.WriteLine("StationModelType =" + m.type);
            sw.WriteLine("SpawnChance =" + m.spawnChance.ToString(CultureInfo.InvariantCulture));
            sw.WriteLine("SpawnMode =" + m.spawnMode);

            sw.WriteLine("- Compatible Type List -");

            for (int ct = 0; ct < m.compatibleTypes.Length; ct++)
            {
                sw.WriteLine("" + (int)m.compatibleTypes[ct]);
            }

            sw.WriteLine("- Types To Check -");

            for (int t = 0; t < m.typesToCheck.Length; t++)
            {
                sw.WriteLine("" + (int)m.typesToCheck[t]);
            }

            if (m.triggers != null)
            {
                sw.WriteLine("- PGTrigger List -");
                sw.WriteLine("Size =" + m.triggers.Length);

                foreach (PGTrigger tg in m.triggers)
                {
                    sw.WriteLine("- PGTrigger Start -");
                    sw.WriteLine("Type =" + tg.type);
                    sw.WriteLine("Orientation =" + (int)tg.orientation);
                    sw.WriteLine("Position X =" + (int)tg.pos.X);
                    sw.WriteLine("Position Y =" + (int)tg.pos.Y);
                    sw.WriteLine("Position L =" + (int)tg.pos.Z);
                    sw.WriteLine("Immediate Execution =" + ((tg.iexec) ? "TRUE" : "FALSE"));
                }
            }

            sw.WriteLine("- StationModel Data -");

            for (int i = 0; i < m.width; i++)
            {
                for (int j = 0; j < m.height; j++)
                {
                    for (int l = 0; l < m.depth; l++)
                    {
                        sw.WriteLine("" + (int)m.model[i, j, l]);
                    }
                }
            }

            sw.WriteLine("- Tile Groups Information -");

            sw.WriteLine("GroupCount =" + m.tileGroups.groupCount);

            for (int i = 0; i < m.tileGroups.groupCount; i++)
            {
                sw.WriteLine("- Group Start -");
                sw.WriteLine("GroupID =" + m.tileGroups.groupParams[i].groupID);
                sw.WriteLine("Chance =" + m.tileGroups.groupParams[i].chance.ToString(CultureInfo.InvariantCulture));
                sw.WriteLine("Linked =" + (m.tileGroups.groupParams[i].linked ? "TRUE" : "FALSE"));
                sw.WriteLine("ReplaceType =" + (short)m.tileGroups.groupParams[i].replaceType);
                sw.WriteLine("SharedRoll =" + (m.tileGroups.groupParams[i].sharedroll ? "TRUE" : "FALSE"));
                sw.WriteLine("SRGroupID =" + m.tileGroups.groupParams[i].srgroupID);
            }

            sw.WriteLine("- Tile Group Layout -");

            for (int i = 0; i < m.width; i++)
            {
                for (int j = 0; j < m.height; j++)
                {
                    for (int l = 0; l < m.depth; l++)
                    {
                        sw.WriteLine("" + m.tileGroups.layout[i, j, l]);
                    }
                }
            }



            sw.Close();
            */
        }

        public static StationModel ImportFromFile(string path, bool loadFromRes)
        {
            /*
            StationModel m = new StationModel();
            m.tileGroups = new RandomTileGroups();
            RandomTileGroups.GroupParams currentGroupParams = new RandomTileGroups.GroupParams();

            bool formatError = false, iexec = false;

            if (Application.isEditor && !loadFromRes)
            {
                StreamReader sr = new StreamReader(path);
                string currentLine;


                sr.ReadLine();//Read and ignore descriptor line

                if ((currentLine = sr.ReadLine()).Contains("Width ="))
                    m.width = int.Parse(currentLine.Replace("Width =", ""));

                if ((currentLine = sr.ReadLine()).Contains("Height ="))
                    m.height = int.Parse(currentLine.Replace("Height =", ""));

                if ((currentLine = sr.ReadLine()).Contains("Levels ="))
                    m.depth = int.Parse(currentLine.Replace("Levels =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginX ="))
                    m.ox = int.Parse(currentLine.Replace("OriginX =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginY ="))
                    m.oy = int.Parse(currentLine.Replace("OriginY =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginL ="))
                    m.ol = int.Parse(currentLine.Replace("OriginL =", ""));

                if ((currentLine = sr.ReadLine()).Contains("CompatibleTypes ="))
                    m.compatibleTypes = new StationTileType[int.Parse(currentLine.Replace("CompatibleTypes =", ""))];

                if ((currentLine = sr.ReadLine()).Contains("TypesToCheck ="))
                    m.typesToCheck = new StationTileType[int.Parse(currentLine.Replace("TypesToCheck =", ""))];

                if ((currentLine = sr.ReadLine()).Contains("StationModelType ="))//If this line is correctly read file format is from new version
                {
                    m.type = currentLine.Replace("StationModelType =", "");

                    if ((currentLine = sr.ReadLine()).Contains("SpawnChance ="))
                    {
                        m.spawnChance = float.Parse(currentLine.Replace("SpawnChance =", ""), CultureInfo.InvariantCulture);

                        if ((currentLine = sr.ReadLine()).Contains("SpawnMode ="))
                        {
                            m.spawnMode = byte.Parse(currentLine.Replace("SpawnMode =", ""));

                            sr.ReadLine();//Read and ignore descriptor line

                        }

                    }

                }
                else//DESCRIPTOR LINE BEING READ TOO SOON IMPLIES MODEL FILE IS FROM OLDER VERSION, LOAD DEFAULT VALUES
                {
                    m.spawnChance = 1;//Default to 100% spawnchance if setting file is from older version
                    m.spawnMode = SPAWNMODE_RAND;
                    m.type = "StationModelFile";

                }//Descriptor line already processed, keep going



                if (m.compatibleTypes != null)
                {
                    for (int ct = 0; ct < m.compatibleTypes.Length; ct++)
                    {
                        m.compatibleTypes[ct] = (StationTileType)int.Parse(sr.ReadLine());
                    }
                }





                sr.ReadLine();//Read and ignore descriptor line


                if (m.typesToCheck != null)
                {
                    for (int t = 0; t < m.typesToCheck.Length; t++)
                    {
                        m.typesToCheck[t] = (StationTileType)int.Parse(sr.ReadLine());
                    }
                }



                //Check for the - PGTrigger List - Descriptor line
                //If it does not match this means that we've already read the next descriptor line
                if ((currentLine = sr.ReadLine()).Contains("- PGTrigger List -"))
                {
                    if ((currentLine = sr.ReadLine()).Contains("Size ="))
                        m.triggers = new PGTrigger[int.Parse(currentLine.Replace("Size =", ""))];

                    if (m.triggers != null)
                    {
                        for (int i = 0; i < m.triggers.Length; i++)
                        {

                            if (!formatError)
                                sr.ReadLine();//Read descriptor line if format error was not thrown
                            else
                                formatError = false;


                            short ctype = 0;
                            Vector3D cpos = new Vector3D(0, 0, 0);
                            Orientation cor = Orientation.Northbound;

                            if ((currentLine = sr.ReadLine()).Contains("Type ="))
                            {
                                ctype = short.Parse(currentLine.Replace("Type =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Orientation ="))
                            {
                                cor = (Orientation)int.Parse(currentLine.Replace("Orientation =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Position X ="))
                            {
                                cpos.X = int.Parse(currentLine.Replace("Position X =", ""));
                            }
                            if ((currentLine = sr.ReadLine()).Contains("Position Y ="))
                            {
                                cpos.Y = int.Parse(currentLine.Replace("Position Y =", ""));
                            }
                            if ((currentLine = sr.ReadLine()).Contains("Position L ="))
                            {
                                cpos.Z = int.Parse(currentLine.Replace("Position L =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Immediate Execution ="))
                            {
                                currentLine = currentLine.Replace("Immediate Execution =", "");

                                iexec = currentLine.Contains("TRUE");
                            }
                            else
                            {
                                formatError = true;//Set the format error flag to TRUE so that we can handle descriptor already being read
                                iexec = false;//Default to false iexec if setting file is from older version
                            }

                            m.triggers[i] = new PGTrigger(cpos, cor, ctype, iexec);

                        }

                        if (!formatError)//Read and ignore descriptor line if format error has not been thrown
                        {
                            sr.ReadLine();
                        }
                        else//Otherwise reset error flag for future use
                        {
                            formatError = false;
                        }


                    }
                }

                m.model = new StationTileType[m.width, m.height, m.depth];


                //Load model layout
                for (int i = 0; i < m.width; i++)
                {
                    for (int j = 0; j < m.height; j++)
                    {
                        for (int l = 0; l < m.depth; l++)
                        {
                            m.model[i, j, l] = (StationTileType)int.Parse(sr.ReadLine());
                        }
                    }
                }

                //Read Tile Groups Descriptor
                try
                {
                    //If tile group descriptor is not present, init default tile group and return
                    if (!(currentLine = sr.ReadLine()).Contains("Tile Groups Information"))
                    {
                        m.tileGroups = new RandomTileGroups();
                        m.tileGroups.groupCount = 1;

                        currentGroupParams.chance = 1;
                        currentGroupParams.groupID = 0;
                        currentGroupParams.linked = true;
                        m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();
                        m.tileGroups.groupParams.Add(currentGroupParams);
                        m.tileGroups.init = true;
                        m.tileGroups.layout = initEmptyGroupLayout(m.width, m.height, m.depth);

                        Debug.Log("Loaded StationModel Without Tile Groups!");

                        return m;
                    }

                }
                catch
                {
                    m.tileGroups = new RandomTileGroups();
                    m.tileGroups.groupCount = 1;

                    currentGroupParams.chance = 1;
                    currentGroupParams.groupID = 0;
                    currentGroupParams.linked = true;
                    m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();
                    m.tileGroups.groupParams.Add(currentGroupParams);
                    m.tileGroups.init = true;
                    m.tileGroups.layout = initEmptyGroupLayout(m.width, m.height, m.depth);

                    Debug.Log("Loaded StationModel Without Tile Groups!");

                    return m;
                }

                //Start reading tile group info
                if ((currentLine = sr.ReadLine()).Contains("GroupCount ="))
                    m.tileGroups.groupCount = byte.Parse(currentLine.Replace("GroupCount =", ""));

                m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();

                for (int i = 0; i < m.tileGroups.groupCount; i++)
                {

                    sr.ReadLine();//Read descriptor

                    if ((currentLine = sr.ReadLine()).Contains("GroupID ="))
                        currentGroupParams.groupID = byte.Parse(currentLine.Replace("GroupID =", ""));

                    if ((currentLine = sr.ReadLine()).Contains("Chance ="))
                        currentGroupParams.chance = float.Parse(currentLine.Replace("Chance =", ""), CultureInfo.InvariantCulture);

                    if ((currentLine = sr.ReadLine()).Contains("Linked ="))
                        currentGroupParams.linked = (currentLine.Replace("Linked =", "").Contains("TRUE") ? true : false);

                    if ((currentLine = sr.ReadLine()).Contains("ReplaceType ="))
                        currentGroupParams.replaceType = (StationTileType)byte.Parse(currentLine.Replace("ReplaceType =", ""));

                    if ((currentLine = sr.ReadLine()).Contains("SharedRoll ="))
                        currentGroupParams.sharedroll = (currentLine.Replace("SharedRoll =", "").Contains("TRUE") ? true : false);

                    if ((currentLine = sr.ReadLine()).Contains("SRGroupID ="))
                        currentGroupParams.srgroupID = byte.Parse(currentLine.Replace("SRGroupID =", ""));

                    m.tileGroups.groupParams.Add(currentGroupParams);
                }

                sr.ReadLine();//Read group layout descriptor

                m.tileGroups.layout = new byte[m.width, m.height, m.depth];

                //Load model layout
                for (int i = 0; i < m.width; i++)
                {
                    for (int j = 0; j < m.height; j++)
                    {
                        for (int l = 0; l < m.depth; l++)
                        {
                            try
                            {
                                m.tileGroups.layout[i, j, l] = byte.Parse(sr.ReadLine());
                            }
                            catch
                            {
                                m.tileGroups.layout[i, j, l] = 0;
                                Debug.Log("Error Loading Template Tile Groups!");
                            }

                        }
                    }
                }



                sr.Close();
            }
            else
            {
                TextAsset ta = (TextAsset)Resources.Load("PDTFiles/Saved Data/StationModels/" + Path.GetFileNameWithoutExtension(path), typeof(TextAsset));

                StringReader sr = new StringReader(ta.text);

                string currentLine;

                sr.ReadLine();//Read and ignore descriptor line

                if ((currentLine = sr.ReadLine()).Contains("Width ="))
                    m.width = int.Parse(currentLine.Replace("Width =", ""));

                if ((currentLine = sr.ReadLine()).Contains("Height ="))
                    m.height = int.Parse(currentLine.Replace("Height =", ""));

                if ((currentLine = sr.ReadLine()).Contains("Levels ="))
                    m.depth = int.Parse(currentLine.Replace("Levels =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginX ="))
                    m.ox = int.Parse(currentLine.Replace("OriginX =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginY ="))
                    m.oy = int.Parse(currentLine.Replace("OriginY =", ""));

                if ((currentLine = sr.ReadLine()).Contains("OriginL ="))
                    m.ol = int.Parse(currentLine.Replace("OriginL =", ""));

                if ((currentLine = sr.ReadLine()).Contains("CompatibleTypes ="))
                    m.compatibleTypes = new StationTileType[int.Parse(currentLine.Replace("CompatibleTypes =", ""))];

                if ((currentLine = sr.ReadLine()).Contains("TypesToCheck ="))
                    m.typesToCheck = new StationTileType[int.Parse(currentLine.Replace("TypesToCheck =", ""))];


                if ((currentLine = sr.ReadLine()).Contains("StationModelType ="))//If this line is correctly read file format is from new version
                {
                    m.type = currentLine.Replace("StationModelType =", "");

                    if ((currentLine = sr.ReadLine()).Contains("SpawnChance ="))
                    {
                        m.spawnChance = float.Parse(currentLine.Replace("SpawnChance =", ""), CultureInfo.InvariantCulture);

                        if ((currentLine = sr.ReadLine()).Contains("SpawnMode ="))
                        {
                            m.spawnMode = byte.Parse(currentLine.Replace("SpawnMode =", ""));

                            sr.ReadLine();//Read and ignore descriptor line

                        }

                    }

                }
                else//DESCRIPTOR LINE BEING READ TOO SOON IMPLIES MODEL FILE IS FROM OLDER VERSION, LOAD DEFAULT VALUES
                {
                    m.spawnChance = 1;//Default to 100% spawnchance if setting file is from older version
                    m.spawnMode = SPAWNMODE_RAND;
                    m.type = "StationModelFile";

                }//Descriptor line already processed, keep going

                if (m.compatibleTypes != null)
                {
                    for (int ct = 0; ct < m.compatibleTypes.Length; ct++)
                    {
                        m.compatibleTypes[ct] = (StationTileType)int.Parse(sr.ReadLine());
                    }
                }

                sr.ReadLine();//Read and ignore descriptor line

                if (m.typesToCheck != null)
                {
                    for (int t = 0; t < m.typesToCheck.Length; t++)
                    {
                        m.typesToCheck[t] = (StationTileType)int.Parse(sr.ReadLine());
                    }
                }

                if ((currentLine = sr.ReadLine()).Contains("- PGTrigger List -"))
                {
                    if ((currentLine = sr.ReadLine()).Contains("Size ="))
                        m.triggers = new PGTrigger[int.Parse(currentLine.Replace("Size =", ""))];

                    if (m.triggers != null)
                    {
                        for (int i = 0; i < m.triggers.Length; i++)
                        {

                            if (!formatError)
                                sr.ReadLine();//Read descriptor line if format error was not thrown
                            else
                                formatError = false;

                            short ctype = 0;
                            Vector3D cpos = new Vector3D(0, 0, 0);
                            Orientation cor = Orientation.Northbound;

                            if ((currentLine = sr.ReadLine()).Contains("Type ="))
                            {
                                ctype = short.Parse(currentLine.Replace("Type =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Orientation ="))
                            {
                                cor = (Orientation)int.Parse(currentLine.Replace("Orientation =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Position X ="))
                            {
                                cpos.X = int.Parse(currentLine.Replace("Position X =", ""));
                            }
                            if ((currentLine = sr.ReadLine()).Contains("Position Y ="))
                            {
                                cpos.Y = int.Parse(currentLine.Replace("Position Y =", ""));
                            }
                            if ((currentLine = sr.ReadLine()).Contains("Position L ="))
                            {
                                cpos.Z = int.Parse(currentLine.Replace("Position L =", ""));
                            }

                            if ((currentLine = sr.ReadLine()).Contains("Immediate Execution ="))
                            {
                                currentLine = currentLine.Replace("Immediate Execution =", "");

                                iexec = currentLine.Contains("TRUE");
                            }
                            else
                            {
                                formatError = true;//Set the format error flag to TRUE so that we can handle descriptor already being read
                                iexec = false;//Default to false iexec if setting file is from older version
                            }

                            m.triggers[i] = new PGTrigger(cpos, cor, ctype, iexec);


                        }

                        if (!formatError)//Read and ignore descriptor line if format error has not been thrown
                        {
                            sr.ReadLine();
                        }
                        else//Otherwise reset error flag for future use
                        {
                            formatError = false;
                        }

                    }
                }

                m.model = new StationTileType[m.width, m.height, m.depth];

                for (int i = 0; i < m.width; i++)
                {
                    for (int j = 0; j < m.height; j++)
                    {
                        for (int l = 0; l < m.depth; l++)
                        {
                            m.model[i, j, l] = (StationTileType)int.Parse(sr.ReadLine());
                        }
                    }
                }

                //Read Tile Groups Descriptor
                try
                {
                    //If tile group descriptor is not present, init default tile group and return
                    if (!(currentLine = sr.ReadLine()).Contains("Tile Groups Information"))
                    {
                        m.tileGroups = new RandomTileGroups();
                        m.tileGroups.groupCount = 1;

                        currentGroupParams.chance = 1;
                        currentGroupParams.groupID = 0;
                        currentGroupParams.linked = false;
                        m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();
                        m.tileGroups.groupParams.Add(currentGroupParams);
                        m.tileGroups.init = true;
                        m.tileGroups.layout = initEmptyGroupLayout(m.width, m.height, m.depth);

                        Debug.Log("Loaded StationModel Without Tile Groups!");

                        return m;
                    }

                }
                catch
                {
                    m.tileGroups = new RandomTileGroups();
                    m.tileGroups.groupCount = 1;

                    currentGroupParams.chance = 1;
                    currentGroupParams.groupID = 0;
                    currentGroupParams.linked = false;
                    m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();
                    m.tileGroups.groupParams.Add(currentGroupParams);
                    m.tileGroups.init = true;
                    m.tileGroups.layout = initEmptyGroupLayout(m.width, m.height, m.depth);
                    return m;
                }

                //Start reading tile group info
                if ((currentLine = sr.ReadLine()).Contains("GroupCount ="))
                    m.tileGroups.groupCount = byte.Parse(currentLine.Replace("GroupCount =", ""));

                m.tileGroups.groupParams = new List<RandomTileGroups.GroupParams>();

                for (int i = 0; i < m.tileGroups.groupCount; i++)
                {

                    sr.ReadLine();//Read descriptor

                    if ((currentLine = sr.ReadLine()).Contains("GroupID ="))
                        currentGroupParams.groupID = byte.Parse(currentLine.Replace("GroupID =", ""));

                    if ((currentLine = sr.ReadLine()).Contains("Chance ="))
                        currentGroupParams.chance = float.Parse(currentLine.Replace("Chance =", ""), CultureInfo.InvariantCulture);

                    if ((currentLine = sr.ReadLine()).Contains("Linked ="))
                        currentGroupParams.linked = (currentLine.Replace("Linked =", "").Contains("TRUE") ? true : false);

                    if ((currentLine = sr.ReadLine()).Contains("ReplaceType ="))
                        currentGroupParams.replaceType = (StationTileType)byte.Parse(currentLine.Replace("ReplaceType =", ""));

                    if ((currentLine = sr.ReadLine()).Contains("SharedRoll ="))
                        currentGroupParams.sharedroll = (currentLine.Replace("SharedRoll =", "").Contains("TRUE") ? true : false);

                    if ((currentLine = sr.ReadLine()).Contains("SRGroupID ="))
                        currentGroupParams.srgroupID = byte.Parse(currentLine.Replace("SRGroupID =", ""));


                    m.tileGroups.groupParams.Add(currentGroupParams);
                }

                sr.ReadLine();//Read group layout descriptor

                m.tileGroups.layout = new byte[m.width, m.height, m.depth];

                //Load group layout
                for (int i = 0; i < m.width; i++)
                {
                    for (int j = 0; j < m.height; j++)
                    {
                        for (int l = 0; l < m.depth; l++)
                        {
                            try
                            {
                                m.tileGroups.layout[i, j, l] = byte.Parse(sr.ReadLine());
                            }
                            catch
                            {
                                m.tileGroups.layout[i, j, l] = 0;
                                Debug.Log("Error Loading Template Tile Groups!");
                            }
                        }
                    }
                }


                sr.Close();

            }





            m.orientation = Orientation.Northbound;

            return m;
            */
            return null;
        }

        public static StationModel pickRandom(StationModel[] models, System.Random rand)
        {
            return models[rand.Next(models.Length)];
        }
        //Returns a random model based on chance, defaults back to pickRandom if none is found
        public static StationModel pickRandomChance(StationModel[] models, float n, System.Random rand)
        {

            List<StationModel> canPick = new List<StationModel>();

            for (int i = 0; i < models.Length; i++)
            {

                if (n < models[i].spawnChance)
                {
                    canPick.Add(models[i]);
                }
            }
            if (canPick.Count > 0)
                return canPick[(rand.Next(canPick.Count))];
            else return models[rand.Next(models.Length)];
        }

        public bool typeToCheck(StationTileType n, StationTileType s, StationTileType e, StationTileType w)
        {

            bool northok = false, southok = false, eastok = false, westok = false;

            foreach (StationTileType t in compatibleTypes)
            {
                if (!northok)
                {
                    northok = t == n;
                }
                if (!southok)
                {
                    southok = t == s;
                }
                if (!eastok)
                {
                    eastok = t == e;
                }
                if (!westok)
                {
                    westok = t == w;
                }
            }

            return (northok && southok && westok && eastok);
        }

        public bool typeToCheck3D(StationTileType n, StationTileType s, StationTileType e, StationTileType w, StationTileType a, StationTileType b)
        {

            bool northok = false, southok = false, eastok = false, westok = false, aboveok = false, belowok = false;

            foreach (StationTileType t in compatibleTypes)
            {
                if (!northok)
                {
                    northok = t == n;
                }
                if (!southok)
                {
                    southok = t == s;
                }
                if (!eastok)
                {
                    eastok = t == e;
                }
                if (!westok)
                {
                    westok = t == w;
                }
                if (!aboveok)
                {
                    aboveok = (a == StationTileType.Empty || a == StationTileType.ERROR);
                }
                if (!belowok)
                {
                    belowok = (b == StationTileType.Empty || b == StationTileType.ERROR);
                }
            }

            return (northok && southok && westok && eastok && aboveok && belowok);
        }

        public bool spaceCheck3D(StationTileType c, StationTileType a, StationTileType b)
        {
            bool centerok = false, aboveok = false, belowok = false;

            if (!centerok)
            {
                centerok = (c == StationTileType.Empty);
            }
            if (!aboveok)
            {
                aboveok = (a == StationTileType.Empty || a == StationTileType.ERROR);
            }
            if (!belowok)
            {
                belowok = (b == StationTileType.Empty || b == StationTileType.ERROR);
            }

            return (centerok && aboveok && belowok);

        }

        public static StationTileType[,,] getTGProcessedLayout(StationModel m, System.Random r)
        {
            float[] rollValue = new float[m.tileGroups.groupCount];

            byte gid;

            //Initialize roll values for linked groups
            for (int i = 0; i < m.tileGroups.groupCount; i++)
            {
                rollValue[i] = (m.tileGroups.groupParams[i].linked) ? (float)r.NextDouble() : -1f;
            }

            //Process roll values for shared groups
            for (int i = 0; i < m.tileGroups.groupCount; i++)
            {
                rollValue[i] = (m.tileGroups.groupParams[i].sharedroll) ? rollValue[m.tileGroups.groupParams[i].srgroupID] : rollValue[i];
            }

            StationTileType[,,] processed = (StationTileType[,,])m.model.Clone();

            for (int i = 0; i < m.width; i++)
            {
                for (int j = 0; j < m.height; j++)
                {
                    for (int l = 0; l < m.depth; l++)
                    {

                        gid = m.tileGroups.layout[i, j, l];

                        if (gid == 0)
                            continue;

                        //Set new roll value if the current group is unlinked
                        rollValue[gid] = (m.tileGroups.groupParams[gid].linked) ? rollValue[gid] : (float)r.NextDouble();

                        //Switch tile types if roll value is lower than chance param
                        //Ternary operation use to make sure a SHARED GROUP uses the parent chance value instead of its own
                        if (rollValue[gid] < ((m.tileGroups.groupParams[gid].sharedroll) ? m.tileGroups.groupParams[m.tileGroups.groupParams[gid].srgroupID].chance : m.tileGroups.groupParams[gid].chance))
                        {
                            processed[i, j, l] = m.tileGroups.groupParams[gid].replaceType;
                        }

                    }
                }
            }

            return processed;
        }

        public static StationModel Closet()
        {

            buildData = new StationTileType[3, 3, 1];

            buildData[0, 0, 0] = StationTileType.Empty;
            buildData[1, 0, 0] = StationTileType.EntranceCloset;
            buildData[2, 0, 0] = StationTileType.Empty;

            buildData[0, 1, 0] = StationTileType.RoomCloset;
            buildData[1, 1, 0] = StationTileType.RoomCloset;
            buildData[2, 1, 0] = StationTileType.RoomCloset;

            buildData[0, 2, 0] = StationTileType.RoomCloset;
            buildData[1, 2, 0] = StationTileType.RoomCloset;
            buildData[2, 2, 0] = StationTileType.RoomCloset;


            return new StationModel(1, 0, 0, buildData, new StationTileType[] { StationTileType.Empty, StationTileType.EntranceCloset }, new StationTileType[] { StationTileType.RoomCloset }, "Closet", 1, SPAWNMODE_RAND);
        }

        public static byte[,,] initEmptyGroupLayout(int w, int h, int d)
        {
            byte[,,] ar = new byte[w, h, d];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    for (int l = 0; l < d; l++)
                    {
                        ar[i, j, l] = 0;
                    }
                }
            }
            return ar;
        }




    }

}
