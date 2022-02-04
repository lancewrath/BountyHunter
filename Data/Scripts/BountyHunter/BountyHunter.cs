using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Systems;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using VRage.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Contracts;
using SpaceEngineers.Game.ModAPI;
using System.Collections.Generic;
using System;
using System.IO;
using VRageMath;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World.Generator;
using Sandbox.Game.SessionComponents;
using VRage.Game;
using VRage.Collections;
using VRage;
using VRage.ObjectBuilders;
using Sandbox.Game.Contracts;
using VRage.Library.Utils;
using VRage.Game.ObjectBuilders.Components.Contracts;
using Sandbox.Game.World;
using static VRage.Game.MyObjectBuilder_Checkpoint;
using Sandbox.Definitions;
using VRage.Utils;

namespace RazMods
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class BountyHunter : MySessionComponentBase
    {
        #region vars
        Action<object, MyDamageInformation> destroyHandler;
        Dictionary<long, IMyFaction> factions = new Dictionary<long, IMyFaction>();
        BountySpawner bountySpawner;
        //Contract info
        List<bountyData> bounties = new List<bountyData>();
        QuestManager questManager = new QuestManager();

        //Entity Lists for reference
        List<Character> myCharacters = new List<Character>();
        
        //List of all contract blocks
        List<ContractBlock> contractBlocks = new List<ContractBlock>();

        List<SpawnCallback> spawnCallbacks = new List<SpawnCallback>();

        //Data files - i'd rather store in seperate files and not save gamedata to prevent bloating from mods
        ModItem thisMod;
        public static string modpath = "";
        const string bountySettingsFile = "BountyHunterSettings.xml";
        public string NPCDataFile = "NPCData.xml";
        public string bountyDataFile = "BountyData.xml";
        public string questDataFile = "QuestData.xml";
        const string activeBountyDataFile = "ActiveBountyData.xml";

        //Server checks
        bool bIsServer = false;
        bool bInitialized = false;
        bool bSaveFlag = false;
        int delay = 0;
        int questdelay = 0;
        public static int BOUNTYTICK = 600;
        public static int QUESTTICK = 1000;
        public static int MAXGLOBALBOUNTIES = 20;
        public static int MAXBOUNTIESPERCHARACTER = 3;
        public static int REWARDMODIFIER = 1000;
        Random _random = new Random();

        #endregion

        #region MainLoop

        public override void UpdateBeforeSimulation()
        {

            if (!bIsServer)
                return;

            //put some space between when we call these functions so it doesn't lag out the game
            delay++;
            questdelay++;
            if(questdelay >= QUESTTICK)
            {
                List<PlayerQuest> pq = questManager.quests.FindAll(q => q.completed);
                foreach (PlayerQuest q in pq)
                {
                    //set Quest log
                    PlayerQuest playerQuests = questManager.quests.Find(pp => !pp.completed && pp.playerID == q.playerID);
                    if (playerQuests != null)
                    {
                        MyVisualScriptLogicProvider.SetQuestlog(true, playerQuests.questName, playerQuests.playerID);
                        playerQuests.questObjective = MyVisualScriptLogicProvider.AddQuestlogObjective(playerQuests.objective, false, false, playerQuests.playerID);
                        playerQuests.questDetail = MyVisualScriptLogicProvider.AddQuestlogDetail(playerQuests.questdesc, false, true, playerQuests.playerID);
                    }
                    else
                    {
                        MyVisualScriptLogicProvider.SetQuestlog(false, "None", q.playerID);
                    }
                }


                questManager.quests.RemoveAll(q => q.completed);
                questdelay = 0;
            }
            if (delay >= BOUNTYTICK)
            {
                UpdateFactions();
                if(bounties.Count < MAXGLOBALBOUNTIES)
                {
                    //Add a new bounty
                    if (contractBlocks.Count > 0)
                    {
                        GenerateBounty();
                    }

                }
                


                delay = 0;
            }

            base.UpdateBeforeSimulation();
        }

        #endregion

        #region BountyFunctions

        public void GenerateBounty()
        {
            //select a random faction
            int randomfaction = _random.Next(factions.Count - 1);
            IMyFaction afaction = null;
            int i = 0;
            foreach (var item in factions)
            {
                if (i == randomfaction)
                {
                    afaction = item.Value;
                    break;
                }
                i++;
            }

            if (afaction == null)
                return;

            //select random enemy faction      
            try
            {
                SpawnCallback sc = new SpawnCallback(this,afaction);
                spawnCallbacks.Add(sc);
                string shipname = BountyGenerator.ShipNames[_random.Next(0, BountyGenerator.ShipNames.Length - 1)];
                List<IMyCubeGrid> grids = bountySpawner.SpawnBounty(afaction, sc.action, shipname);
                sc.SetList(grids);
            }
            catch (Exception exc)
            {
                //MyAPIGateway.Utilities.ShowMessage("Error", exc.StackTrace);
                //Console.WriteLine(exc.ToString());
                //MyAPIGateway.Utilities.ShowMessage("Error", exc.StackTrace);
                //MyVisualScriptLogicProvider.ShowNotificationToAll("Error "+ exc.Message, 5000, "Red");
            }

            
        }

        public void CreateBountyData(IMyFaction afaction, List<IMyCubeGrid> grids)
        {
            if (afaction == null)
                return;
            if (grids == null)
                return;
            if (grids.Count > 0)
            {   
                bountyData bData = new bountyData();
                IMyCubeGrid bountygrid = GetLargestGrid(grids);
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Check Spawned Grid");
                if (bountygrid != null)
                {
                    bountygrid.Save = true;
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                    bountygrid.GetBlocks(blocks);
                    int blockcount = blocks.Count;
                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounty Grid Spawned: " + bountygrid.DisplayName);
                    //MyVisualScriptLogicProvider.ShowNotificationToAll("Bounty Grid Spawned: " + bountygrid.DisplayName, 5000, "White");
                    IMyCockpit cockpit = GetCockpitBlock(bountygrid);

                    string charname = "Bounty";
                    bool isMale = true;
                    //Generate a name
                    if (_random.Next(0, 99) > 49)
                    {
                        charname = NameGenerator.Generate(NameGenerator.Gender.Male);
                    }
                    else
                    {
                        isMale = false;
                        charname = NameGenerator.Generate(NameGenerator.Gender.Female);
                    }
                    bData.name = charname;
                    bData.isMale = isMale;
                    MyAPIGateway.Session.Factions.AddNewNPCToFaction(afaction.FactionId, charname);
                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Added " + charname + " to " + afaction.Name);
                    var mems = afaction.Members;
                    MyFactionMember bountymember = new MyFactionMember();
                    foreach (var member in mems)
                    {
                        if (MyVisualScriptLogicProvider.GetPlayersName(member.Value.PlayerId).Equals(charname))
                        {
                            bountymember = member.Value;
                            break;
                        }
                    }
                    bData.characterowner = bountymember.PlayerId;
                    if (cockpit != null)
                    {
                        if (bountygrid.IsStatic)
                        {
                            bData.targettype = BountyTargetType.COCKPIT;
                            bountygrid.Physics.SetSpeeds(Vector3D.Zero, Vector3D.Zero);
                            bountygrid.ChangeGridOwnership(bountymember.PlayerId, MyOwnershipShareModeEnum.Faction);
                            var pos = new MyPositionAndOrientation(bountygrid.PositionComp.WorldAABB.Center + bountygrid.WorldMatrix.Backward * 2.5, bountygrid.WorldMatrix.Backward, bountygrid.WorldMatrix.Up);
                            IMyCharacter character = bountySpawner.CreateNPCCharacter(bountymember.PlayerId, charname, pos, isMale);
                            Character characterData = new Character(character, bountygrid);
                            myCharacters.Add(characterData);
                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Generated Character");
                            //MyVisualScriptLogicProvider.ShowNotificationToAll("Generated Character", 5000, "White");
                            character.CharacterDied += Character_CharacterDied;
                            //try to put character in cockpit
                            character.SetPosition(cockpit.GetPosition());
                            character.AimedPoint = cockpit.GetPosition();
                            character.Use();
                            cockpit.AttachPilot(character);
                            bData.targettype = BountyTargetType.CHARACTER;
                            bData.targetid = character.EntityId;
                            bData.characterSpawned = true;
                            bData.characterid = character.EntityId;
                            character.Save = true;
                            MyVisualScriptLogicProvider.StoreEntityString(character.Name, "BOUNTYID", "" + bData.targetid);
                        } else
                        {
                            
                            cockpit.DisplayName = charname;
                            cockpit.Name = charname;
                            cockpit.CustomName = charname;
                            bData.targettype = BountyTargetType.COCKPIT;
                            bData.targetid = cockpit.EntityId;
                            MyVisualScriptLogicProvider.StoreEntityString(bountygrid.Name, "BOUNTYID", "" + bData.targetid);
                        }
                        //setup bouty data                       
                        bData.factionid = afaction.FactionId;
                                               
                    }
                    else
                    {

                        IMyRemoteControl remote = GetRemoteControlBlock(bountygrid);
                        if(remote != null)
                        {
                            bData.targetid = remote.EntityId;
                            remote.DisplayName = charname;
                            remote.Name = charname;
                            remote.CustomName = charname;
                            bData.targettype = BountyTargetType.REMOTE;
                        }
                        else
                        {
                            IMySlimBlock b = blocks[_random.Next(0, blocks.Count - 1)];
                            //choose some random block
                            b.FatBlock.DisplayName = charname;
                            b.FatBlock.Name = charname;
                            bData.targetid = b.FatBlock.EntityId;
                            bData.targettype = BountyTargetType.BLOCK;
                        }                 
                        //setup bounty data                        
                        bData.factionid = afaction.FactionId;
                        MyVisualScriptLogicProvider.StoreEntityString(bountygrid.Name, "BOUNTYID", "" + bData.targetid);
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Target is a Grid");
                        //MyVisualScriptLogicProvider.ShowNotificationToAll("Target is a Grid", 5000, "White");

                    }

                    List<IMyFaction> enemies = GetEnemyFactions(afaction);
                    IMyFaction placedfaction = enemies[_random.Next(enemies.Count - 1)];
                    bData.placedfaction = placedfaction.FactionId;
                    bData.reward = blockcount * REWARDMODIFIER;
                    
                    bounties.Add(bData);
                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Generate Bounty");
                    AddBountytoBlocks(bData);
                    return;

                }

                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Add Bounty Finished");

                //MyVisualScriptLogicProvider.ShowNotificationToAll(" New Bounties Available!", 5000, "White");
            } else
            {
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Spawned Grid had "+grids.Count);
            }
        }

        public void AddBountytoBlocks(bountyData bData)
        {
            IMyEntity ent = MyAPIGateway.Entities.GetEntityById(bData.targetid);
            IMyFaction efaction = MyAPIGateway.Session.Factions.TryGetFactionById(bData.factionid);
            IMyFaction pfaction = MyAPIGateway.Session.Factions.TryGetFactionById(bData.placedfaction);

            string reason = BountyGenerator.missionreason[_random.Next(0, BountyGenerator.missionreason.Length - 1)];
            reason = reason.Replace("@TARGET@", ent.DisplayName);
            reason = reason.Replace("@FACTION@", efaction.Name);
            reason = reason.Replace("@PFACTION@",pfaction.Name);

            string about = BountyGenerator.missionend[_random.Next(0,BountyGenerator.missionend.Length - 1)];
            about = about.Replace("@TARGET@",ent.DisplayName);
            about = about.Replace("@FACTION@",efaction.Name);
            about = about.Replace("@PFACTION@", pfaction.Name);

            string reward = BountyGenerator.rewardtext[_random.Next(0,BountyGenerator.rewardtext.Length - 1)];
            string goodbye = BountyGenerator.missiongoodbye[_random.Next(0,BountyGenerator.missiongoodbye.Length - 1)];
            
            string bountyDescription = reason;
            bData.desc = bountyDescription;
            if (ent != null && efaction != null && pfaction != null)
            {
                foreach (ContractBlock cblock in contractBlocks)
                {
                    if (cblock.faction.FactionId != bData.factionid)
                    {
                        MyContractHunter hunter = new MyContractHunter(cblock.contractBlock.EntityId, bData.reward, 1, 320, ent.EntityId);
                        hunter.SetDetails("Bounty Contract", bountyDescription, 200, 10);
                        long oldbalance = 0;

                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        IMyPlayer player = players.Find(x => x.IdentityId == cblock.contractBlock.OwnerId);

                        if (player != null)
                        {
                            player.RequestChangeBalance(bData.reward * 2);
                        }
                        MyAddContractResultWrapper cw = MyAPIGateway.ContractSystem.AddContract(hunter);
                        if (cw.Success)
                        {
                            contract c = new contract(cw.ContractId, cw.ContractConditionId);
                            bData.contracts.Add(c);
                            bSaveFlag = true;
                        } else
                        {
                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Failed to add Bounty");
                            //MyVisualScriptLogicProvider.ShowNotificationToAll("Failed to add Bounty", 5000, "Red");
                        }
                        if (player != null)
                        {
                            player.RequestChangeBalance(-(bData.reward * 2));
                        }

                    }
                }
            }
            else
            {
                bSaveFlag = true;
                bounties.Remove(bData);
                return;
            }


        }


        #endregion

        #region Initialization

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            if (!bInitialized)
                Initialize();
        }

        public override void LoadData()
        {
            base.LoadData();
            //check if bounty file exists, get a list of all contracts
            MyLog.Default.WriteLineAndConsole("Check Bounty Data");
            MyLog.Default.Debug("Check Bounty Data");
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(bountyDataFile, typeof(List<bountyData>)))
            {
                var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(bountyDataFile, typeof(List<bountyData>));
                if (reader != null)
                {
                    string data = reader.ReadToEnd();
                    bounties = MyAPIGateway.Utilities.SerializeFromXML<List<bountyData>>(data);
                    MyLog.Default.WriteLineAndConsole("Bounty Data Loaded");
                    MyLog.Default.Debug("Bounty Data Loaded");
                }
            }
            //check if NPC Data file exists, get a list of all contracts
            MyLog.Default.WriteLineAndConsole("Check NPC Data");
            MyLog.Default.Debug("Check NPC Data");
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(NPCDataFile, typeof(List<characterData>)))
            {
                var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(NPCDataFile, typeof(List<characterData>));
                if (reader != null)
                {
                    string data = reader.ReadToEnd();
                    List<characterData> cdata = MyAPIGateway.Utilities.SerializeFromXML<List<characterData>>(data);
                    if (cdata != null)
                    {
                        foreach (var character in cdata)
                        {
                            IMyCharacter ch = (IMyCharacter)MyVisualScriptLogicProvider.GetEntityById(character.characterid);
                            if (ch != null)
                            {
                                Character mycharacter = new Character(ch);
                                //check if there is a grid attached
                                IMyCubeGrid cgrid = (IMyCubeGrid)MyVisualScriptLogicProvider.GetEntityById(character.gridid);
                                if (cgrid != null)
                                {
                                    mycharacter.characterGrid = cgrid;
                                }
                                myCharacters.Add(mycharacter);
                            }
                        }
                    }
                    MyLog.Default.WriteLineAndConsole("NPC Data Loaded");
                    MyLog.Default.Debug("NPC Data Loaded");
                }
            }
            MyLog.Default.WriteLineAndConsole("Check Quest Data");
            MyLog.Default.Debug("Check Quest Data");
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(questDataFile, typeof(QuestManager)))
            {
                var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(questDataFile, typeof(QuestManager));
                if (reader != null)
                {
                    string data = reader.ReadToEnd();
                    QuestManager qm = MyAPIGateway.Utilities.SerializeFromXML<QuestManager>(data);
                    if (qm != null)
                        questManager = qm;

                    MyLog.Default.WriteLineAndConsole("Quest Data Loaded");
                }
            }

            bountySpawner = new BountySpawner();
            MyLog.Default.WriteLineAndConsole("Bounty: Load Spawn Groups");
            MyLog.Default.Debug("Bounty: Load Spawn Groups");
            bountySpawner.SetupSpawns();

        }

        public override void SaveData()
        {

            base.SaveData();

                string bountydata = MyAPIGateway.Utilities.SerializeToXML(bounties);
                TextWriter tw = MyAPIGateway.Utilities.WriteFileInWorldStorage(bountyDataFile, typeof(string));
                tw.Write(bountydata);
                tw.Close();

                List<characterData> characterdata = GetCharacterData(myCharacters);
                string cdata = MyAPIGateway.Utilities.SerializeToXML(characterdata);
                tw = MyAPIGateway.Utilities.WriteFileInWorldStorage(NPCDataFile, typeof(string));
                tw.Write(cdata);
                tw.Close();


                string qdata = MyAPIGateway.Utilities.SerializeToXML(questManager);
                tw = MyAPIGateway.Utilities.WriteFileInWorldStorage(questDataFile, typeof(string));
                tw.Write(qdata);
                tw.Close();

                bSaveFlag = false;
            
        }

        public void Initialize()
        {
            
            //make sure this runs serverside only for xbox compat.
            bIsServer = MyAPIGateway.Multiplayer.IsServer;
            bInitialized = true;
            
            if (!bIsServer)
                return;

            MyLog.Default.WriteLineAndConsole("Initialize Bounty Hunter");
            MyLog.Default.Debug("Initialize Bounty Hunter");
            MyAPIGateway.Session.SessionSettings.EnableEconomy = true;
            MyAPIGateway.Session.SessionSettings.EnableBountyContracts = true;

            //get current factions
            factions = MyAPIGateway.Session.Factions.Factions;





            //fetch current entity list
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            MyLog.Default.WriteLineAndConsole("Check Entities");
            MyLog.Default.Debug("Check Entities");
            //Parse existing characters in world
            IMyEntity[] ents = new IMyEntity[entities.Count];
            entities.CopyTo(ents, 0);

            foreach (IMyEntity ent in ents)
            {
                if(CheckIsCharacter(ent))
                {
                    //prune list for grid checks
                    entities.Remove(ent);
                }
            }

            //Parse existing grids in world
            ents = new IMyEntity[entities.Count];
            entities.CopyTo(ents, 0);
            foreach (IMyEntity ent in ents)
            {
                if (CheckNewGrid(ent))
                {
                    //prune list for grid checks
                    entities.Remove(ent);
                }
            }
            entities.CopyTo(ents, 0);
            foreach (IMyEntity ent in ents)
            {
                bountySpawner.CheckPlanets(ent);

            }

            //destroyHandler = DestroyHandler;
            MyLog.Default.WriteLineAndConsole("Bounty: SetCallbacks");
            MyLog.Default.Debug("Bounty: SetCallbacks");
            SetCallbacks();
            MyLog.Default.WriteLineAndConsole("BOUNTY SYSTEM INITIALIZED!");
            MyLog.Default.Debug("BOUNTY SYSTEM INITIALIZED!");
            MyAPIGateway.Utilities.ShowMessage("Bounty", "BOUNTY SYSTEM INITIALIZED!");
        }

        public void SetCallbacks()
        {

            //Set Callbacks to reduce load on main loop
            
            MyAPIGateway.Entities.OnEntityAdd += CheckCharacter;
            MyAPIGateway.Entities.OnEntityAdd += CheckGrid;
            MyAPIGateway.Entities.OnEntityRemove += ProcessRemovedGrid;
            MyAPIGateway.ContractSystem.CustomActivateContract += ContractSystem_CustomActivateContract;
            MyAPIGateway.ContractSystem.CustomCanActivateContract += ActivationResults;
            MyAPIGateway.ContractSystem.CustomUpdate += ContractSystem_CustomUpdate;
            MyAPIGateway.ContractSystem.CustomNeedsUpdate += UpdateContract;
            MyAPIGateway.ContractSystem.CustomTimeRanOut += ContractSystem_CustomTimeRanOut;
            MyAPIGateway.ContractSystem.CustomFinishCondition += CustomConditionFinish;
            MyAPIGateway.ContractSystem.CustomConditionFinished += ContractSystem_CustomConditionFinished;
            MyAPIGateway.ContractSystem.CustomFinishFor += ContractSystem_CustomFinishFor;
            MyAPIGateway.ContractSystem.CustomFinish += ContractSystem_CustomFinish;
            MyAPIGateway.ContractSystem.CustomFailFor += ContractSystem_CustomFailFor;
            MyAPIGateway.ContractSystem.CustomFail += ContractSystem_CustomFail;
            MyAPIGateway.ContractSystem.CustomCleanUp += ContractSystem_CustomCleanUp;
           //MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler(1, destroyHandler);
            MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(1, BountyDamageHandler);
        }

        #endregion

        #region CallBacks
        private void BountyDamageHandler(object target, MyDamageInformation info)
        {
            if (target == null)
                return;

            if (target as IMySlimBlock != null)
            {
                IMySlimBlock entity = target as IMySlimBlock;
                if (entity != null)
                {
                    if (entity.Integrity <= 0)
                    {
                        if (entity.FatBlock == null)
                            return;

                        List<bountyData> bData = bounties.FindAll(x => x.targetid == entity.FatBlock.EntityId);
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", entity.FatBlock.DisplayName + " Died!");
                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        string insult = Insults.insults[_random.Next(0, Insults.insults.Length - 1)];
                        string deaths = DeathGenerator.deaths[_random.Next(0, DeathGenerator.deaths.Length-1)];

                        

                        foreach (bountyData bd in bData)
                        {
                            if (!bd.targetdead && !bd.characterSpawned)
                            {
                                if (bd.targettype == BountyTargetType.BLOCK || bd.targettype == BountyTargetType.REMOTE)
                                {
                                    bd.targetdead = true;
                                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyComplete", entity.FatBlock.GetPosition());

                                    MyLog.Default.WriteLineAndConsole("BOUNTY: Bounty Complete");
                                    //Find all players for contract, reward bonus for the one who made the kill
                                    foreach (var acon in bd.activeContracts)
                                    {
                                        if (acon.playerid == info.AttackerId)
                                        {

                                            MyVisualScriptLogicProvider.ShowNotification("KILL BONUS " + bd.reward * 0.25 + " Credits", 10000, "Orange", acon.playerid);
                                            MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);
                                            acon.bonus = true;
                                            IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);


                                            
                                            if(!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                            {
                                                MyLog.Default.WriteLineAndConsole("BOUNTY: Try Finish - Invalid Contract ID: " + acon.contractid);
                                            }
                                        }
                                        else
                                        {
                                            IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);
                                            if (p != null)
                                            {
                                                if (MeasureDistance(p.Character.GetPosition(), entity.FatBlock.GetPosition()) <= 5000)
                                                {
                                                    MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);
                                                    if(!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                                    {
                                                        MyLog.Default.WriteLineAndConsole("BOUNTY: Try Finish - Invalid Contract ID: " + acon.contractid);
                                                    }
                                                }
                                                else
                                                {
                                                    IMyPlayer o = players.Find(x => x.IdentityId == info.AttackerId);
                                                    if (o != null)
                                                    {
                                                        MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + entity.FatBlock.DisplayName + " was killed by " + o.DisplayName, 5000, "Red", acon.playerid);
                                                        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                    }
                                                    else
                                                    {
                                                        MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + entity.FatBlock.DisplayName + " was killed.", 5000, "Red", acon.playerid);
                                                    }
                                                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                    if(!MyAPIGateway.ContractSystem.TryFailCustomContract(acon.contractid))
                                                    {
                                                        MyLog.Default.WriteLineAndConsole("BOUNTY: Try Fail - Invalid Contract ID: " + acon.contractid);
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }
                                else if (bd.targettype == BountyTargetType.COCKPIT && !bd.characterSpawned)
                                {
                                    //spawn a character here
                                    var pos = new MyPositionAndOrientation(entity.FatBlock.PositionComp.WorldAABB.Center + entity.FatBlock.WorldMatrix.Backward * 2.5, entity.FatBlock.WorldMatrix.Backward, entity.FatBlock.WorldMatrix.Up);
                                    IMyCharacter character = bountySpawner.CreateNPCCharacter(bd.characterowner, entity.FatBlock.DisplayName, pos, bd.isMale);
                                    Character characterData = new Character(character, entity.FatBlock.CubeGrid);
                                    myCharacters.Add(characterData);
                                    character.CharacterDied += Character_CharacterDied;
                                    character.SetPosition(entity.FatBlock.GetPosition());
                                    bd.characterSpawned = true;
                                    bd.characterid = character.EntityId;
                                    MyLog.Default.WriteLineAndConsole("BOUNTY: Spawned Character: ID - " + bd.characterid);
                                    //make sure character is saved in case restart happens
                                    bSaveFlag = true;
                                    
                                    foreach (var acon in bd.activeContracts)
                                    {
                                        
                                        MyVisualScriptLogicProvider.SendChatMessageColored(insult, Color.Red, bd.name, acon.playerid);
                                        MyVisualScriptLogicProvider.AddGPSToEntity(bd.name, bd.name, "Kill Target", Color.Orange, acon.playerid);
                                        MyVisualScriptLogicProvider.SetGPSHighlight(bd.name, bd.name, "Kill Target", Color.Orange,true,10,120,Color.Red, acon.playerid);
                                    }

                                }
                                else if (bd.targettype == BountyTargetType.COCKPIT && bd.characterSpawned)
                                {
                                    IMyEntity ent = MyAPIGateway.Entities.GetEntityById(bd.characterid);
                                    if (ent != null)
                                    {
                                        if (ent as IMyCharacter != null)
                                        {
                                            IMyCharacter character = ent as IMyCharacter;

                                            if (character != null)
                                            {
                                                if (character.IsDead)
                                                {
                                                    bd.targetdead = true;

                                                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyComplete", character.GetPosition());
                                                    MyVisualScriptLogicProvider.RemoveGPSFromEntityForAll(bd.name, bd.name, "Target Dead");
                                                    MyVisualScriptLogicProvider.CreateExplosion(character.GetPosition(), 100);
                                                    MyLog.Default.WriteLineAndConsole("BOUNTY: Character Killed: " + character.Name);
                                                    //Find all players for contract, reward bonus for the one who made the kill
                                                    foreach (var acon in bd.activeContracts)
                                                    {
                                                        MyVisualScriptLogicProvider.SendChatMessageColored(deaths, Color.Red, bd.name, acon.playerid);
                                                        if (acon.playerid == info.AttackerId)
                                                        {

                                                            MyVisualScriptLogicProvider.ShowNotification("KILL BONUS " + bd.reward * 0.25 + " Credits", 10000, "Orange", acon.playerid);
                                                            MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);
                                                            acon.bonus = true;
                                                            IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);



                                                            if(!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                                            {
                                                                MyLog.Default.WriteLineAndConsole("BOUNTY: Failed to Finish Contract: "+ acon.contractid);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);
                                                            if (p != null)
                                                            {
                                                                if (MeasureDistance(p.Character.GetPosition(), character.GetPosition()) <= 5000)
                                                                {
                                                                    MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);
                                                                    if(!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                                                    {
                                                                        MyLog.Default.WriteLineAndConsole("BOUNTY: Try Finish - Failed: " + acon.contractid);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    IMyPlayer o = players.Find(x => x.IdentityId == info.AttackerId);
                                                                    if (o != null)
                                                                    {
                                                                        MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + character.DisplayName + " was killed by " + o.DisplayName, 5000, "Red", acon.playerid);
                                                                        MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                                    }
                                                                    else
                                                                    {
                                                                        MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + character.DisplayName + " was killed.", 5000, "Red", acon.playerid);
                                                                    }
                                                                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                                    if(!MyAPIGateway.ContractSystem.TryFailCustomContract(acon.contractid))
                                                                    {
                                                                        MyLog.Default.WriteLineAndConsole("BOUNTY: Try Fail - Failed: " + acon.contractid);
                                                                    }
                                                                }
                                                            }

                                                        }
                                                        
                                                    }
                                                    Character cdata = myCharacters.Find(c => c.character.EntityId==character.EntityId);
                                                    if (cdata != null)
                                                        myCharacters.Remove(cdata);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            } 
            else if (target as IMyCharacter != null)
            {
                IMyCharacter character = target as IMyCharacter;
                if(character != null)
                {
                    if (character.IsDead)
                    {
                        Character cdata = myCharacters.Find(c => c.Equals(character));
                        if (cdata != null)
                            myCharacters.Remove(cdata);
                        List<bountyData> bData = bounties.FindAll(x => x.targetid == character.EntityId || x.characterid == character.EntityId);
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", entity.FatBlock.DisplayName + " Died!");
                        MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounties for character: " + bData.Count);
                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        string deaths = DeathGenerator.deaths[_random.Next(0, DeathGenerator.deaths.Length - 1)];
                        foreach (bountyData bd in bData)
                        {
                            if (!bd.targetdead)
                            {

                                bd.targetdead = true;
                                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyComplete", character.GetPosition());
                                MyVisualScriptLogicProvider.RemoveGPSFromEntityForAll(bd.name, bd.name, "Target Dead");
                                MyVisualScriptLogicProvider.CreateExplosion(character.GetPosition(), 100);
                                //Find all players for contract, reward bonus for the one who made the kill
                                foreach (var acon in bd.activeContracts)
                                {
                                    MyVisualScriptLogicProvider.SendChatMessageColored(deaths, Color.Red, bd.name, acon.playerid);
                                    if (acon.playerid == info.AttackerId)
                                    {

                                        MyVisualScriptLogicProvider.ShowNotification("KILL BONUS " + bd.reward * 0.25 + " Credits", 10000, "Orange", acon.playerid);
                                        MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);



                                        acon.bonus = true;
                                        IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);



                                        if(!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                        {
                                            MyLog.Default.WriteLineAndConsole("BOUNTY: Try Finish - Failed: " + acon.contractid);
                                        }
                                    }
                                    else
                                    {
                                        IMyPlayer p = players.Find(x => x.IdentityId == acon.playerid);
                                        if (p != null)
                                        {

                                            if (MeasureDistance(p.Character.GetPosition(), character.GetPosition()) <= 5000)
                                            {
                                                MyVisualScriptLogicProvider.ShowNotification("Collected Bounty " + bd.reward + " Credits", 10000, "Green", acon.playerid);
                                                if (!MyAPIGateway.ContractSystem.TryFinishCustomContract(acon.contractid))
                                                {
                                                    MyLog.Default.WriteLineAndConsole("BOUNTY: Try Finish - Failed: " + acon.contractid);
                                                }
                                            }
                                            else
                                            {
                                                IMyPlayer o = players.Find(x => x.IdentityId == info.AttackerId);
                                                if (o != null)
                                                {
                                                    MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + character.DisplayName + " was killed by " + o.DisplayName, 5000, "Red", acon.playerid);
                                                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                }
                                                else
                                                {
                                                    MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + character.DisplayName + " was killed.", 5000, "Red", acon.playerid);
                                                }
                                                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("BountyFail", p.Character.GetPosition());
                                                if(!MyAPIGateway.ContractSystem.TryFailCustomContract(acon.contractid))
                                                {
                                                    MyLog.Default.WriteLineAndConsole("BOUNTY: Try FAIL - Failed: " + acon.contractid);
                                                }
                                            }
                                        }

                                    }
                                }

                            }
                        }
                    }
                }

            }
        }

        private void Character_CharacterDied(IMyCharacter obj)
        {
            /*
            List<bountyData> bData = bounties.FindAll(x => x.targetid == obj.EntityId || x.characterid == obj.EntityId);
            foreach (var item in bData)
            {
                //item.targetdead = true;
            }
            */
        }

        private void ContractSystem_CustomCleanUp(long contractId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "DO CONTRACT CLEANUP");

            List<bountyData> bData = bounties.FindAll(b => b.contracts.FindAll(bb => bb.contractid == contractId).Count > 0);

            bounties.RemoveAll(old => bData.Contains(old));
            MyAPIGateway.ContractSystem.RemoveContract(contractId);
            bSaveFlag = true;


        }

        private void ContractSystem_CustomFail(long contractId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract Failed");
        }

        private void ContractSystem_CustomFailFor(long contractId, long identityId, bool isAbandon)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract Failed:  cid: " + contractId+" abandon: "+ isAbandon.ToString());
            foreach (var bData in bounties)
            {
                activeContract acon = bData.activeContracts.Find(y => y.contractid == contractId && y.playerid == identityId);
                if(acon != null)
                {
                    PlayerQuest quest = questManager.quests.Find(x => x.playerID == identityId && x.questid == contractId);
                    if (quest != null)
                    {
                        quest.completed = true;
                        if (isAbandon)
                        {

                            MyVisualScriptLogicProvider.ReplaceQuestlogDetail(quest.questDetail, "- Abandoned -", false, identityId);

                        }
                        else
                        {
                            MyVisualScriptLogicProvider.ReplaceQuestlogDetail(quest.questDetail, " - Failed - ", false, identityId);
                            MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(quest.questDetail, true, identityId);
                            
                        }

                    }
                }
            }
        }

        private void ContractSystem_CustomFinish(long contractId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract Finished:  cid: " + contractId);
            
        }

        private void ContractSystem_CustomFinishFor(long contractId, long identityId, int rewardeeCount)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract Finished:  cid: " + contractId + " rewardees" + rewardeeCount);

            foreach (var bData in bounties)
            {
                MyLog.Default.WriteLineAndConsole("Finish Contract: " + contractId);
                activeContract acon = bData.activeContracts.Find(y => y.contractid == contractId && y.playerid == identityId);
                if(acon!=null)
                {
                    MyLog.Default.WriteLineAndConsole("Found Contract: " + contractId);
                    PlayerQuest quest = questManager.quests.Find(x => x.playerID == identityId && x.questid == contractId);
                    if (quest != null)
                    {
                        MyLog.Default.WriteLineAndConsole("Finish Quest: " + contractId);
                        int rewardAmount = bData.reward;
                        string bonus = "";
                        if(acon.bonus)
                        {
                            bonus = "Kill Bonus: "+(int)(bData.reward * 0.25);
                        }
                        MyVisualScriptLogicProvider.SetQuestlog(true, quest.questName, quest.playerID);
                        MyVisualScriptLogicProvider.AddQuestlogObjective(quest.objective, false, true, quest.playerID);
                        MyVisualScriptLogicProvider.ReplaceQuestlogDetail(quest.questDetail, "Target Destroyed - Reward: "+ rewardAmount+" Credits "+ bonus, true, identityId);
                        MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(quest.questDetail,true,identityId);
                        quest.completed = true;
                        //questManager.quests.Remove(quest);
                    }
                    IMyGps gps = MyAPIGateway.Session.GPS.GetGpsList(identityId).Find(g => g.Hash == acon.gpsHash);
                    if(gps!=null)
                    {
                        MyAPIGateway.Session.GPS.RemoveGps(identityId, gps);
                    }
                    
                    if(acon.bonus)
                    {
                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        IMyPlayer player = players.Find(p => p.PlayerID == acon.playerid);
                        if(player!=null)
                        {
                            player.RequestChangeBalance((long)(bData.reward * 0.25));
                        }
                        
                    }
                    
                    
                }
                
            }


        }

        public bool CustomConditionFinish(long contractId, long conditionId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Condition Finish Check:  cid: " + contractId + " condition" + conditionId);

            foreach (var bData in bounties)
            {
                activeContract acon = bData.activeContracts.Find(x => x.contractid == contractId);
                if (acon != null)
                {
                    if (bData.targetdead)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Condition Finished");
                        
                        return true;
                    }
                }
            }

            return false;
        }

        private void ContractSystem_CustomConditionFinished(long conditionId, long contractId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Condition was Finished");
        }

        private void ContractSystem_CustomTimeRanOut(long contractId)
        {
            foreach (var bData in bounties)
            {
                activeContract acon = bData.activeContracts.Find(x => x.contractid == contractId);
                if(acon != null)
                {
                    //notify player that time ran out
                    IMyEntity ent = MyAPIGateway.Entities.GetEntityById(acon.entityid);
                    MyVisualScriptLogicProvider.ShowNotification("Bounty Failed. " + ent.DisplayName + " got away.", 5000, "Red", acon.playerid);
                    IMyGps gps = MyAPIGateway.Session.GPS.GetGpsList(acon.playerid).Find(x => x.Hash == acon.gpsHash);
                    if (gps != null)
                    {
                        MyAPIGateway.Session.GPS.RemoveGps(acon.playerid, gps);
                    }
                    PlayerQuest quest = questManager.quests.Find(x => x.playerID == acon.playerid && x.questid == contractId);
                    if (quest != null)
                    {

                        
                        MyVisualScriptLogicProvider.ReplaceQuestlogDetail(quest.questDetail, " - Time Expired - ", false, acon.playerid);
                        MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(quest.questDetail, true, acon.playerid);

                        

                    }
                }
            }
        }

        public bool UpdateContract(long contractId)
        {           
            return true;
        }

        private void ContractSystem_CustomUpdate(long contractId, MyCustomContractStateEnum newState, MyTimeSpan currentTime)
        {
            foreach (var bData in bounties)
            {
                activeContract acon = bData.activeContracts.Find(x => x.contractid == contractId);
                if (acon != null)
                {
                    //notify player that time ran out
                    IMyEntity ent = MyAPIGateway.Entities.GetEntityById(acon.entityid);
                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Custom Update:  "+ newState.ToString());





                }
            }
        }

        public MyActivationCustomResults ActivationResults(long a, long b)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Activate Results? ID: " + a + " indentityId" + b);

            //check how many contracts character has

            int bcount = 0;
            foreach(var bounty in bounties)
            {
                IMyEntity ent = MyAPIGateway.Entities.GetEntityById(bounty.targetid);
                if(ent==null)
                {
                    MyVisualScriptLogicProvider.ShowNotification("Error: Target is destoryed or lost", 5000, "Red", b);
                    removeContract(bounty.targetid);
                    return MyActivationCustomResults.Error_General;
                }


                if (!bounty.targetdead)
                    bcount += bounty.activeContracts.FindAll(bb => bb.playerid == b).Count;
            }
            if(bcount >= MAXBOUNTIESPERCHARACTER)
            {
                MyVisualScriptLogicProvider.ShowNotification("Can only have "+ MAXBOUNTIESPERCHARACTER + " active contracts at a time!", 5000, "Red", b);
                return MyActivationCustomResults.Error_General;
            }
            else
            {

                return MyActivationCustomResults.Success;
            }

            
        }

        private void ContractSystem_CustomActivateContract(long contractId, long identityId)
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Activate Contract ID: "+contractId+" indentityId"+ identityId);

            foreach (var bData in bounties)
            {
                contract con = bData.contracts.Find(x => x.contractid == contractId);
                if (con != null)
                {
                    
                    IMyEntity ent = MyAPIGateway.Entities.GetEntityById(bData.targetid);
                    if (ent != null)
                    {
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Activation:  cid: " + contractId + " cond: " + identityId+ "ent: "+ con.entityid);                                     
                        activeContract acon = new activeContract(identityId, contractId, con.conditionid, bData.targetid);
                        Vector3D shippos = ent.GetPosition();
                        Vector3D pos = new Vector3D(shippos.X + _random.Next(0, 500), shippos.Y + _random.Next(0, 600), shippos.Z + _random.Next(0, 500));
                        IMyGps gps = MyAPIGateway.Session.GPS.Create("Last Known Coordinates: " + bData.name, "An anonymous tip came in saying they were spotted in the vicinity.", pos, true);
                        gps.GPSColor = Color.Crimson;
                        acon.gpsHash = gps.Hash;
                        MyAPIGateway.Session.GPS.AddGps(identityId, gps);
                        PlayerQuest quest = new PlayerQuest();
                        quest.questName = "Bounty - " + bData.name;
                        quest.questid = contractId;
                        quest.playerID = identityId;
                        MyVisualScriptLogicProvider.SetQuestlogTitle("Bounty - " + bData.name, identityId);
                        MyVisualScriptLogicProvider.SetQuestlog(true, quest.questName, identityId);
                        quest.objective = "Seek out and Kill " + bData.name;
                        quest.questObjective = MyVisualScriptLogicProvider.AddQuestlogObjective(quest.objective, false, false, identityId);
                        quest.questdesc = bData.desc;
                        quest.questDetail = MyVisualScriptLogicProvider.AddQuestlogDetail(quest.questdesc, false, true, identityId);
                        //MyVisualScriptLogicProvider.SetQuestlog(true, quest.questName, identityId);

                        MyVisualScriptLogicProvider.SetQuestlogVisible(true, identityId);
                        questManager.quests.Add(quest);
                        bSaveFlag = true;
                        //MyAPIGateway.Utilities.ShowMissionScreen("Bounty","Assassinate Target","Seek out and Kill "+ent.DisplayName,)
                        bData.activeContracts.Add(acon);
                        return;
                    }
                }
            }
        }

        private void Bountygrid_OnGridSplit(IMyCubeGrid arg1, IMyCubeGrid arg2)
        {

        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            var fat = obj.FatBlock;
            if (obj.BlockDefinition.Id.SubtypeName.Equals("ContractBlock"))
            {
                if (fat as IMyFunctionalBlock != null)
                {
                    IMyFunctionalBlock fb = fat as IMyFunctionalBlock;
                    if (fb != null)
                    {

                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(fat.OwnerId);
                        if(faction != null)
                        {
                            ContractBlock cb = new ContractBlock(fb, faction, fb.CubeGrid);
                            contractBlocks.Add(cb);
                        }
                    }


                }
            }
        }

        private void Grid_OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj == null)
                return;

            var fat = obj.FatBlock;
            if (obj.BlockDefinition.Id.SubtypeName.Equals("ContractBlock"))
            {
                if (fat as IMyFunctionalBlock != null)
                {
                    IMyFunctionalBlock fb = fat as IMyFunctionalBlock;
                    if (fb != null)
                    {
                        ContractBlock cb = contractBlocks.Find(x => x.contractBlock.EntityId == fb.EntityId);
                        if (cb != null)
                            contractBlocks.Remove(cb);
                    }


                }
            }
        }

        #endregion

        #region CheckFunctions

        public void CheckGrid(IMyEntity entity)
        {
            CheckNewGrid(entity);
        }

        ///<summary>
        ///Get all the contract blocks in <paramref name="entity"/>
        ///</summary>
        ///<param name="entity">entity to check</param>
        ///<returns><c>true</c> If entity was a grid</returns>
        public bool CheckNewGrid(IMyEntity entity)
        {
            if (entity == null)
                return false;

            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;

                if (grid == null)
                {
                    return false;
                }

                //make sure this isn't a projection
                if (grid.Transparent)
                {
                    return false;
                }
                
                grid.OnBlockRemoved += Grid_OnBlockRemoved;
                grid.OnBlockAdded += Grid_OnBlockAdded;
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Find Contract Blocks in "+ grid.DisplayName);
                List<IMyFunctionalBlock> cBlocks = GetContractBlocks(entity);

                foreach (IMyFunctionalBlock block in cBlocks)
                {
                    ContractBlock cblock = contractBlocks.Find(x => x.contractBlock.EntityId == block.EntityId);
                    //add it if we don't have it
                    if (cblock == null)
                    {
                        
                        IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                        if (gridfaction != null)
                        {
                            contractBlocks.Add(new ContractBlock(block, gridfaction, grid));
                        }
                        
                    } else
                    {
                        //do we have the grid and or faction data?                       
                        cblock.contractBlock = block;
                        cblock.parentGrid = grid;
                        IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
                        if (gridfaction != null)
                        {
                            cblock.faction = gridfaction;
                        }
                    }
                }
                
                return true;
            }
            return false;
        }



        ///<summary>
        ///Removes contract blocks when this grid is removed <paramref name="entity"/>
        ///</summary>
        ///<param name="entity">entity to check</param>
        ///<returns><c>void</c></returns>
        public void ProcessRemovedGrid(IMyEntity entity)
        {
            if (entity == null)
                return;
            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;

                if (grid == null)
                {
                    return;
                }

                //make sure this isn't a projection
                if (grid.Transparent)
                {
                    return;
                }

                List<IMyFunctionalBlock> cBlocks = GetContractBlocks(entity);
                foreach (IMyFunctionalBlock block in cBlocks)
                {
                    ContractBlock cblock = contractBlocks.Find(x => x.contractBlock.EntityId == block.EntityId);
                    //remove entry if we have it.
                    if(cblock != null)
                    {
                        contractBlocks.Remove(cblock);
                    }
                }

            }
        }

        public void CheckCharacter(IMyEntity entity)
        {
            CheckIsCharacter(entity);
        }
        
        ///<summary>
        ///Checks if <paramref name="entity"/> is a character
        ///</summary>
        ///<param name="entity">entity to check</param>
        ///<returns><c>true</c> if entitry was a character</returns>
        public bool CheckIsCharacter(IMyEntity entity)
        {
            if (entity == null)
                return false;
            if (entity as IMyCharacter != null)
            {
                var character = entity as IMyCharacter;

                if (character == null)
                {
                    return false;
                }
                if (character.IsPlayer)
                {
                    return true;
                }

                //check if this character is dead.
                if (character.IsDead)
                {

                    //remove this character
                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);
                    IMyPlayer player = players.Find(x => x.Character == character);
                    if (player != null)
                    {
                        string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(player.IdentityId);
                        IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);
                        if (gridfaction != null)
                        {
                            if (gridfaction.FounderId != player.IdentityId)
                            {
                                MyAPIGateway.Session.Factions.KickMember(gridfaction.FactionId, player.IdentityId);
                            }

                        }
                    }
                    //remove any contracts tied to this character
                    removeContract(character.EntityId);
                    //remove from character list if we have it
                    Character mychar = myCharacters.Find(x => x.character.EntityId == character.EntityId);
                    if(mychar != null)
                    {
                        myCharacters.Remove(mychar);
                    }
                    character.Delete();

                }
                else
                {
                    //check if we already have this character
                    Character mychar = myCharacters.Find(x => x.character.EntityId == character.EntityId);
                    //if we cant find it then add it
                    if(mychar == null)
                        myCharacters.Add(new Character(character));
                }
                return true;

            }
            return false;
        }


        #endregion

        #region UpdateFunctions

        ///<summary>
        ///Updates Factions list but removes player factions.
        ///</summary>
        public void UpdateFactions()
        {
            factions.Clear();
            Dictionary<long, IMyFaction> f = MyAPIGateway.Session.Factions.Factions;
            foreach (var faction in f)
            {
                if(faction.Value.IsEveryoneNpc())
                {
                    factions.Add(faction.Key, faction.Value);
                }
            }
        }

        #endregion

        #region GetObjectFunctions

        public List<characterData> GetCharacterData(List<Character> characters)
        {
            List<characterData> charData = new List<characterData>();
            foreach (var character in characters)
            {
                if (character.character == null) continue;
                characterData cdata = new characterData();
                cdata.characterid = character.character.EntityId;
                if (character.characterGrid != null)
                {
                    cdata.gridid = character.characterGrid.EntityId;
                }
            }

            return charData;
        }

        public List<IMyFaction> GetEnemyFactions(IMyFaction afaction)
        {
            List<IMyFaction> enemyfactions = new List<IMyFaction>();

            foreach (var faction in factions)
            {
                if (faction.Value.FactionId == afaction.FactionId)
                    continue;

                if(MyAPIGateway.Session.Factions.GetReputationBetweenFactions(faction.Value.FactionId, afaction.FactionId) < 0)
                {
                    enemyfactions.Add(faction.Value);
                }
            }

            return enemyfactions;
        }

        public IMyRemoteControl GetRemoteControlBlock(IMyCubeGrid grid)
        {
            IMyRemoteControl remote = null;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            foreach (IMySlimBlock block in blocks)
            {
                if (block != null)
                {
                    var fat = block.FatBlock;
                    if (fat != null)
                    {
                        if (fat as IMyRemoteControl != null)
                        {
                            remote = fat as IMyRemoteControl;
                            break;
                        }
                    }
                }
            }

            return remote;
        }

        public IMyCockpit GetCockpitBlock(IMyCubeGrid grid)
        {
            IMyCockpit cockpit = null;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            foreach (IMySlimBlock block in blocks)
            {
                if (block != null)
                {
                    var fat = block.FatBlock;
                    if (fat != null)
                    {
                        if (fat as IMyCockpit != null)
                        {
                            cockpit = fat as IMyCockpit;
                            break;
                        }
                    }
                }
            }

            return cockpit;
        }

        public IMyCubeGrid GetLargestGrid(List<IMyCubeGrid> grids)
        {
            IMyCubeGrid largestgrid = null;
            int size = 0;
            foreach (IMyCubeGrid grid in grids)
            {
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                if (blocks.Count > size)
                {
                    largestgrid = grid;
                    size = blocks.Count;
                }
            }
            return largestgrid;
        }

        public List<IMyFunctionalBlock> GetContractBlocks(IMyEntity entity)
        {
            List<IMyFunctionalBlock> cblock = new List<IMyFunctionalBlock>();
            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                {
                    return cblock;
                }
                List<IMyFunctionalBlock> jumpDrives = new List<IMyFunctionalBlock>();
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                foreach (var block in blocks)
                {
                    if (block != null)
                    {
                        
                        var fat = block.FatBlock;
                        if (fat != null)
                        {
                            
                            if (block.BlockDefinition.Id.SubtypeName.Equals("ContractBlock"))
                            {
                                if (fat as IMyFunctionalBlock != null)
                                {
                                    IMyFunctionalBlock fb = fat as IMyFunctionalBlock;
                                    if (fb != null)
                                    {
                                        
                                        cblock.Add(fb);
                                    }


                                }
                            }
                        }
                    }
                }
            }
            return cblock;
        }

        #endregion

        #region ContractFunctions

        public void removeContract(long entitiyid)
        {

            foreach (bountyData bounty in bounties)
            {
                List<activeContract> acont = bounty.activeContracts.FindAll(y => y.entityid == entitiyid);
                if (acont != null)
                {
                    activeContract[] ac = acont.ToArray();

                    foreach (activeContract contract in ac)
                    {
                        MyAPIGateway.ContractSystem.TryFailCustomContract(contract.contractid);
                        MyAPIGateway.ContractSystem.RemoveContract(contract.contractid);
                        acont.Remove(contract);
                    }

                }
                List<contract> cont = bounty.contracts.FindAll(x => x.entityid == entitiyid);
                if (cont != null)
                {
                    contract[] c = cont.ToArray();
                    foreach (contract contract in c)
                    {
                        MyAPIGateway.ContractSystem.RemoveContract(contract.contractid);
                        cont.Remove(contract);
                    }
                }
            }
            bSaveFlag = true;


            
        }

        #endregion

        #region UtilityFunctions


        public static double MeasureDistance(Vector3D coordsStart, Vector3D coordsEnd)
        {

            double distance = Math.Round(Vector3D.Distance(coordsStart, coordsEnd), 2);
            return distance;

        }

        #endregion

    }


    #region HelperClasses

    public class SpawnCallback
    {
        public Action action;
        IMyFaction faction;
        List<IMyCubeGrid> grids;
        BountyHunter bountyHunter;
        public SpawnCallback()
        {

        }

        public SpawnCallback(BountyHunter b, IMyFaction f)
        {
            bountyHunter = b;           
            faction = f;
            action = SetBounty;
        }

        public void SetList(List<IMyCubeGrid> g)
        {
            grids = g;
        }

        public void SetBounty()
        {
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Callback!");
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Spawned Grids Count: " + grids.Count);
            bountyHunter.CreateBountyData(faction, grids);
        }
    }

    public class Character
    {
        public Character()
        {

        }
        public Character(IMyCharacter c)
        {
            character = c;
        }
        public Character(IMyCharacter c, IMyCubeGrid g)
        {
            character = c;
            characterGrid = g;
        }

        public IMyCharacter character;
        public IMyCubeGrid characterGrid;
    }

    public class ContractBlock
    {
        public ContractBlock()
        {
           
        }

        public ContractBlock(IMyFunctionalBlock cb, IMyFaction fact, IMyCubeGrid pg)
        {
            contractBlock = cb;
            faction = fact;
            parentGrid = pg;
        }

        public IMyFunctionalBlock contractBlock;
        public IMyFaction faction;
        public IMyCubeGrid parentGrid;

    }

    public class MyContractHunter : IMyContractCustom
    {
        public MyContractHunter(long startBlockId, int moneyReward, int collateral, int duration, long targetIdentityId)
        {
            StartBlockId = startBlockId;
            MoneyReward = moneyReward;
            Collateral = collateral;
            Duration = duration;
            TargetIdentityId = targetIdentityId;
            DefinitionId = new MyDefinitionId(MyObjectBuilderType.Parse("MyObjectBuilder_Contract"), "CustomBounty");
            SetDetails();


        }

        public void SetDetails(string name = "Bounty", string desc = "Kill Some dude", int rep = 0, int fail = 0)
        {
            Name = name;
            Description = desc;
            ReputationReward = rep;
            FailReputationPrice = fail;

        }

        public long TargetIdentityId
        {
            get;
            private set;
        }

        public long StartBlockId
        {
            get;
            private set;
        }

        public int MoneyReward
        {
            get;
            private set;
        }

        public int Collateral
        {
            get;
            private set;
        }

        public int Duration
        {
            get;
            private set;
        }

        public MyDefinitionId DefinitionId
        {
            get;
            set;
        }

        public long? EndBlockId
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public int ReputationReward
        {
            get;
            set;
        }

        public int FailReputationPrice
        {
            get;
            set;
        }

        Action<long> IMyContract.OnContractAcquired
        {
            get;
            set;
        }
        Action IMyContract.OnContractSucceeded
        {
            get;
            set;
        }
        Action IMyContract.OnContractFailed
        {
            get;
            set;
        }



    }

    public enum BountyTargetType
    {
        BLOCK = 0,
        REMOTE = 1,
        COCKPIT = 2,
        CHARACTER = 1
    }

    #endregion


    #region serializable classes

    [System.Serializable]
    public class QuestManager
    {
        public List<PlayerQuest> quests = new List<PlayerQuest>();
    }

    [System.Serializable]
    public class PlayerQuest
    {
        public long playerID = 0;
        public int questObjective = 0;
        public int questDetail = 0;
        public string questName = "";
        public string objective = "";
        public long questid = 0;
        public bool completed = false;
        public string questdesc = "";

    }


    [System.Serializable]
    public class bountyData
    {
        public bountyData()
        {

        }

        public bountyData(string bname, long fid)
        {
            name = bname;
            factionid = fid;
        }
        public bool targetdead = false;
        public string desc = "";
        public string name = "Bounty";
        public long factionid = 0;
        public long placedfaction = 0;       
        public long targetid = 0;
        public BountyTargetType targettype = BountyTargetType.BLOCK;
        public bool characterSpawned = false;
        public long characterid = 0;
        public long characterowner = 0;
        public bool isMale = true;
        public int reward = 0;
        public List<contract> contracts = new List<contract>();
        public List<activeContract> activeContracts = new List<activeContract>();
    }


    [System.Serializable]
    public class characterData
    {
        public long characterid = 0;
        public long gridid = 0;
    }

    [System.Serializable]
    public class activeContract
    {
        public activeContract()
        {

        }

        public activeContract(long pid,long cid,long conid,long entid)
        {
            playerid = pid;
            contractid = cid;
            conditionid = conid;
            entityid = entid;
        }

        public long playerid = 0;
        public long contractid = 0;
        public long conditionid = 0;
        public long entityid = 0;
        public int gpsHash = 0;
        public bool bonus = true;
    }

    [System.Serializable]
    public class contract
    {
        public contract()
        {

        }

        public contract(long ctid, long conid)
        {
            contractid = ctid;
            conditionid = conid;
        }
        public long contractid = 0;
        public long conditionid = 0;
        public long entityid = 0;
    }

    #endregion
}
