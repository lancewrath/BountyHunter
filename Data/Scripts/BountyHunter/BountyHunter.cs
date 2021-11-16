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

namespace RazMods
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class BountyHunter : MySessionComponentBase
    {
        List<IMyFunctionalBlock> m_blocks = new List<IMyFunctionalBlock>();
        List<BountyGrid> Bounties = new List<BountyGrid>();
        Dictionary<long, IMyFaction> factions;
        bool bIsServer = false;
        bool binitialized = false;
        int delay = 0;
        public static int BOUNTYTICK = 60;
        Random _random = new Random();
        public override void UpdateBeforeSimulation()
        {
            if (!binitialized)
                Init();
            if (!bIsServer)
                return;
            //put some space between when we call these functions so it doesn't lag out the game
            delay++;
            if (delay >= BOUNTYTICK)
            {
                
                foreach (var bountyGrid in Bounties)
                {
                    if(!bountyGrid.HasBounties())
                    {
                        //let's add some bounties!
                        AddBounty(bountyGrid);
                    }
                }
                delay = 0;
            }


        }


        void Init()
        {
            bIsServer = MyAPIGateway.Multiplayer.IsServer;
            binitialized = true;

            if (!bIsServer)
                return;

            //make sure economy and bounty contracts are on
            MyAPIGateway.Session.SessionSettings.EnableEconomy = true;
            MyAPIGateway.Session.SessionSettings.EnableBountyContracts = true;

            //get current factions
            factions = MyAPIGateway.Session.Factions.Factions;


            //fetch current entity list
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);

            foreach (IMyEntity entity in entities)
            {
                CheckNewGrid(entity);
            }

            MyAPIGateway.Session.SessionSettings.EconomyTickInSeconds = 10;
            //MyAPIGateway.Utilities.ShowNotification("Initialized", 5000);
           
            MyAPIGateway.Utilities.ShowMessage("Bounty", "Mod Initialized");
            MyAPIGateway.Entities.OnEntityAdd += CheckNewGrid;

            //MyAPIGateway.Entities.OnEntityRemove += RemoveTheGrid;
            MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(1, BountyDamageHandler);
            MyAPIGateway.ContractSystem.CustomFinish += ContractSystem_CustomFinish;
            MyAPIGateway.ContractSystem.CustomFinishFor += ContractSystem_CustomFinishFor;
            MyAPIGateway.ContractSystem.CustomFail += ContractSystem_CustomFail;
            MyAPIGateway.ContractSystem.CustomActivateContract += ContractSystem_CustomActivateContract;           
            MyAPIGateway.ContractSystem.CustomCanActivateContract = ActivationResults;
            MyAPIGateway.ContractSystem.CustomUpdate += ContractSystem_CustomUpdate;
            MyAPIGateway.ContractSystem.CustomNeedsUpdate = UpdateContract;
            MyAPIGateway.ContractSystem.CustomFailFor += ContractSystem_CustomFailFor;
            MyAPIGateway.ContractSystem.CustomTimeRanOut += ContractSystem_CustomTimeRanOut;
            MyAPIGateway.ContractSystem.CustomCleanUp += ContractSystem_CustomCleanUp;
            MyAPIGateway.ContractSystem.CustomConditionFinished += ContractSystem_CustomConditionFinished;
            MyAPIGateway.ContractSystem.CustomFinishCondition = CustomConditionFinish;

            delay = MyAPIGateway.Session.ElapsedPlayTime.Minutes;
        }

        private void BountyDamageHandler(object target, MyDamageInformation info)
        {
            if (target as IMyCharacter != null)
            {
                IMyCharacter obj = target as IMyCharacter;
                if (obj != null)
                {
                    BountyGrid bg = Bounties.Find(x => x.npc == obj);
                    if (bg != null)
                    {
                        
                        bg.SetLastAttackerID(info.AttackerId);
                        
                        //Do something cool here
                    }
                }
            }
        }

        public bool CustomConditionFinish(long conditionId, long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {

                if(bg.GetCharacter()!=null)
                {
                    if(bg.GetCharacter().IsDead)
                    {
                        return true;
                    }
                }


                //Do something cool here
            }
            return false;
        }

        private void ContractSystem_CustomConditionFinished(long conditionId, long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
            }
        }

        private void ContractSystem_CustomCleanUp(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                
                //Do something cool here
            }
        }

        private void ContractSystem_CustomTimeRanOut(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
            }
        }

        private void ContractSystem_CustomFailFor(long contractId, long identityId, bool isAbandon)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
            }
        }

        public bool UpdateContract(long contradid)
        {
            //set some stuff in the class fields to change this
            return true;
        }

        private void ContractSystem_CustomUpdate(long contractId, MyCustomContractStateEnum newState, MyTimeSpan currentTime)
        {
            /*
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                IMyCharacter character = bg.GetCharacter();
                if (character != null)
                {
                    if (character.IsDead)
                    {

                        MyAPIGateway.ContractSystem.TryFinishCustomContract(contractId);
                        //bg.RemoveUnaccepted();
                    }
                }
                //Do something cool here
            }
            */
        }

        private void ContractSystem_CustomActivateContract(long contractId, long identityId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
                
                //Vector3D shippos = bg.targetgrid.GetPosition();
                //Vector3D pos = new Vector3D(shippos.X + _random.Next(0, 500), shippos.Y + _random.Next(0, 600), shippos.Z + _random.Next(0, 500));

                //MyVisualScriptLogicProvider.AddGPSObjective("Last Known Coordinates: " + MyVisualScriptLogicProvider.GetPlayersName(bg.ownerid), "An anonymous tip came in saying they were spotted in the vicinity.", pos, Color.OrangeRed, 60 * 30, identityId);
                bg.PlayerAccepted(identityId,contractId);
                //localGPSPlayer = MyAPIGateway.Session.GPS.Create(gpsName, "These coordinates were sent to you by the Exiled Engineer, leader of the CORRUPT faction. Proceed with caution.", gpsCoords, true);
                MyAPIGateway.Session.GPS.AddGps(identityId, bg.GetGPS());
            }
        }

        private void ContractSystem_CustomFail(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
            }
        }

        private void ContractSystem_CustomFinishFor(long contractId, long identityId, int rewardeeCount)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                string pname = MyVisualScriptLogicProvider.GetPlayersName(bg.ownerid);
                long pid = bg.GetContractPlayer(contractId);
                string oname = MyVisualScriptLogicProvider.GetPlayersName(identityId);
                MyVisualScriptLogicProvider.ShowNotification("Bounty for " + pname + " Completed!", 5000, "Green", pid);
                MyAPIGateway.Session.GPS.RemoveGps(identityId, bg.GetGPS());
                //Do something cool here
                /*
                List<contractData> data = bg.GetActiveContracts();
                if (data != null)
                {
                    var playerList = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(playerList);

                    foreach (contractData contract in data)
                    {
                        if (contract.playerid != identityId)
                        {
                            foreach (var player in playerList)
                            {
                                if(player.PlayerID==contract.playerid)
                                {
                                    if(MeasureDistance(player.GetPosition(), bg.GetGPS().Coords)<5000)
                                    {
                                        MyAPIGateway.ContractSystem.TryFinishCustomContract(contract.contractid);
                                    } else
                                    {
                                        MyAPIGateway.ContractSystem.TryFailCustomContract(contract.contractid);
                                        bg.RemoveContract(contract.contractid, oname);
                                    }
                                }
                            }
                            
                            
                            
                        }

                    }
                }
                */
            }
        }

        private void ContractSystem_CustomFinish(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //Do something cool here
            }
        }

        private void Character_CharacterDied(IMyCharacter obj)
        {
            BountyGrid bg = Bounties.Find(x => x.npc == obj);
            if (bg != null)
            {
                string attacker = "was killed by their own stupidity.";
                IMyEntity ent = MyVisualScriptLogicProvider.GetEntityById(bg.GetLastAttackerID());
                if(ent != null)
                {
                    if (ent.GetType().ToString().Contains("MyCubeGrid"))
                    {

                        if (ent as IMyCubeGrid != null)
                        {
                            var grid = ent as IMyCubeGrid;
                            if (grid != null)
                            {
                                if (grid == bg.targetgrid)
                                {
                                    attacker = "was obliterated by their own ship.";
                                }
                                else
                                {
                                    if (grid.BigOwners.Count > 0)
                                    {
                                        string pname = MyVisualScriptLogicProvider.GetPlayersName(grid.BigOwners[0]);
                                        attacker = "was obliterated by " + pname + "'s Ship";
                                    }


                                }
                            }
                        }


                    }
                    else if(ent.GetType().ToString().Contains("MyHandDrill"))
                    {
                        attacker = "was given a new butthole by a Hand Drill";
                    }
                    else 
                    { 

                        attacker = ent.GetType().ToString();

                    }
                }
                //string attacker = MyVisualScriptLogicProvider.GetPlayersName(bg.GetLastAttackerID());
                bg.TargetDied();
                List<contractData> data = bg.GetActiveContracts();
                var playerList = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(playerList);
                if (data != null)
                {
                    foreach (contractData contract in data)
                    {
                        foreach (var player in playerList)
                        {
                            if (player.PlayerID == contract.playerid)
                            {
                                if (MeasureDistance(player.GetPosition(), bg.GetGPS().Coords) < 5000)
                                {
                                    MyAPIGateway.ContractSystem.TryFinishCustomContract(contract.contractid);
                                }
                                else
                                {
                                    MyAPIGateway.ContractSystem.TryFailCustomContract(contract.contractid);
                                    bg.RemoveContract(contract.contractid);
                                }
                            }
                        }

                    }
                }
                MyAPIGateway.Utilities.ShowMessage("Bounty", obj.DisplayName+" "+ attacker);
                //Do something cool here
            }
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
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Found " + cblock.Count + " Contract Blocks");
            return cblock;
        }


        double MeasureDistance(Vector3D coordsStart, Vector3D coordsEnd)
        {

            double distance = Math.Round(Vector3D.Distance(coordsStart, coordsEnd), 2);
            return distance;

        }


        public void CheckNewGrid(IMyEntity entity)
        {
            if (entity as IMyCubeGrid != null)
            {
                var grid = entity as IMyCubeGrid;

                if (grid == null)
                {
                    return;
                }
                //refresh factions
                factions = MyAPIGateway.Session.Factions.Factions;
                //append any contract blocks we might find

                m_blocks.AddArray(GetContractBlocks(entity).ToArray());

                AddPotentialBountyGrid(grid);
                grid.OnBlockAdded += new Action<IMySlimBlock>(CheckBlockAdded);
                //AddBountyContracts(grid);
                delay = 0;
            }
        }

        public void CheckBlockAdded(IMySlimBlock block)
        {
            if(block!=null)
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
                                m_blocks.Add(fb);
                            }


                        }
                    }
                }
            }
        }

        public bool TrySeatCharacter(IMyCubeGrid grid,IMyCharacter character)
        {
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
                            IMyCockpit cp = fat as IMyCockpit;
                            if (cp != null)
                            {
                                
                                character.SetPosition(cp.GetPosition());
                                cp.AttachPilot(character);

                                if(cp.Pilot==character)
                                    return true;
                            }
                        }
                        if(fat as IMyCryoChamber != null)
                        {
                            IMyCryoChamber cc = fat as IMyCryoChamber;
                            if(cc != null)
                            {
                                character.SetPosition(cc.GetPosition());
                                cc.AttachPilot(character);
                                if(cc.Pilot==character)
                                    return true;
                            }
                        }
                        
                    }
                }
            }
            return false;
        }

        public IMyCockpit FindCockPit(IMyCubeGrid grid)
        {
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);
            foreach(IMySlimBlock block in blocks)
            {
                if(block!=null)
                {
                    var fat = block.FatBlock;
                    if (fat != null)
                    {
                        if (fat as IMyCockpit != null)
                        {
                            IMyCockpit cp = fat as IMyCockpit;
                            return cp;
                        }
                    }
                }
            }
            return null;
        }

        public void AddPotentialBountyGrid(IMyCubeGrid grid)
        {

            if (grid == null)
            {
                return;
            }
            //ignore if we have this already
            BountyGrid bg = Bounties.Find(x => x.targetgrid == grid);
            if (bg != null)
            {
                if(bg.GetCharacter() != null)
                {
                    IMyCharacter c = bg.GetCharacter();
                    if(c.Parent == null)
                        TrySeatCharacter(grid, c);
                }
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Already Tracking " + grid.DisplayName);
                return;
            }


            //ignore respawn grids, grids not in scene and economy grids
            //if (!grid.IsRespawnGrid && !grid.DisplayName.Contains("economy"))
            //{
            //make sure grid has some kind of loot boxes
            //if(grid.HasInventory)
            //{
                IMyCharacter character = null;
                List<long> owners = grid.BigOwners;
                if(owners != null)
                {
                    if(owners.Count > 0)
                    {
                        long owner = owners[0];
                        string charname = MyVisualScriptLogicProvider.GetPlayersName(owner);
                        if (MyAPIGateway.Players.TryGetSteamId(owner) == 0)
                        {
                            string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(owners[0]);
                            IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);
                            if (gridfaction != null)
                            {
                                //check and see if grid owner is faction owner
                                if (gridfaction.FounderId == owner)
                                {

                                    //we don't dont want to add bounty to npc Founder
                                    //make a new NPC
                                    if (_random.Next(0,1)==1)
                                    {
                                        charname = NameGenerator.Generate(NameGenerator.Gender.Male);
                                    } else
                                    {
                                        charname = NameGenerator.Generate(NameGenerator.Gender.Female);
                                    }
                                    //add new NPC
                                    
                                    MyAPIGateway.Session.Factions.AddNewNPCToFaction(gridfaction.FactionId, charname);
                                    var mems = gridfaction.Members;
                                    foreach (var member in mems)
                                    {
                                        //member.Value.PlayerId

                                        if (MyVisualScriptLogicProvider.GetPlayersName(member.Value.PlayerId).Equals(charname))
                                        {
                                            //give grid to new NPC
                                            owner = member.Value.PlayerId;
                                            grid.Physics.SetSpeeds(Vector3D.Zero, Vector3D.Zero);
                                            grid.ChangeGridOwnership(owner, MyOwnershipShareModeEnum.Faction);
                                            //Create an NPC character we can kill;
                                            var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, (Vector3)grid.WorldMatrix.Backward, (Vector3)(Vector3)grid.WorldMatrix.Up);
                                            character = CreateNPCCharacter(owner, charname, pos);
                                            
                                            bool seated = TrySeatCharacter(grid, character);
                                            if (!seated)
                                            {

                                            }
                                            character.CharacterDied += Character_CharacterDied;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        bg = new BountyGrid(grid, owner);
                        if(character!=null)
                        {
                            bg.SetCharacter(character);
                        }
                        MyAPIGateway.Utilities.ShowMessage("Bounty", "Tracking: "+ grid.DisplayName);
                        Bounties.Add(bg);
                    }
                }
            //}
            //}         
        }



        public List<long> GetEnemyFactionContractBlocks(string fname)
        {
            List<long> blockids = new List<long>();

            foreach (var block in m_blocks)
            {
                if (MyVisualScriptLogicProvider.GetPlayersFactionName(block.OwnerId).Equals(fname))
                {
                    blockids.Add(block.EntityId);
                }

            }
            return blockids;
        }

        public List<long> GetEnemyFactionContractBlocks(long factionid)
        {
            List<long> blockids = new List<long>();

            foreach (var block in m_blocks)
            {
                IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
                if(f != null)
                {
                    if(MyAPIGateway.Session.Factions.AreFactionsEnemies(factionid, f.FactionId))
                    {

                        blockids.Add(block.EntityId);

                    }
                    
                }
                
                
            }

            return blockids;
        }

        public bool IsPlayer(long id)
        {

            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            if (players.Count == 0)
                return false;

            IMyPlayer p = players.Find(x => x.IdentityId == id);
            if (p != null)
                return true;
            return false;
        }

        public IMyCharacter CreateNPCCharacter(long id, string name, MyPositionAndOrientation csys)
        {
            var ob = new MyObjectBuilder_Character()
            {
                Name = name,
                DisplayName = name,
                SubtypeName = "Drone_Bot",
                CharacterModel = "Space_Skeleton",
                EntityId = 0,
                AIMode = true,
                JetpackEnabled = true,
                EnableBroadcasting = false,
                NeedsOxygenFromSuit = false,
                OxygenLevel = 1,
                MovementState = MyCharacterMovementEnum.Standing,
                PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled,
                PositionAndOrientation = csys,
                Health = 1000,
                OwningPlayerIdentityId = id,
                ColorMaskHSV = new Vector3(0, -0, -0),
            };
            var npc = MyEntities.CreateFromObjectBuilder(ob, true) as IMyCharacter;
            if (npc != null)
            {
                MyEntities.Add((MyEntity)npc, true);
            } else
            {
                MyAPIGateway.Utilities.ShowMessage("Bounty", "Failed to create NPC.");
            }
            return npc;
        }

        public bool AddBounty(BountyGrid bgrid)
        {
            bool newbounties = false;
            //bounty is finished do not try to add
            if(bgrid.ConditionMet() || !bgrid.IsTargetAlive())
                return false;

            string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(bgrid.ownerid);
            IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);
            if (gridfaction != null)
            {
                List<IMyFaction> enemies = GetEnemyFactions(gridfaction);
                foreach (IMyFaction enemy in enemies)
                {
                    if(enemy != null)
                    {
                        //make sure there are contract blocks in the world
                        List<long> blockids = GetEnemyFactionContractBlocks(enemy.FactionId);
                        if(blockids.Count > 0)
                        {
                            foreach (var blockid in blockids)
                            {
                                //long id = MyAPIGateway.Players.TryGetIdentityId(MyAPIGateway.Multiplayer.ServerId);
                                
                                string name = MyVisualScriptLogicProvider.GetPlayersName(bgrid.ownerid);
                                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                                bgrid.targetgrid.GetBlocks(blocks);
                                int reward = blocks.Count * 1000;
                                var ent = MyVisualScriptLogicProvider.GetEntityById(blockid);
                                if (ent as IMyCubeBlock != null)
                                {
                                    IMyCubeBlock storeblock = ent as IMyCubeBlock;
                                    if (storeblock != null)
                                    {
                                        IMyFaction pfaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(storeblock.OwnerId);
                                        if (pfaction != null)
                                        {
                                            int relation = MyAPIGateway.Session.Factions.GetReputationBetweenFactions(pfaction.FactionId, bgrid.ownerid);
                                            if (relation<=-500)
                                            {
                                                if (bgrid.ownerid != storeblock.OwnerId)
                                                {
                                                    if (bgrid.GetCharacter() != null)
                                                    {
                                                        if (!bgrid.GetCharacter().IsDead)
                                                        {
                                                            MyContractHunter hunter = new MyContractHunter(blockid, reward, 100, 60, bgrid.GetCharacter().EntityId);
                                                            string enemyfounder = MyVisualScriptLogicProvider.GetPlayersName(enemy.FounderId);

                                                            string desc = enemyfounder + " of " + enemy.Name + " has put out a bounty on " + name + " for their aggressive violence against it's members. They were last seen piloting a ship called " + bgrid.targetgrid.DisplayName + ". Please subdue them with excess force! ";
                                                            hunter.SetDetails("Bounty Contract", desc, 200, 10);
                                                            //MyContractBounty bounty = new MyContractBounty(blockid, shipsize, 100, 60, bgrid.ownerid); // bgrid.ownerid);
                                                            //IMyContractCustom contract = (IMyContractCustom)bounty;
                                                            var cw = MyAPIGateway.ContractSystem.AddContract(hunter);
                                                            if (cw.Success)
                                                            {
                                                                newbounties = true;
                                                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounty Created for " + name);
                                                                bgrid.AddBountyContract(cw.ContractId);
                                                                MyAPIGateway.ContractSystem.GetContractDefinitionId(cw.ContractId);
                                                                if (!bgrid.HasGPS())
                                                                {
                                                                    Vector3D shippos = bgrid.targetgrid.GetPosition();
                                                                    Vector3D pos = new Vector3D(shippos.X + _random.Next(0, 500), shippos.Y + _random.Next(0, 600), shippos.Z + _random.Next(0, 500));
                                                                    IMyGps gps = MyAPIGateway.Session.GPS.Create("Last Known Coordinates: " + name, "An anonymous tip came in saying they were spotted in the vicinity.", pos, true);
                                                                    gps.GPSColor = Color.Crimson;
                                                                    bgrid.SetGPS(gps);
                                                                }

                                                            }
                                                            else
                                                            {
                                                                if (bgrid.GetCharacter().IsDead)
                                                                {
                                                                    MyAPIGateway.Utilities.ShowMessage("Bounty", "Dead.. " + name);
                                                                }
                                                            }
                                                        }
                                                    } 
                                                    else
                                                    {
                                                        //Add character.

                                                        
                                                        bgrid.targetgrid.ChangeGridOwnership(bgrid.ownerid, MyOwnershipShareModeEnum.Faction);
                                                        //Create an NPC character we can kill;
                                                        var pos = new MyPositionAndOrientation(bgrid.targetgrid.PositionComp.WorldAABB.Center + bgrid.targetgrid.WorldMatrix.Backward * 2.5, (Vector3)bgrid.targetgrid.WorldMatrix.Backward, (Vector3)(Vector3)bgrid.targetgrid.WorldMatrix.Up);
                                                        IMyCharacter character = CreateNPCCharacter(bgrid.ownerid, name, pos);
                                                        List<IMyPlayer> _players = new List<IMyPlayer>();
                                                        MyAPIGateway.Players.GetPlayers(_players);
                                                        IMyPlayer player = _players.Find(x => x.IdentityId == bgrid.ownerid);
                                                        if(player != null)
                                                        {
                                                            if(player.Character==null)
                                                            {
                                                                player.SpawnIntoCharacter(character);
                                                                MyAPIGateway.Utilities.ShowMessage("Bounty", "Spawned " + name);
                                                            }
                                                        }
                                                        IMyCockpit cp = FindCockPit(bgrid.targetgrid);
                                                        if (cp != null)
                                                        {
                                                            cp.AttachPilot(character);
                                                        }
                                                        bgrid.SetCharacter(character);
                                                        character.CharacterDied += Character_CharacterDied;
                                                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Grid has no Character" + name);
                                                    }
                                                }
                                            } else
                                            {
                                                MyAPIGateway.Utilities.ShowMessage("Bounty", "Factions are enemies");
                                            }
                                        }
                                    }

                                } else
                                {
                                    MyAPIGateway.Utilities.ShowMessage("Bounty", "Store block error");
                                }


                            }
                        }
                    }
                }
            }
            if(newbounties)
            {
                MyAPIGateway.Utilities.ShowMessage("Bounty", "New Bounties Available");
            }
            return false;
        }

        //var posOr = new MyPositionAndOrientation(_block.PositionComp.WorldAABB.Center + _block.WorldMatrix.Backward * 2.5, (Vector3)_block.WorldMatrix.Backward, (Vector3)(Vector3)_block.WorldMatrix.Up);
        /*
        public void AddBountyContracts(IMyCubeGrid grid)
        {
            if (grid.BigOwners.Count > 0)
            {
                //get first big owner
                long owner = grid.BigOwners[0];
                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(owner);
                IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);

                if (gridfaction != null)
                {
                    List<IMyFaction> enemies = GetEnemyFactions(gridfaction);
                    foreach (IMyFaction efaction in enemies)
                    {
                        if (efaction != null)
                        {
                            long contractid = 0;
                            if (m_blocks.Count > 0)
                            {
                                List<long> bids = GetEnemyFactionContractBlocks(efaction.Name);
                                if (bids.Count > 0)
                                {
                                    foreach (var cid in bids)
                                    {
                                        string npcname = MyVisualScriptLogicProvider.GetPlayersEntityName(owner);
                                        if (!IsPlayer(owner))
                                        {
                                            if (gridfaction.FounderId == owner)
                                            {

                                                npcname = NameGenerator.Generate(NameGenerator.Gender.Male);
                                                MyAPIGateway.Session.Factions.AddNewNPCToFaction(gridfaction.FactionId, npcname);
                                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "NPC Created : "+ npcname);
                                                var mems = gridfaction.Members;
                                                int i = 0;
                                                
                                                foreach (var member in mems)
                                                {
                                                    //member.Value.PlayerId

                                                    if (MyVisualScriptLogicProvider.GetPlayersName(member.Value.PlayerId).Equals(npcname))
                                                    {

                                                        owner = member.Value.PlayerId;
                                                        grid.ChangeGridOwnership(owner, MyOwnershipShareModeEnum.Faction);


                                                        MyAPIGateway.Utilities.ShowMessage("Bounty", "NPC Created : " + npcname + " " + owner);
                                                        var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, (Vector3)grid.WorldMatrix.Backward, (Vector3)(Vector3)grid.WorldMatrix.Up);
                                                        IMyCharacter character = CreateNPCCharacter(owner, npcname, pos);
                                                    }

                                                }
                                                //

                                                //

                                                //owner = character.EntityId;

                                            }

                                        }
                                        

                                        MyContractBounty bounty = new MyContractBounty(cid, blocks.Count * 1000, 100, 60, owner);
                                        
                                        var cw = MyAPIGateway.ContractSystem.AddContract(bounty);
                                        MyVisualScriptLogicProvider.AddBountyContract(cid, blocks.Count * 1000, 100, 60, owner, out contractid);
                                        
                                        if (cw.Success)
                                        {
                                            //MyEntity ent = MyVisualScriptLogicProvider.GetEntityById(cid);
                                            
                                            MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounty Activated for "+ npcname);


                                            BountyGrid bg = Bounties.Find(x => x.targetgrid == grid);
                                            if (bg != null)
                                            {
                                                bg.AddBountyContract(contractid);
                                                bg.UpdateBounty(blocks.Count * 100);
                                            }
                                            else
                                            {
                                                bg = new BountyGrid(grid);
                                                bg.AddBountyContract(contractid);
                                                bg.UpdateBounty(blocks.Count * 100);
                                                Bounties.Add(bg);
                                            }

                                        } else
                                        {
                                            
                                            MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounty Failed : " + cid);
                                        }


                                    }

                                }

                            }
                            //if bounty doesn't exist make it, otherwise add contract


                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Bounty Available on : "+grid.CustomName);

                        }
                    }
                }
            }
        }
        */
        
        public MyActivationCustomResults ActivationResults(long a, long b)
        {
            MyAPIGateway.Utilities.ShowMessage("Bounty", "Trying to accept contract : ");
            return MyActivationCustomResults.Success;
        }



 

        public List<IMyFaction> GetEnemyFactions(IMyFaction faction)
        {
            List<IMyFaction> enemyFactions = new List<IMyFaction>();

            foreach (var item in factions)
            {
                if(faction.IsEnemy(item.Value.FounderId))
                {
                    enemyFactions.Add(item.Value);
                }
            }

            return enemyFactions;
        }

    }

    public class MyContractHunter : IMyContract, IMyContractCustom
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

        public void SetDetails(string name = "Bounty", string desc = "Kill Some dude",int rep = 0, int fail = 0)
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

    public class contractData
    {
        public long playerid = 0;
        public long contractid = 0;
    }

    public class BountyGrid
    {
        public IMyCubeGrid targetgrid = null;
        public List<long> contracts = new List<long>();
        public List<contractData> playersAccepted = new List<contractData>();
        public IMyFaction faction = null;
        public int bounty = 0;
        public long ownerid = 0;
        public IMyCharacter npc = null;
        public bool isPlayer = false;
        public bool isTargetAlive = true;
        public bool conditionMet = false;
        public long killedby = 0;
        public IMyGps targetGPS = null;

        public void RemoveUnaccepted()
        {
            long[] cons = contracts.ToArray();
            for (int i = 0; i < cons.Length; i++)
            {
                contractData cd = playersAccepted.Find(x => x.contractid == cons[i]);
                if(cd==null)
                {
                    contracts.Remove(cons[i]);
                }
            }
        }

        public IMyGps GetGPS()
        {
            return targetGPS;
        }

        public void SetGPS(IMyGps gps)
        {
            targetGPS = gps;
        }

        public bool HasGPS()
        {
            if(targetGPS == null)
            {
                return false;
            }
            return true;
        }

        public IMyCharacter GetCharacter()
        {
            return npc;
        }

        public bool ConditionMet()
        {
            return conditionMet;
        }

        public void RemoveContract(long contract,string oname="Someone Else")
        {
            contractData cd = playersAccepted.Find(x => x.contractid == contract);
            if (cd != null)
            {
                string pname = MyVisualScriptLogicProvider.GetPlayersName(ownerid);
                MyVisualScriptLogicProvider.ShowNotification("Bounty for " + pname + " completed by "+ oname, 5000, "Red", cd.playerid);
                playersAccepted.Remove(cd);
            }
            contracts.Remove(contract);
            MyAPIGateway.ContractSystem.TryFailCustomContract(contract);
            MyAPIGateway.ContractSystem.RemoveContract(contract);
            
        }

        public List<contractData> GetActiveContracts()
        {
            return playersAccepted;
        }

        public long GetContractPlayer(long contract)
        {
            contractData cd = playersAccepted.Find(x => x.contractid == contract);
            if(cd != null)
                return cd.playerid;
            return 0;
        }

        public BountyGrid(IMyCubeGrid grid)
        {
            targetgrid = grid;
        }

        public BountyGrid(IMyCubeGrid grid,long owner)
        {
            targetgrid = grid;
            ownerid = owner;
        }

        public void PlayerAccepted(long p,long c)
        {
            contractData cd = new contractData();
            cd.playerid = p;
            cd.contractid = c;
            playersAccepted.Add(cd);
        }

        public void TargetDied()
        {
            isTargetAlive = false;
            conditionMet = true;
        }

        public bool IsTargetAlive()
        {
            return isTargetAlive;
        }

        public void SetCharacter(IMyCharacter c)
        {
            this.npc = c;
        }

        public void SetIsPlayer(bool p)
        {
            isPlayer = p;
        }

        public bool HasBounties()
        {
            if (contracts.Count > 0)
                return true;
            return false;
        }

        public int GetBountyValue()
        {
            return bounty;
        }

        public List<long> GetBounties()
        {
            return contracts;
        }

        public void UpdateBounty(int b)
        {
            bounty = b;
            foreach (long c in contracts)
            {

            }
        }

        public bool HasBountyId(long contract)
        {
            if(contracts.Find(x => x == contract)!=0)
                return true;
            return false;
        }

        public void AddBountyContract(long contract)
        {
            contracts.Add(contract);
        }

        public long GetLastAttackerID()
        {
            return killedby;
        }

        public void SetLastAttackerID(long attackerId)
        {
            killedby = attackerId;
        }
    }



    public class NameGenerator
    {
        private static Random _random = new Random();
        private static List<string> _dudes = new List<string> {
        "AARON",
        "ABEL",
        "ABRAHAM",
        "ADAM",
        "ADAN",
        "ADOLFO",
        "ADOLPH",
        "ADRIAN",
        "AGUSTIN",
        "AL",
        "ALAN",
        "ALBERT",
        "ALBERTO",
        "ALEJANDRO",
        "ALEX",
        "ALEXANDER",
        "ALEXIS",
        "ALFONSO",
        "ALFRED",
        "ALFREDO",
        "ALI",
        "ALLAN",
        "ALLEN",
        "ALONZO",
        "ALPHONSO",
        "ALTON",
        "ALVARO",
        "ALVIN",
        "AMOS",
        "ANDRE",
        "ANDRES",
        "ANDREW",
        "ANDY",
        "ANGEL",
        "ANGELO",
        "ANTHONY",
        "ANTOINE",
        "ANTON",
        "ANTONIO",
        "ANTONY",
        "ARCHIE",
        "ARMAND",
        "ARMANDO",
        "ARNOLD",
        "ARRON",
        "ART",
        "ARTHUR",
        "ARTURO",
        "ASHLEY",
        "AUBREY",
        "AUGUST",
        "AURELIO",
        "AUSTIN",
        "AVERY",
        "BARNEY",
        "BARRY",
        "BART",
        "BASIL",
        "BEAU",
        "BEN",
        "BENITO",
        "BENJAMIN",
        "BENNETT",
        "BENNIE",
        "BENNY",
        "BERNARD",
        "BERNARDO",
        "BERNIE",
        "BERT",
        "BILL",
        "BILLIE",
        "BILLY",
        "BLAINE",
        "BLAIR",
        "BLAKE",
        "BOB",
        "BOBBIE",
        "BOBBY",
        "BOOKER",
        "BOYD",
        "BRAD",
        "BRADFORD",
        "BRADLEY",
        "BRADY",
        "BRAIN",
        "BRANDEN",
        "BRANDON",
        "BRENDAN",
        "BRENT",
        "BRET",
        "BRETT",
        "BRIAN",
        "BROCK",
        "BRUCE",
        "BRUNO",
        "BRYAN",
        "BRYANT",
        "BRYCE",
        "BRYON",
        "BUDDY",
        "BUFORD",
        "BURTON",
        "BYRON",
        "CALEB",
        "CALVIN",
        "CAMERON",
        "CAREY",
        "CARL",
        "CARLO",
        "CARLOS",
        "CARLTON",
        "CARMELO",
        "CARMEN",
        "CARROLL",
        "CARSON",
        "CARTER",
        "CARY",
        "CASEY",
        "CECIL",
        "CEDRIC",
        "CESAR",
        "CHAD",
        "CHARLES",
        "CHARLEY",
        "CHARLIE",
        "CHASE",
        "CHESTER",
        "CHRIS",
        "CHRISTIAN",
        "CHRISTOPHER",
        "CHUCK",
        "CLAIR",
        "CLARENCE",
        "CLARK",
        "CLAUDE",
        "CLAY",
        "CLAYTON",
        "CLEMENT",
        "CLEO",
        "CLEVELAND",
        "CLIFF",
        "CLIFFORD",
        "CLIFTON",
        "CLINT",
        "CLINTON",
        "CLYDE",
        "CODY",
        "COLBY",
        "COLE",
        "COLIN",
        "COLLIN",
        "CONRAD",
        "COREY",
        "CORNELIUS",
        "CORNELL",
        "CORY",
        "COURTNEY",
        "COY",
        "CRAIG",
        "CRUZ",
        "CURT",
        "CURTIS",
        "DALE",
        "DALLAS",
        "DALTON",
        "DAMIAN",
        "DAMIEN",
        "DAMON",
        "DAN",
        "DANA",
        "DANE",
        "DANIAL",
        "DANIEL",
        "DANNY",
        "DANTE",
        "DAREN",
        "DARIN",
        "DARIUS",
        "DARNELL",
        "DARREL",
        "DARRELL",
        "DARREN",
        "DARRIN",
        "DARRYL",
        "DARWIN",
        "DARYL",
        "DAVE",
        "DAVID",
        "DAVIS",
        "DEAN",
        "DELBERT",
        "DELMAR",
        "DEMETRIUS",
        "DENIS",
        "DENNIS",
        "DENNY",
        "DENVER",
        "DEREK",
        "DERICK",
        "DERRICK",
        "DESMOND",
        "DEVIN",
        "DEVON",
        "DEWAYNE",
        "DEWEY",
        "DEXTER",
        "DICK",
        "DIEGO",
        "DION",
        "DIRK",
        "DOMINGO",
        "DOMINIC",
        "DOMINICK",
        "DOMINIQUE",
        "DON",
        "DONALD",
        "DONNELL",
        "DONNIE",
        "DONNY",
        "DONOVAN",
        "DOUG",
        "DOUGLAS",
        "DOYLE",
        "DREW",
        "DUANE",
        "DUDLEY",
        "DUSTIN",
        "DWAYNE",
        "DWIGHT",
        "DYLAN",
        "EARL",
        "EARNEST",
        "ED",
        "EDDIE",
        "EDDY",
        "EDGAR",
        "EDMOND",
        "EDMUND",
        "EDUARDO",
        "EDWARD",
        "EDWARDO",
        "EDWIN",
        "EFRAIN",
        "ELBERT",
        "ELDON",
        "ELI",
        "ELIAS",
        "ELIJAH",
        "ELLIOT",
        "ELLIOTT",
        "ELLIS",
        "ELMER",
        "ELTON",
        "ELVIN",
        "ELVIS",
        "ELWOOD",
        "EMANUEL",
        "EMERSON",
        "EMERY",
        "EMIL",
        "EMILIO",
        "EMMANUEL",
        "EMMETT",
        "EMORY",
        "ENRIQUE",
        "ERIC",
        "ERICK",
        "ERIK",
        "ERNEST",
        "ERNESTO",
        "ERNIE",
        "ERROL",
        "ERVIN",
        "ERWIN",
        "ESTEBAN",
        "ETHAN",
        "EUGENE",
        "EVAN",
        "EVERETT",
        "FABIAN",
        "FEDERICO",
        "FELIPE",
        "FELIX",
        "FERNANDO",
        "FIDEL",
        "FLETCHER",
        "FLOYD",
        "FORREST",
        "FRANCIS",
        "FRANCISCO",
        "FRANK",
        "FRANKIE",
        "FRANKLIN",
        "FRED",
        "FREDDIE",
        "FREDDY",
        "FREDERIC",
        "FREDERICK",
        "FREDRICK",
        "GABRIEL",
        "GALE",
        "GALEN",
        "GARLAND",
        "GARRETT",
        "GARRY",
        "GARY",
        "GAVIN",
        "GENARO",
        "GENE",
        "GEOFFREY",
        "GEORGE",
        "GERALD",
        "GERARD",
        "GERARDO",
        "GERMAN",
        "GERRY",
        "GILBERT",
        "GILBERTO",
        "GLEN",
        "GLENN",
        "GONZALO",
        "GORDON",
        "GRADY",
        "GRAHAM",
        "GRANT",
        "GREG",
        "GREGG",
        "GREGORIO",
        "GREGORY",
        "GROVER",
        "GUADALUPE",
        "GUILLERMO",
        "GUS",
        "GUSTAVO",
        "GUY",
        "HAL",
        "HANS",
        "HARLAN",
        "HARLEY",
        "HAROLD",
        "HARRIS",
        "HARRISON",
        "HARRY",
        "HARVEY",
        "HEATH",
        "HECTOR",
        "HENRY",
        "HERBERT",
        "HERIBERTO",
        "HERMAN",
        "HIRAM",
        "HOLLIS",
        "HOMER",
        "HORACE",
        "HOUSTON",
        "HOWARD",
        "HUBERT",
        "HUGH",
        "HUGO",
        "HUMBERTO",
        "HUNG",
        "HUNTER",
        "IAN",
        "IGNACIO",
        "IRA",
        "IRVIN",
        "IRVING",
        "IRWIN",
        "ISAAC",
        "ISAIAH",
        "ISIDRO",
        "ISMAEL",
        "ISRAEL",
        "ISSAC",
        "IVAN",
        "JACK",
        "JACKIE",
        "JACKSON",
        "JACOB",
        "JACQUES",
        "JAIME",
        "JAKE",
        "JAMAL",
        "JAME",
        "JAMES",
        "JAMIE",
        "JAN",
        "JARED",
        "JARROD",
        "JARVIS",
        "JASON",
        "JASPER",
        "JAVIER",
        "JAY",
        "JAYSON",
        "JEAN",
        "JEFF",
        "JEFFERSON",
        "JEFFERY",
        "JEFFREY",
        "JEFFRY",
        "JERALD",
        "JEREMIAH",
        "JEREMY",
        "JERMAINE",
        "JEROME",
        "JERRY",
        "JESS",
        "JESSE",
        "JESSIE",
        "JESUS",
        "JIM",
        "JIMMIE",
        "JIMMY",
        "JOAN",
        "JOAQUIN",
        "JODY",
        "JOE",
        "JOEL",
        "JOESPH",
        "JOEY",
        "JOHN",
        "JOHNATHAN",
        "JOHNATHON",
        "JOHNNIE",
        "JOHNNY",
        "JON",
        "JONATHAN",
        "JONATHON",
        "JORDAN",
        "JORGE",
        "JOSE",
        "JOSEPH",
        "JOSH",
        "JOSHUA",
        "JOSUE",
        "JUAN",
        "JULIAN",
        "JULIO",
        "JULIUS",
        "JUNIOR",
        "JUSTIN",
        "KARL",
        "KEITH",
        "KELLY",
        "KELVIN",
        "KEN",
        "KENDALL",
        "KENDRICK",
        "KENNETH",
        "KENNY",
        "KENT",
        "KERMIT",
        "KERRY",
        "KEVIN",
        "KIM",
        "KIRBY",
        "KIRK",
        "KRIS",
        "KRISTOPHER",
        "KURT",
        "KURTIS",
        "KYLE",
        "LAMAR",
        "LAMONT",
        "LANCE",
        "LANDON",
        "LANE",
        "LARRY",
        "LAURENCE",
        "LAVERNE",
        "LAWRENCE",
        "LEE",
        "LELAND",
        "LEO",
        "LEON",
        "LEONARD",
        "LEONARDO",
        "LEONEL",
        "LEROY",
        "LESLIE",
        "LESTER",
        "LEVI",
        "LEWIS",
        "LINCOLN",
        "LINWOOD",
        "LIONEL",
        "LLOYD",
        "LOGAN",
        "LONNIE",
        "LOREN",
        "LORENZO",
        "LOUIE",
        "LOUIS",
        "LOWELL",
        "LOYD",
        "LUCAS",
        "LUIS",
        "LUKE",
        "LUTHER",
        "LYLE",
        "LYNN",
        "MACK",
        "MALCOLM",
        "MANUEL",
        "MARC",
        "MARCEL",
        "MARCELINO",
        "MARCO",
        "MARCOS",
        "MARCUS",
        "MARIANO",
        "MARIO",
        "MARION",
        "MARK",
        "MARLIN",
        "MARLON",
        "MARSHALL",
        "MARTIN",
        "MARTY",
        "MARVIN",
        "MARY",
        "MASON",
        "MATHEW",
        "MATT",
        "MATTHEW",
        "MAURICE",
        "MAURICIO",
        "MAX",
        "MAXWELL",
        "MAYNARD",
        "MELVIN",
        "MERLE",
        "MERLIN",
        "MERRILL",
        "MICAH",
        "MICHAEL",
        "MICHEAL",
        "MICHEL",
        "MICKEY",
        "MIGUEL",
        "MIKE",
        "MILES",
        "MILLARD",
        "MILTON",
        "MITCHELL",
        "MOHAMMAD",
        "MOISES",
        "MONROE",
        "MONTE",
        "MONTY",
        "MORGAN",
        "MORRIS",
        "MOSES",
        "MURRAY",
        "MYRON",
        "NATHAN",
        "NATHANIEL",
        "NEAL",
        "NED",
        "NEIL",
        "NELSON",
        "NESTOR",
        "NICHOLAS",
        "NICK",
        "NICKOLAS",
        "NICOLAS",
        "NOAH",
        "NOE",
        "NOEL",
        "NOLAN",
        "NORBERT",
        "NORMAN",
        "NORRIS",
        "NUMBERS",
        "OCTAVIO",
        "ODELL",
        "OLIVER",
        "OLLIE",
        "OMAR",
        "ORLANDO",
        "ORVILLE",
        "OSCAR",
        "OTIS",
        "OTTO",
        "OWEN",
        "PABLO",
        "PASQUALE",
        "PAT",
        "PATRICK",
        "PAUL",
        "PEDRO",
        "PERCY",
        "PERRY",
        "PETE",
        "PETER",
        "PHIL",
        "PHILIP",
        "PHILLIP",
        "PIERRE",
        "PRESTON",
        "QUENTIN",
        "QUINCY",
        "QUINTON",
        "RAFAEL",
        "RALPH",
        "RAMIRO",
        "RAMON",
        "RANDAL",
        "RANDALL",
        "RANDOLPH",
        "RANDY",
        "RAPHAEL",
        "RAUL",
        "RAY",
        "RAYMOND",
        "RAYMUNDO",
        "REED",
        "REGGIE",
        "REGINALD",
        "RENE",
        "REUBEN",
        "REX",
        "REYNALDO",
        "RICARDO",
        "RICHARD",
        "RICK",
        "RICKEY",
        "RICKIE",
        "RICKY",
        "RIGOBERTO",
        "RILEY",
        "ROB",
        "ROBBIE",
        "ROBBY",
        "ROBERT",
        "ROBERTO",
        "ROBIN",
        "ROCCO",
        "ROCKY",
        "ROD",
        "RODERICK",
        "RODGER",
        "RODNEY",
        "RODOLFO",
        "RODRIGO",
        "ROGELIO",
        "ROGER",
        "ROLAND",
        "ROLANDO",
        "ROMAN",
        "ROMEO",
        "RON",
        "RONALD",
        "RONNIE",
        "ROOSEVELT",
        "RORY",
        "ROSCOE",
        "ROSS",
        "ROY",
        "ROYCE",
        "RUBEN",
        "RUDOLPH",
        "RUDY",
        "RUFUS",
        "RUSSEL",
        "RUSSELL",
        "RUSTY",
        "RYAN",
        "SALVADOR",
        "SALVATORE",
        "SAM",
        "SAMMIE",
        "SAMMY",
        "SAMUEL",
        "SANFORD",
        "SANTIAGO",
        "SANTOS",
        "SAUL",
        "SCOT",
        "SCOTT",
        "SCOTTY",
        "SEAN",
        "SEBASTIAN",
        "SERGIO",
        "SETH",
        "SHANE",
        "SHANNON",
        "SHAUN",
        "SHAWN",
        "SHELBY",
        "SHELDON",
        "SHELTON",
        "SHERMAN",
        "SIDNEY",
        "SILAS",
        "SIMON",
        "SOLOMON",
        "SON",
        "SONNY",
        "SPENCER",
        "STACEY",
        "STACY",
        "STAN",
        "STANLEY",
        "STEFAN",
        "STEPHAN",
        "STEPHEN",
        "STERLING",
        "STEVE",
        "STEVEN",
        "STEWART",
        "STUART",
        "SYLVESTER",
        "TAYLOR",
        "TED",
        "TEDDY",
        "TERENCE",
        "TERRANCE",
        "TERRELL",
        "TERRENCE",
        "TERRY",
        "THADDEUS",
        "THEODORE",
        "THERON",
        "THOMAS",
        "THURMAN",
        "TIM",
        "TIMMY",
        "TIMOTHY",
        "TOBY",
        "TODD",
        "TOM",
        "TOMAS",
        "TOMMIE",
        "TOMMY",
        "TONY",
        "TRACY",
        "TRAVIS",
        "TRENT",
        "TRENTON",
        "TREVOR",
        "TRISTAN",
        "TROY",
        "TRUMAN",
        "TY",
        "TYLER",
        "TYRONE",
        "TYSON",
        "ULYSSES",
        "VAN",
        "VANCE",
        "VAUGHN",
        "VERN",
        "VERNON",
        "VICENTE",
        "VICTOR",
        "VINCE",
        "VINCENT",
        "VIRGIL",
        "VITO",
        "WADE",
        "WALLACE",
        "WALTER",
        "WARD",
        "WARREN",
        "WAYNE",
        "WELDON",
        "WENDELL",
        "WESLEY",
        "WILBERT",
        "WILBUR",
        "WILEY",
        "WILFORD",
        "WILFRED",
        "WILFREDO",
        "WILL",
        "WILLARD",
        "WILLIAM",
        "WILLIAMS",
        "WILLIE",
        "WILLIS",
        "WILMER",
        "WILSON",
        "WINFRED",
        "WINSTON",
        "WM",
        "WOODROW",
        "XAVIER",
        "ZACHARY",
        "ZACHERY"
        };
        private static List<string> _ladies = new List<string> {
        "ABBIE",
        "ABBY",
        "ABIGAIL",
        "ADA",
        "ADDIE",
        "ADELA",
        "ADELAIDE",
        "ADELE",
        "ADELINE",
        "ADRIAN",
        "ADRIANA",
        "ADRIENNE",
        "AGNES",
        "AIDA",
        "AILEEN",
        "AIMEE",
        "AISHA",
        "ALANA",
        "ALBA",
        "ALBERTA",
        "ALEJANDRA",
        "ALEXANDRA",
        "ALEXANDRIA",
        "ALEXIS",
        "ALFREDA",
        "ALICE",
        "ALICIA",
        "ALINE",
        "ALISA",
        "ALISHA",
        "ALISON",
        "ALISSA",
        "ALLENE",
        "ALLIE",
        "ALLISON",
        "ALLYSON",
        "ALMA",
        "ALTA",
        "ALTHEA",
        "ALYCE",
        "ALYSON",
        "ALYSSA",
        "AMALIA",
        "AMANDA",
        "AMBER",
        "AMELIA",
        "AMIE",
        "AMPARO",
        "AMY",
        "ANA",
        "ANASTASIA",
        "ANDREA",
        "ANGEL",
        "ANGELA",
        "ANGELIA",
        "ANGELICA",
        "ANGELINA",
        "ANGELINE",
        "ANGELIQUE",
        "ANGELITA",
        "ANGIE",
        "ANITA",
        "ANN",
        "ANNA",
        "ANNABELLE",
        "ANNE",
        "ANNETTE",
        "ANNIE",
        "ANNMARIE",
        "ANTIONETTE",
        "ANTOINETTE",
        "ANTONIA",
        "APRIL",
        "ARACELI",
        "ARLENE",
        "ARLINE",
        "ASHLEE",
        "ASHLEIGH",
        "ASHLEY",
        "AUDRA",
        "AUDREY",
        "AUGUSTA",
        "AURELIA",
        "AURORA",
        "AUTUMN",
        "AVA",
        "AVIS",
        "BARBARA",
        "BARBRA",
        "BEATRICE",
        "BEATRIZ",
        "BECKY",
        "BELINDA",
        "BENITA",
        "BERNADETTE",
        "BERNADINE",
        "BERNICE",
        "BERTA",
        "BERTHA",
        "BERTIE",
        "BERYL",
        "BESSIE",
        "BETH",
        "BETHANY",
        "BETSY",
        "BETTE",
        "BETTIE",
        "BETTY",
        "BETTYE",
        "BEULAH",
        "BEVERLEY",
        "BEVERLY",
        "BIANCA",
        "BILLIE",
        "BLANCA",
        "BLANCHE",
        "BOBBI",
        "BOBBIE",
        "BOBBY",
        "BONITA",
        "BONNIE",
        "BRANDI",
        "BRANDIE",
        "BRANDY",
        "BRENDA",
        "BRIANA",
        "BRIANNA",
        "BRIDGET",
        "BRIDGETT",
        "BRIDGETTE",
        "BRIGITTE",
        "BRITNEY",
        "BRITTANY",
        "BRITTNEY",
        "BROOKE",
        "CAITLIN",
        "CALLIE",
        "CAMILLA",
        "CAMILLE",
        "CANDACE",
        "CANDICE",
        "CANDY",
        "CARA",
        "CAREY",
        "CARISSA",
        "CARLA",
        "CARLENE",
        "CARLY",
        "CARMELA",
        "CARMELLA",
        "CARMEN",
        "CAROL",
        "CAROLE",
        "CAROLINA",
        "CAROLINE",
        "CAROLYN",
        "CARRIE",
        "CARYN",
        "CASANDRA",
        "CASEY",
        "CASSANDRA",
        "CASSIE",
        "CATALINA",
        "CATHERINE",
        "CATHLEEN",
        "CATHRYN",
        "CATHY",
        "CECELIA",
        "CECILE",
        "CECILIA",
        "CELESTE",
        "CELIA",
        "CELINA",
        "CHANDRA",
        "CHARITY",
        "CHARLENE",
        "CHARLOTTE",
        "CHARMAINE",
        "CHASITY",
        "CHELSEA",
        "CHELSEY",
        "CHERI",
        "CHERIE",
        "CHERRY",
        "CHERYL",
        "CHRIS",
        "CHRISTA",
        "CHRISTI",
        "CHRISTIAN",
        "CHRISTIE",
        "CHRISTINA",
        "CHRISTINE",
        "CHRISTY",
        "CHRYSTAL",
        "CINDY",
        "CLAIRE",
        "CLARA",
        "CLARE",
        "CLARICE",
        "CLARISSA",
        "CLAUDETTE",
        "CLAUDIA",
        "CLAUDINE",
        "CLEO",
        "COLEEN",
        "COLETTE",
        "COLLEEN",
        "CONCEPCION",
        "CONCETTA",
        "CONNIE",
        "CONSTANCE",
        "CONSUELO",
        "CORA",
        "CORINA",
        "CORINE",
        "CORINNE",
        "CORNELIA",
        "CORRINE",
        "CORTNEY",
        "COURTNEY",
        "CRISTINA",
        "CRYSTAL",
        "CYNTHIA",
        "DAISY",
        "DALE",
        "DANA",
        "DANIELLE",
        "DAPHNE",
        "DARCY",
        "DARLA",
        "DARLENE",
        "DAWN",
        "DAYNA",
        "DEANA",
        "DEANN",
        "DEANNA",
        "DEANNE",
        "DEBBIE",
        "DEBORA",
        "DEBORAH",
        "DEBRA",
        "DEE",
        "DEENA",
        "DEIDRA",
        "DEIDRE",
        "DEIRDRE",
        "DELIA",
        "DELLA",
        "DELORES",
        "DELORIS",
        "DENA",
        "DENICE",
        "DENISE",
        "DESIREE",
        "DESSIE",
        "DIANA",
        "DIANE",
        "DIANN",
        "DIANNA",
        "DIANNE",
        "DINA",
        "DIONNE",
        "DIXIE",
        "DOLLIE",
        "DOLLY",
        "DOLORES",
        "DOMINIQUE",
        "DONA",
        "DONNA",
        "DORA",
        "DOREEN",
        "DORETHA",
        "DORIS",
        "DOROTHEA",
        "DOROTHY",
        "DORTHY",
        "EARLENE",
        "EARLINE",
        "EARNESTINE",
        "EBONY",
        "EDDIE",
        "EDITH",
        "EDNA",
        "EDWINA",
        "EDYTHE",
        "EFFIE",
        "EILEEN",
        "ELAINE",
        "ELBA",
        "ELDA",
        "ELEANOR",
        "ELENA",
        "ELINOR",
        "ELISA",
        "ELISABETH",
        "ELISE",
        "ELISHA",
        "ELIZA",
        "ELIZABETH",
        "ELLA",
        "ELLEN",
        "ELMA",
        "ELNORA",
        "ELOISE",
        "ELSA",
        "ELSIE",
        "ELVA",
        "ELVIA",
        "ELVIRA",
        "EMILIA",
        "EMILIE",
        "EMILY",
        "EMMA",
        "ENID",
        "ERICA",
        "ERICKA",
        "ERIKA",
        "ERIN",
        "ERMA",
        "ERNA",
        "ERNESTINE",
        "ESMERALDA",
        "ESPERANZA",
        "ESSIE",
        "ESTELA",
        "ESTELLA",
        "ESTELLE",
        "ESTER",
        "ESTHER",
        "ETHEL",
        "ETTA",
        "EUGENIA",
        "EULA",
        "EUNICE",
        "EVA",
        "EVANGELINA",
        "EVANGELINE",
        "EVE",
        "EVELYN",
        "FAITH",
        "FANNIE",
        "FANNY",
        "FAY",
        "FAYE",
        "FELECIA",
        "FELICIA",
        "FERN",
        "FLORA",
        "FLORENCE",
        "FLORINE",
        "FLOSSIE",
        "FRAN",
        "FRANCES",
        "FRANCESCA",
        "FRANCINE",
        "FRANCIS",
        "FRANCISCA",
        "FRANKIE",
        "FREDA",
        "FREIDA",
        "FRIEDA",
        "GABRIELA",
        "GABRIELLE",
        "GAIL",
        "GALE",
        "GAY",
        "GAYLA",
        "GAYLE",
        "GENA",
        "GENEVA",
        "GENEVIEVE",
        "GEORGETTE",
        "GEORGIA",
        "GEORGINA",
        "GERALDINE",
        "GERI",
        "GERMAINE",
        "GERTRUDE",
        "GILDA",
        "GINA",
        "GINGER",
        "GLADYS",
        "GLENDA",
        "GLENNA",
        "GLORIA",
        "GOLDIE",
        "GRACE",
        "GRACIE",
        "GRACIELA",
        "GRETA",
        "GRETCHEN",
        "GUADALUPE",
        "GUSSIE",
        "GWEN",
        "GWENDOLYN",
        "HALEY",
        "HALLIE",
        "HANNAH",
        "HARRIET",
        "HARRIETT",
        "HATTIE",
        "HAZEL",
        "HEATHER",
        "HEIDI",
        "HELEN",
        "HELENA",
        "HELENE",
        "HELGA",
        "HENRIETTA",
        "HERMINIA",
        "HESTER",
        "HILARY",
        "HILDA",
        "HILLARY",
        "HOLLIE",
        "HOLLY",
        "HOPE",
        "IDA",
        "ILA",
        "ILENE",
        "IMELDA",
        "IMOGENE",
        "INA",
        "INES",
        "INEZ",
        "INGRID",
        "IRENE",
        "IRIS",
        "IRMA",
        "ISABEL",
        "ISABELLA",
        "ISABELLE",
        "IVA",
        "IVY",
        "JACKIE",
        "JACKLYN",
        "JACLYN",
        "JACQUELINE",
        "JACQUELYN",
        "JADE",
        "JAIME",
        "JAMES",
        "JAMI",
        "JAMIE",
        "JAN",
        "JANA",
        "JANE",
        "JANELL",
        "JANELLE",
        "JANET",
        "JANETTE",
        "JANICE",
        "JANIE",
        "JANINE",
        "JANIS",
        "JANNA",
        "JANNIE",
        "JASMIN",
        "JASMINE",
        "JAYNE",
        "JEAN",
        "JEANETTE",
        "JEANIE",
        "JEANINE",
        "JEANNE",
        "JEANNETTE",
        "JEANNIE",
        "JEANNINE",
        "JENIFER",
        "JENNA",
        "JENNIE",
        "JENNIFER",
        "JENNY",
        "JERI",
        "JERRI",
        "JERRY",
        "JESSE",
        "JESSICA",
        "JESSIE",
        "JEWEL",
        "JEWELL",
        "JILL",
        "JILLIAN",
        "JIMMIE",
        "JO",
        "JOAN",
        "JOANN",
        "JOANNA",
        "JOANNE",
        "JOCELYN",
        "JODI",
        "JODIE",
        "JODY",
        "JOHANNA",
        "JOHN",
        "JOHNNIE",
        "JOLENE",
        "JONI",
        "JORDAN",
        "JOSEFA",
        "JOSEFINA",
        "JOSEPHINE",
        "JOSIE",
        "JOY",
        "JOYCE",
        "JUANA",
        "JUANITA",
        "JUDI",
        "JUDITH",
        "JUDY",
        "JULIA",
        "JULIANA",
        "JULIANNE",
        "JULIE",
        "JULIET",
        "JULIETTE",
        "JUNE",
        "JUSTINA",
        "JUSTINE",
        "KAITLIN",
        "KAITLYN",
        "KARA",
        "KAREN",
        "KARI",
        "KARIN",
        "KARINA",
        "KARLA",
        "KARYN",
        "KASEY",
        "KATE",
        "KATELYN",
        "KATHARINE",
        "KATHERINE",
        "KATHERYN",
        "KATHI",
        "KATHIE",
        "KATHLEEN",
        "KATHRINE",
        "KATHRYN",
        "KATHY",
        "KATIE",
        "KATINA",
        "KATRINA",
        "KATY",
        "KAY",
        "KAYE",
        "KAYLA",
        "KEISHA",
        "KELLEY",
        "KELLI",
        "KELLIE",
        "KELLY",
        "KELSEY",
        "KENDRA",
        "KENYA",
        "KERI",
        "KERRI",
        "KERRY",
        "KIM",
        "KIMBERLEE",
        "KIMBERLEY",
        "KIMBERLY",
        "KIRSTEN",
        "KITTY",
        "KRIS",
        "KRISTA",
        "KRISTEN",
        "KRISTI",
        "KRISTIE",
        "KRISTIN",
        "KRISTINA",
        "KRISTINE",
        "KRISTY",
        "KRYSTAL",
        "LACEY",
        "LACY",
        "LADONNA",
        "LAKEISHA",
        "LAKESHA",
        "LAKISHA",
        "LANA",
        "LARA",
        "LASHONDA",
        "LATANYA",
        "LATASHA",
        "LATISHA",
        "LATONYA",
        "LATOYA",
        "LAURA",
        "LAUREL",
        "LAUREN",
        "LAURI",
        "LAURIE",
        "LAVERNE",
        "LAVONNE",
        "LAWANDA",
        "LEA",
        "LEAH",
        "LEANN",
        "LEANNA",
        "LEANNE",
        "LEE",
        "LEEANN",
        "LEIGH",
        "LEILA",
        "LELA",
        "LELIA",
        "LENA",
        "LENORA",
        "LENORE",
        "LEOLA",
        "LEONA",
        "LEONOR",
        "LESA",
        "LESLEY",
        "LESLIE",
        "LESSIE",
        "LETA",
        "LETHA",
        "LETICIA",
        "LETITIA",
        "LIBBY",
        "LIDIA",
        "LILA",
        "LILIA",
        "LILIAN",
        "LILIANA",
        "LILLIAN",
        "LILLIE",
        "LILLY",
        "LILY",
        "LINA",
        "LINDA",
        "LINDSAY",
        "LINDSEY",
        "LISA",
        "LIZ",
        "LIZA",
        "LIZZIE",
        "LOIS",
        "LOLA",
        "LOLITA",
        "LORA",
        "LORAINE",
        "LORENA",
        "LORENE",
        "LORETTA",
        "LORI",
        "LORIE",
        "LORNA",
        "LORRAINE",
        "LORRIE",
        "LOTTIE",
        "LOU",
        "LOUELLA",
        "LOUISA",
        "LOUISE",
        "LOURDES",
        "LUANN",
        "LUCIA",
        "LUCILE",
        "LUCILLE",
        "LUCINDA",
        "LUCY",
        "LUELLA",
        "LUISA",
        "LULA",
        "LUPE",
        "LUZ",
        "LYDIA",
        "LYNDA",
        "LYNETTE",
        "LYNN",
        "LYNNE",
        "LYNNETTE",
        "MA",
        "MABEL",
        "MABLE",
        "MADELEINE",
        "MADELINE",
        "MADELYN",
        "MADGE",
        "MAE",
        "MAGDALENA",
        "MAGGIE",
        "MAI",
        "MALINDA",
        "MALLORY",
        "MAMIE",
        "MANDY",
        "MANUELA",
        "MARA",
        "MARCELLA",
        "MARCI",
        "MARCIA",
        "MARCIE",
        "MARCY",
        "MARGARET",
        "MARGARITA",
        "MARGERY",
        "MARGIE",
        "MARGO",
        "MARGOT",
        "MARGRET",
        "MARGUERITE",
        "MARI",
        "MARIA",
        "MARIAN",
        "MARIANA",
        "MARIANNE",
        "MARIBEL",
        "MARICELA",
        "MARIE",
        "MARIETTA",
        "MARILYN",
        "MARINA",
        "MARION",
        "MARISA",
        "MARISOL",
        "MARISSA",
        "MARITZA",
        "MARJORIE",
        "MARLA",
        "MARLENE",
        "MARQUITA",
        "MARSHA",
        "MARTA",
        "MARTHA",
        "MARTINA",
        "MARVA",
        "MARY",
        "MARYANN",
        "MARYANNE",
        "MARYELLEN",
        "MARYLOU",
        "MATILDA",
        "MATTIE",
        "MAUDE",
        "MAURA",
        "MAUREEN",
        "MAVIS",
        "MAXINE",
        "MAY",
        "MAYRA",
        "MEAGAN",
        "MEGAN",
        "MEGHAN",
        "MELANIE",
        "MELBA",
        "MELINDA",
        "MELISA",
        "MELISSA",
        "MELLISA",
        "MELODY",
        "MELVA",
        "MERCEDES",
        "MEREDITH",
        "MERLE",
        "MIA",
        "MICHAEL",
        "MICHAELA",
        "MICHELE",
        "MICHELL",
        "MICHELLE",
        "MILAGROS",
        "MILDRED",
        "MILLICENT",
        "MILLIE",
        "MINA",
        "MINDY",
        "MINERVA",
        "MINNIE",
        "MIRANDA",
        "MIRIAM",
        "MISTY",
        "MITZI",
        "MOLLIE",
        "MOLLY",
        "MONA",
        "MONICA",
        "MONIKA",
        "MONIQUE",
        "MORGAN",
        "MURIEL",
        "MYRA",
        "MYRNA",
        "MYRTLE",
        "NADIA",
        "NADINE",
        "NAN",
        "NANCY",
        "NANETTE",
        "NANNIE",
        "NAOMI",
        "NATALIA",
        "NATALIE",
        "NATASHA",
        "NELDA",
        "NELL",
        "NELLIE",
        "NELLY",
        "NETTIE",
        "NEVA",
        "NICHOLE",
        "NICOLE",
        "NIKKI",
        "NINA",
        "NITA",
        "NOELLE",
        "NOEMI",
        "NOLA",
        "NONA",
        "NORA",
        "NOREEN",
        "NORMA",
        "OCTAVIA",
        "ODESSA",
        "OFELIA",
        "OLA",
        "OLGA",
        "OLIVE",
        "OLIVIA",
        "OLLIE",
        "OPAL",
        "OPHELIA",
        "ORA",
        "PAIGE",
        "PAM",
        "PAMALA",
        "PAMELA",
        "PANSY",
        "PAT",
        "PATRICA",
        "PATRICE",
        "PATRICIA",
        "PATSY",
        "PATTI",
        "PATTY",
        "PAULA",
        "PAULETTE",
        "PAULINE",
        "PEARL",
        "PEARLIE",
        "PEGGY",
        "PENELOPE",
        "PENNY",
        "PETRA",
        "PHOEBE",
        "PHYLLIS",
        "POLLY",
        "PRISCILLA",
        "QUEEN",
        "RACHAEL",
        "RACHEL",
        "RACHELLE",
        "RAE",
        "RAMONA",
        "RANDI",
        "RAQUEL",
        "REBA",
        "REBECCA",
        "REBEKAH",
        "REGINA",
        "RENA",
        "RENAE",
        "RENE",
        "RENEE",
        "REVA",
        "REYNA",
        "RHEA",
        "RHODA",
        "RHONDA",
        "RITA",
        "ROBBIE",
        "ROBERT",
        "ROBERTA",
        "ROBIN",
        "ROBYN",
        "ROCHELLE",
        "ROCIO",
        "RONDA",
        "ROSA",
        "ROSALIA",
        "ROSALIE",
        "ROSALIND",
        "ROSALINDA",
        "ROSALYN",
        "ROSANNA",
        "ROSANNE",
        "ROSARIO",
        "ROSE",
        "ROSEANN",
        "ROSELLA",
        "ROSEMARIE",
        "ROSEMARY",
        "ROSETTA",
        "ROSIE",
        "ROSLYN",
        "ROWENA",
        "ROXANNE",
        "ROXIE",
        "RUBY",
        "RUTH",
        "RUTHIE",
        "SABRINA",
        "SADIE",
        "SALLIE",
        "SALLY",
        "SAMANTHA",
        "SANDRA",
        "SANDY",
        "SARA",
        "SARAH",
        "SASHA",
        "SAUNDRA",
        "SAVANNAH",
        "SELENA",
        "SELINA",
        "SELMA",
        "SERENA",
        "SHANA",
        "SHANNA",
        "SHANNON",
        "SHARI",
        "SHARLENE",
        "SHARON",
        "SHARRON",
        "SHAUNA",
        "SHAWN",
        "SHAWNA",
        "SHEENA",
        "SHEILA",
        "SHELBY",
        "SHELIA",
        "SHELLEY",
        "SHELLY",
        "SHEREE",
        "SHERI",
        "SHERRI",
        "SHERRIE",
        "SHERRY",
        "SHERYL",
        "SHIRLEY",
        "SIERRA",
        "SILVIA",
        "SIMONE",
        "SOCORRO",
        "SOFIA",
        "SONDRA",
        "SONIA",
        "SONJA",
        "SONYA",
        "SOPHIA",
        "SOPHIE",
        "STACEY",
        "STACI",
        "STACIE",
        "STACY",
        "STEFANIE",
        "STELLA",
        "STEPHANIE",
        "SUE",
        "SUMMER",
        "SUSAN",
        "SUSANA",
        "SUSANNA",
        "SUSANNE",
        "SUSIE",
        "SUZANNE",
        "SUZETTE",
        "SYBIL",
        "SYDNEY",
        "SYLVIA",
        "TABATHA",
        "TABITHA",
        "TAMARA",
        "TAMEKA",
        "TAMERA",
        "TAMI",
        "TAMIKA",
        "TAMMI",
        "TAMMIE",
        "TAMMY",
        "TAMRA",
        "TANIA",
        "TANISHA",
        "TANYA",
        "TARA",
        "TASHA",
        "TAYLOR",
        "TERESA",
        "TERI",
        "TERRA",
        "TERRI",
        "TERRIE",
        "TERRY",
        "TESSA",
        "THELMA",
        "THERESA",
        "THERESE",
        "TIA",
        "TIFFANY",
        "TINA",
        "TISHA",
        "TOMMIE",
        "TONI",
        "TONIA",
        "TONYA",
        "TORI",
        "TRACEY",
        "TRACI",
        "TRACIE",
        "TRACY",
        "TRICIA",
        "TRINA",
        "TRISHA",
        "TRUDY",
        "TWILA",
        "URSULA",
        "VALARIE",
        "VALERIA",
        "VALERIE",
        "VANESSA",
        "VELMA",
        "VERA",
        "VERNA",
        "VERONICA",
        "VICKI",
        "VICKIE",
        "VICKY",
        "VICTORIA",
        "VILMA",
        "VIOLA",
        "VIOLET",
        "VIRGIE",
        "VIRGINIA",
        "VIVIAN",
        "VONDA",
        "WANDA",
        "WENDI",
        "WENDY",
        "WHITNEY",
        "WILDA",
        "WILLA",
        "WILLIE",
        "WILMA",
        "WINIFRED",
        "WINNIE",
        "YESENIA",
        "YOLANDA",
        "YOUNG",
        "YVETTE",
        "YVONNE",
        "ZELDA",
        "ZELMA"
                };
                private static List<string> _lastNames = new List<string> {
                "SMITH",
        "JOHNSON",
        "WILLIAMS",
        "BROWN",
        "JONES",
        "MILLER",
        "DAVIS",
        "GARCIA",
        "RODRIGUEZ",
        "WILSON",
        "MARTINEZ",
        "ANDERSON",
        "TAYLOR",
        "THOMAS",
        "HERNANDEZ",
        "MOORE",
        "MARTIN",
        "JACKSON",
        "THOMPSON",
        "WHITE",
        "LOPEZ",
        "LEE",
        "GONZALEZ",
        "HARRIS",
        "CLARK",
        "LEWIS",
        "ROBINSON",
        "WALKER",
        "PEREZ",
        "HALL",
        "YOUNG",
        "ALLEN",
        "SANCHEZ",
        "WRIGHT",
        "KING",
        "SCOTT",
        "GREEN",
        "BAKER",
        "ADAMS",
        "NELSON",
        "HILL",
        "RAMIREZ",
        "CAMPBELL",
        "MITCHELL",
        "ROBERTS",
        "CARTER",
        "PHILLIPS",
        "EVANS",
        "TURNER",
        "TORRES",
        "PARKER",
        "COLLINS",
        "EDWARDS",
        "STEWART",
        "FLORES",
        "MORRIS",
        "NGUYEN",
        "MURPHY",
        "RIVERA",
        "COOK",
        "ROGERS",
        "MORGAN",
        "PETERSON",
        "COOPER",
        "REED",
        "BAILEY",
        "BELL",
        "GOMEZ",
        "KELLY",
        "HOWARD",
        "WARD",
        "COX",
        "DIAZ",
        "RICHARDSON",
        "WOOD",
        "WATSON",
        "BROOKS",
        "BENNETT",
        "GRAY",
        "JAMES",
        "REYES",
        "CRUZ",
        "HUGHES",
        "PRICE",
        "MYERS",
        "LONG",
        "FOSTER",
        "SANDERS",
        "ROSS",
        "MORALES",
        "POWELL",
        "SULLIVAN",
        "RUSSELL",
        "ORTIZ",
        "JENKINS",
        "GUTIERREZ",
        "PERRY",
        "BUTLER",
        "BARNES",
        "FISHER",
        "HENDERSON",
        "COLEMAN",
        "SIMMONS",
        "PATTERSON",
        "JORDAN",
        "REYNOLDS",
        "HAMILTON",
        "GRAHAM",
        "KIM",
        "GONZALES",
        "ALEXANDER",
        "RAMOS",
        "WALLACE",
        "GRIFFIN",
        "WEST",
        "COLE",
        "HAYES",
        "CHAVEZ",
        "GIBSON",
        "BRYANT",
        "ELLIS",
        "STEVENS",
        "MURRAY",
        "FORD",
        "MARSHALL",
        "OWENS",
        "MCDONALD",
        "HARRISON",
        "RUIZ",
        "KENNEDY",
        "WELLS",
        "ALVAREZ",
        "WOODS",
        "MENDOZA",
        "CASTILLO",
        "OLSON",
        "WEBB",
        "WASHINGTON",
        "TUCKER",
        "FREEMAN",
        "BURNS",
        "HENRY",
        "VASQUEZ",
        "SNYDER",
        "SIMPSON",
        "CRAWFORD",
        "JIMENEZ",
        "PORTER",
        "MASON",
        "SHAW",
        "GORDON",
        "WAGNER",
        "HUNTER",
        "ROMERO",
        "HICKS",
        "DIXON",
        "HUNT",
        "PALMER",
        "ROBERTSON",
        "BLACK",
        "HOLMES",
        "STONE",
        "MEYER",
        "BOYD",
        "MILLS",
        "WARREN",
        "FOX",
        "ROSE",
        "RICE",
        "MORENO",
        "SCHMIDT",
        "PATEL",
        "FERGUSON",
        "NICHOLS",
        "HERRERA",
        "MEDINA",
        "RYAN",
        "FERNANDEZ",
        "WEAVER",
        "DANIELS",
        "STEPHENS",
        "GARDNER",
        "PAYNE",
        "KELLEY",
        "DUNN",
        "PIERCE",
        "ARNOLD",
        "TRAN",
        "SPENCER",
        "PETERS",
        "HAWKINS",
        "GRANT",
        "HANSEN",
        "CASTRO",
        "HOFFMAN",
        "HART",
        "ELLIOTT",
        "CUNNINGHAM",
        "KNIGHT",
        "BRADLEY",
        "CARROLL",
        "HUDSON",
        "DUNCAN",
        "ARMSTRONG",
        "BERRY",
        "ANDREWS",
        "JOHNSTON",
        "RAY",
        "LANE",
        "RILEY",
        "CARPENTER",
        "PERKINS",
        "AGUILAR",
        "SILVA",
        "RICHARDS",
        "WILLIS",
        "MATTHEWS",
        "CHAPMAN",
        "LAWRENCE",
        "GARZA",
        "VARGAS",
        "WATKINS",
        "WHEELER",
        "LARSON",
        "CARLSON",
        "HARPER",
        "GEORGE",
        "GREENE",
        "BURKE",
        "GUZMAN",
        "MORRISON",
        "MUNOZ",
        "JACOBS",
        "OBRIEN",
        "LAWSON",
        "FRANKLIN",
        "LYNCH",
        "BISHOP",
        "CARR",
        "SALAZAR",
        "AUSTIN",
        "MENDEZ",
        "GILBERT",
        "JENSEN",
        "WILLIAMSON",
        "MONTGOMERY",
        "HARVEY",
        "OLIVER",
        "HOWELL",
        "DEAN",
        "HANSON",
        "WEBER",
        "GARRETT",
        "SIMS",
        "BURTON",
        "FULLER",
        "SOTO",
        "MCCOY",
        "WELCH",
        "CHEN",
        "SCHULTZ",
        "WALTERS",
        "REID",
        "FIELDS",
        "WALSH",
        "LITTLE",
        "FOWLER",
        "BOWMAN",
        "DAVIDSON",
        "MAY",
        "DAY",
        "SCHNEIDER",
        "NEWMAN",
        "BREWER",
        "LUCAS",
        "HOLLAND",
        "WONG",
        "BANKS",
        "SANTOS",
        "CURTIS",
        "PEARSON",
        "DELGADO",
        "VALDEZ",
        "PENA",
        "RIOS",
        "DOUGLAS",
        "SANDOVAL",
        "BARRETT",
        "HOPKINS",
        "KELLER",
        "GUERRERO",
        "STANLEY",
        "BATES",
        "ALVARADO",
        "BECK",
        "ORTEGA",
        "WADE",
        "ESTRADA",
        "CONTRERAS",
        "BARNETT",
        "CALDWELL",
        "SANTIAGO",
        "LAMBERT",
        "POWERS",
        "CHAMBERS",
        "NUNEZ",
        "CRAIG",
        "LEONARD",
        "LOWE",
        "RHODES",
        "BYRD",
        "GREGORY",
        "SHELTON",
        "FRAZIER",
        "BECKER",
        "MALDONADO",
        "FLEMING",
        "VEGA",
        "SUTTON",
        "COHEN",
        "JENNINGS",
        "PARKS",
        "MCDANIEL",
        "WATTS",
        "BARKER",
        "NORRIS",
        "VAUGHN",
        "VAZQUEZ",
        "HOLT",
        "SCHWARTZ",
        "STEELE",
        "BENSON",
        "NEAL",
        "DOMINGUEZ",
        "HORTON",
        "TERRY",
        "WOLFE",
        "HALE",
        "LYONS",
        "GRAVES",
        "HAYNES",
        "MILES",
        "PARK",
        "WARNER",
        "PADILLA",
        "BUSH",
        "THORNTON",
        "MCCARTHY",
        "MANN",
        "ZIMMERMAN",
        "ERICKSON",
        "FLETCHER",
        "MCKINNEY",
        "PAGE",
        "DAWSON",
        "JOSEPH",
        "MARQUEZ",
        "REEVES",
        "KLEIN",
        "ESPINOZA",
        "BALDWIN",
        "MORAN",
        "LOVE",
        "ROBBINS",
        "HIGGINS",
        "BALL",
        "CORTEZ",
        "LE",
        "GRIFFITH",
        "BOWEN",
        "SHARP",
        "CUMMINGS",
        "RAMSEY",
        "HARDY",
        "SWANSON",
        "BARBER",
        "ACOSTA",
        "LUNA",
        "CHANDLER",
        "BLAIR",
        "DANIEL",
        "CROSS",
        "SIMON",
        "DENNIS",
        "OCONNOR",
        "QUINN",
        "GROSS",
        "NAVARRO",
        "MOSS",
        "FITZGERALD",
        "DOYLE",
        "MCLAUGHLIN",
        "ROJAS",
        "RODGERS",
        "STEVENSON",
        "SINGH",
        "YANG",
        "FIGUEROA",
        "HARMON",
        "NEWTON",
        "PAUL",
        "MANNING",
        "GARNER",
        "MCGEE",
        "REESE",
        "FRANCIS",
        "BURGESS",
        "ADKINS",
        "GOODMAN",
        "CURRY",
        "BRADY",
        "CHRISTENSEN",
        "POTTER",
        "WALTON",
        "GOODWIN",
        "MULLINS",
        "MOLINA",
        "WEBSTER",
        "FISCHER",
        "CAMPOS",
        "AVILA",
        "SHERMAN",
        "TODD",
        "CHANG",
        "BLAKE",
        "MALONE",
        "WOLF",
        "HODGES",
        "JUAREZ",
        "GILL",
        "FARMER",
        "HINES",
        "GALLAGHER",
        "DURAN",
        "HUBBARD",
        "CANNON",
        "MIRANDA",
        "WANG",
        "SAUNDERS",
        "TATE",
        "MACK",
        "HAMMOND",
        "CARRILLO",
        "TOWNSEND",
        "WISE",
        "INGRAM",
        "BARTON",
        "MEJIA",
        "AYALA",
        "SCHROEDER",
        "HAMPTON",
        "ROWE",
        "PARSONS",
        "FRANK",
        "WATERS",
        "STRICKLAND",
        "OSBORNE",
        "MAXWELL",
        "CHAN",
        "DELEON",
        "NORMAN",
        "HARRINGTON",
        "CASEY",
        "PATTON",
        "LOGAN",
        "BOWERS",
        "MUELLER",
        "GLOVER",
        "FLOYD",
        "HARTMAN",
        "BUCHANAN",
        "COBB",
        "FRENCH",
        "KRAMER",
        "MCCORMICK",
        "CLARKE",
        "TYLER",
        "GIBBS",
        "MOODY",
        "CONNER",
        "SPARKS",
        "MCGUIRE",
        "LEON",
        "BAUER",
        "NORTON",
        "POPE",
        "FLYNN",
        "HOGAN",
        "ROBLES",
        "SALINAS",
        "YATES",
        "LINDSEY",
        "LLOYD",
        "MARSH",
        "MCBRIDE",
        "OWEN",
        "SOLIS",
        "PHAM",
        "LANG",
        "PRATT",
        "LARA",
        "BROCK",
        "BALLARD",
        "TRUJILLO",
        "SHAFFER",
        "DRAKE",
        "ROMAN",
        "AGUIRRE",
        "MORTON",
        "STOKES",
        "LAMB",
        "PACHECO",
        "PATRICK",
        "COCHRAN",
        "SHEPHERD",
        "CAIN",
        "BURNETT",
        "HESS",
        "LI",
        "CERVANTES",
        "OLSEN",
        "BRIGGS",
        "OCHOA",
        "CABRERA",
        "VELASQUEZ",
        "MONTOYA",
        "ROTH",
        "MEYERS",
        "CARDENAS",
        "FUENTES",
        "WEISS",
        "HOOVER",
        "WILKINS",
        "NICHOLSON",
        "UNDERWOOD",
        "SHORT",
        "CARSON",
        "MORROW",
        "COLON",
        "HOLLOWAY",
        "SUMMERS",
        "BRYAN",
        "PETERSEN",
        "MCKENZIE",
        "SERRANO",
        "WILCOX",
        "CAREY",
        "CLAYTON",
        "POOLE",
        "CALDERON",
        "GALLEGOS",
        "GREER",
        "RIVAS",
        "GUERRA",
        "DECKER",
        "COLLIER",
        "WALL",
        "WHITAKER",
        "BASS",
        "FLOWERS",
        "DAVENPORT",
        "CONLEY",
        "HOUSTON",
        "HUFF",
        "COPELAND",
        "HOOD",
        "MONROE",
        "MASSEY",
        "ROBERSON",
        "COMBS",
        "FRANCO",
        "LARSEN",
        "PITTMAN",
        "RANDALL",
        "SKINNER",
        "WILKINSON",
        "KIRBY",
        "CAMERON",
        "BRIDGES",
        "ANTHONY",
        "RICHARD",
        "KIRK",
        "BRUCE",
        "SINGLETON",
        "MATHIS",
        "BRADFORD",
        "BOONE",
        "ABBOTT",
        "CHARLES",
        "ALLISON",
        "SWEENEY",
        "ATKINSON",
        "HORN",
        "JEFFERSON",
        "ROSALES",
        "YORK",
        "CHRISTIAN",
        "PHELPS",
        "FARRELL",
        "CASTANEDA",
        "NASH",
        "DICKERSON",
        "BOND",
        "WYATT",
        "FOLEY",
        "CHASE",
        "GATES",
        "VINCENT",
        "MATHEWS",
        "HODGE",
        "GARRISON",
        "TREVINO",
        "VILLARREAL",
        "HEATH",
        "DALTON",
        "VALENCIA",
        "CALLAHAN",
        "HENSLEY",
        "ATKINS",
        "HUFFMAN",
        "ROY",
        "BOYER",
        "SHIELDS",
        "LIN",
        "HANCOCK",
        "GRIMES",
        "GLENN",
        "CLINE",
        "DELACRUZ",
        "CAMACHO",
        "DILLON",
        "PARRISH",
        "ONEILL",
        "MELTON",
        "BOOTH",
        "KANE",
        "BERG",
        "HARRELL",
        "PITTS",
        "SAVAGE",
        "WIGGINS",
        "BRENNAN",
        "SALAS",
        "MARKS",
        "RUSSO",
        "SAWYER",
        "BAXTER",
        "GOLDEN",
        "HUTCHINSON",
        "LIU",
        "WALTER",
        "MCDOWELL",
        "WILEY",
        "RICH",
        "HUMPHREY",
        "JOHNS",
        "KOCH",
        "SUAREZ",
        "HOBBS",
        "BEARD",
        "GILMORE",
        "IBARRA",
        "KEITH",
        "MACIAS",
        "KHAN",
        "ANDRADE",
        "WARE",
        "STEPHENSON",
        "HENSON",
        "WILKERSON",
        "DYER",
        "MCCLURE",
        "BLACKWELL",
        "MERCADO",
        "TANNER",
        "EATON",
        "CLAY",
        "BARRON",
        "BEASLEY",
        "ONEAL",
        "PRESTON",
        "SMALL",
        "WU",
        "ZAMORA",
        "MACDONALD",
        "VANCE",
        "SNOW",
        "MCCLAIN",
        "STAFFORD",
        "OROZCO",
        "BARRY",
        "ENGLISH",
        "SHANNON",
        "KLINE",
        "JACOBSON",
        "WOODARD",
        "HUANG",
        "KEMP",
        "MOSLEY",
        "PRINCE",
        "MERRITT",
        "HURST",
        "VILLANUEVA",
        "ROACH",
        "NOLAN",
        "LAM",
        "YODER",
        "MCCULLOUGH",
        "LESTER",
        "SANTANA",
        "VALENZUELA",
        "WINTERS",
        "BARRERA",
        "LEACH",
        "ORR",
        "BERGER",
        "MCKEE",
        "STRONG",
        "CONWAY",
        "STEIN",
        "WHITEHEAD",
        "BULLOCK",
        "ESCOBAR",
        "KNOX",
        "MEADOWS",
        "SOLOMON",
        "VELEZ",
        "ODONNELL",
        "KERR",
        "STOUT",
        "BLANKENSHIP",
        "BROWNING",
        "KENT",
        "LOZANO",
        "BARTLETT",
        "PRUITT",
        "BUCK",
        "BARR",
        "GAINES",
        "DURHAM",
        "GENTRY",
        "MCINTYRE",
        "SLOAN",
        "MELENDEZ",
        "ROCHA",
        "HERMAN",
        "SEXTON",
        "MOON",
        "HENDRICKS",
        "RANGEL",
        "STARK",
        "LOWERY",
        "HARDIN",
        "HULL",
        "SELLERS",
        "ELLISON",
        "CALHOUN",
        "GILLESPIE",
        "MORA",
        "KNAPP",
        "MCCALL",
        "MORSE",
        "DORSEY",
        "WEEKS",
        "NIELSEN",
        "LIVINGSTON",
        "LEBLANC",
        "MCLEAN",
        "BRADSHAW",
        "GLASS",
        "MIDDLETON",
        "BUCKLEY",
        "SCHAEFER",
        "FROST",
        "HOWE",
        "HOUSE",
        "MCINTOSH",
        "HO",
        "PENNINGTON",
        "REILLY",
        "HEBERT",
        "MCFARLAND",
        "HICKMAN",
        "NOBLE",
        "SPEARS",
        "CONRAD",
        "ARIAS",
        "GALVAN",
        "VELAZQUEZ",
        "HUYNH",
        "FREDERICK",
        "RANDOLPH",
        "CANTU",
        "FITZPATRICK",
        "MAHONEY",
        "PECK",
        "VILLA",
        "MICHAEL",
        "DONOVAN",
        "MCCONNELL",
        "WALLS",
        "BOYLE",
        "MAYER",
        "ZUNIGA",
        "GILES",
        "PINEDA",
        "PACE",
        "HURLEY",
        "MAYS",
        "MCMILLAN",
        "CROSBY",
        "AYERS",
        "CASE",
        "BENTLEY",
        "SHEPARD",
        "EVERETT",
        "PUGH",
        "DAVID",
        "MCMAHON",
        "DUNLAP",
        "BENDER",
        "HAHN",
        "HARDING",
        "ACEVEDO",
        "RAYMOND",
        "BLACKBURN",
        "DUFFY",
        "LANDRY",
        "DOUGHERTY",
        "BAUTISTA",
        "SHAH",
        "POTTS",
        "ARROYO",
        "VALENTINE",
        "MEZA",
        "GOULD",
        "VAUGHAN",
        "FRY",
        "RUSH",
        "AVERY",
        "HERRING",
        "DODSON",
        "CLEMENTS",
        "SAMPSON",
        "TAPIA",
        "BEAN",
        "LYNN",
        "CRANE",
        "FARLEY",
        "CISNEROS",
        "BENTON",
        "ASHLEY",
        "MCKAY",
        "FINLEY",
        "BEST",
        "BLEVINS",
        "FRIEDMAN",
        "MOSES",
        "SOSA",
        "BLANCHARD",
        "HUBER",
        "FRYE",
        "KRUEGER",
        "BERNARD",
        "ROSARIO",
        "RUBIO",
        "MULLEN",
        "BENJAMIN",
        "HALEY",
        "CHUNG",
        "MOYER",
        "CHOI",
        "HORNE",
        "YU",
        "WOODWARD",
        "ALI",
        "NIXON",
        "HAYDEN",
        "RIVERS",
        "ESTES",
        "MCCARTY",
        "RICHMOND",
        "STUART",
        "MAYNARD",
        "BRANDT",
        "OCONNELL",
        "HANNA",
        "SANFORD",
        "SHEPPARD",
        "CHURCH",
        "BURCH",
        "LEVY",
        "RASMUSSEN",
        "COFFEY",
        "PONCE",
        "FAULKNER",
        "DONALDSON",
        "SCHMITT",
        "NOVAK",
        "COSTA",
        "MONTES",
        "BOOKER",
        "CORDOVA",
        "WALLER",
        "ARELLANO",
        "MADDOX",
        "MATA",
        "BONILLA",
        "STANTON",
        "COMPTON",
        "KAUFMAN",
        "DUDLEY",
        "MCPHERSON",
        "BELTRAN",
        "DICKSON",
        "MCCANN",
        "VILLEGAS",
        "PROCTOR",
        "HESTER",
        "CANTRELL",
        "DAUGHERTY",
        "CHERRY",
        "BRAY",
        "DAVILA",
        "ROWLAND",
        "LEVINE",
        "MADDEN",
        "SPENCE",
        "GOOD",
        "IRWIN",
        "WERNER",
        "KRAUSE",
        "PETTY",
        "WHITNEY",
        "BAIRD",
        "HOOPER",
        "POLLARD",
        "ZAVALA",
        "JARVIS",
        "HOLDEN",
        "HAAS",
        "HENDRIX",
        "MCGRATH",
        "BIRD",
        "LUCERO",
        "TERRELL",
        "RIGGS",
        "JOYCE",
        "MERCER",
        "ROLLINS",
        "GALLOWAY",
        "DUKE",
        "ODOM",
        "ANDERSEN",
        "DOWNS",
        "HATFIELD",
        "BENITEZ",
        "ARCHER",
        "HUERTA",
        "TRAVIS",
        "MCNEIL",
        "HINTON",
        "ZHANG",
        "HAYS",
        "MAYO",
        "FRITZ",
        "BRANCH",
        "MOONEY",
        "EWING",
        "RITTER",
        "ESPARZA",
        "FREY",
        "BRAUN",
        "GAY",
        "RIDDLE",
        "HANEY",
        "KAISER",
        "HOLDER",
        "CHANEY",
        "MCKNIGHT",
        "GAMBLE",
        "VANG",
        "COOLEY",
        "CARNEY",
        "COWAN",
        "FORBES",
        "FERRELL",
        "DAVIES",
        "BARAJAS",
        "SHEA",
        "OSBORN",
        "BRIGHT",
        "CUEVAS",
        "BOLTON",
        "MURILLO",
        "LUTZ",
        "DUARTE",
        "KIDD",
        "KEY",
        "COOKE",
        "GOFF",
        "DEJESUS",
        "MARIN",
        "DOTSON",
        "BONNER",
        "COTTON",
        "MERRILL",
        "LINDSAY",
        "LANCASTER",
        "MCGOWAN",
        "FELIX",
        "SALGADO",
        "SLATER",
        "CARVER",
        "GUTHRIE",
        "HOLMAN",
        "FULTON",
        "SNIDER",
        "SEARS",
        "WITT",
        "NEWELL",
        "BYERS",
        "LEHMAN",
        "GORMAN",
        "COSTELLO",
        "DONAHUE",
        "DELANEY",
        "ALBERT",
        "WORKMAN",
        "ROSAS",
        "SPRINGER",
        "JUSTICE",
        "KINNEY",
        "ODELL",
        "LAKE",
        "DONNELLY",
        "LAW",
        "DAILEY",
        "GUEVARA",
        "SHOEMAKER",
        "BARLOW",
        "MARINO",
        "WINTER",
        "CRAFT",
        "KATZ",
        "PICKETT",
        "ESPINOSA",
        "DALY",
        "MALONEY",
        "GOLDSTEIN",
        "CROWLEY",
        "VOGEL",
        "KUHN",
        "PEARCE",
        "HARTLEY",
        "CLEVELAND",
        "PALACIOS",
        "MCFADDEN",
        "BRITT",
        "WOOTEN",
        "CORTES",
        "DILLARD",
        "CHILDERS",
        "ALFORD",
        "DODD",
        "EMERSON",
        "WILDER",
        "LANGE",
        "GOLDBERG",
        "QUINTERO",
        "BEACH",
        "ENRIQUEZ",
        "QUINTANA",
        "HELMS",
        "MACKEY",
        "FINCH",
        "CRAMER",
        "MINOR",
        "FLANAGAN",
        "FRANKS",
        "CORONA",
        "KENDALL",
        "MCCABE",
        "HENDRICKSON",
        "MOSER",
        "MCDERMOTT",
        "CAMP",
        "MCLEOD",
        "BERNAL",
        "KAPLAN",
        "MEDRANO",
        "LUGO",
        "TRACY",
        "BACON",
        "CROWE",
        "RICHTER",
        "WELSH",
        "HOLLEY",
        "RATLIFF",
        "MAYFIELD",
        "TALLEY",
        "HAINES",
        "DALE",
        "GIBBONS",
        "HICKEY",
        "BYRNE",
        "KIRKLAND",
        "FARRIS",
        "CORREA",
        "TILLMAN",
        "SWEET",
        "KESSLER",
        "ENGLAND",
        "HEWITT",
        "BLANCO",
        "CONNOLLY",
        "PATE",
        "ELDER",
        "BRUNO",
        "HOLCOMB",
        "HYDE",
        "MCALLISTER",
        "CASH",
        "CHRISTOPHER",
        "WHITFIELD",
        "MEEKS",
        "HATCHER",
        "FINK",
        "SUTHERLAND",
        "NOEL",
        "RITCHIE",
        "ROSA",
        "LEAL",
        "JOYNER",
        "STARR",
        "MORIN",
        "DELAROSA",
        "CONNOR",
        "HILTON",
        "ALSTON",
        "GILLIAM",
        "WYNN",
        "WILLS",
        "JARAMILLO",
        "ONEIL",
        "NIEVES",
        "BRITTON",
        "RANKIN",
        "BELCHER",
        "GUY",
        "CHAMBERLAIN",
        "TYSON",
        "PUCKETT",
        "DOWNING",
        "SHARPE",
        "BOGGS",
        "TRUONG",
        "PIERSON",
        "GODFREY",
        "MOBLEY",
        "JOHN",
        "KERN",
        "DYE",
        "HOLLIS",
        "BRAVO",
        "MAGANA",
        "RUTHERFORD",
        "NG",
        "TUTTLE",
        "LIM",
        "ROMANO",
        "ARTHUR",
        "TREJO",
        "KNOWLES",
        "LYON",
        "SHIRLEY",
        "QUINONES",
        "CHILDS",
        "DOLAN",
        "HEAD",
        "REYNA",
        "SAENZ",
        "HASTINGS",
        "KENNEY",
        "CANO",
        "FOREMAN",
        "DENTON",
        "VILLALOBOS",
        "PRYOR",
        "SARGENT",
        "DOHERTY",
        "HOPPER",
        "PHAN",
        "WOMACK",
        "LOCKHART",
        "VENTURA",
        "DWYER",
        "MULLER",
        "GALINDO",
        "GRACE",
        "SORENSEN",
        "COURTNEY",
        "PARRA",
        "RODRIGUES",
        "NICHOLAS",
        "AHMED",
        "MCGINNIS",
        "LANGLEY",
        "MADISON",
        "LOCKE",
        "JAMISON",
        "NAVA",
        "GUSTAFSON",
        "SYKES",
        "DEMPSEY",
        "HAMM",
        "RODRIQUEZ",
        "MCGILL",
        "XIONG",
        "ESQUIVEL",
        "SIMMS",
        "KENDRICK",
        "BOYCE",
        "VIGIL",
        "DOWNEY",
        "MCKENNA",
        "SIERRA",
        "WEBBER",
        "KIRKPATRICK",
        "DICKINSON",
        "COUCH",
        "BURKS",
        "SHEEHAN",
        "SLAUGHTER",
        "PIKE",
        "WHITLEY",
        "MAGEE",
        "CHENG",
        "SINCLAIR",
        "CASSIDY",
        "RUTLEDGE",
        "BURRIS",
        "BOWLING",
        "CRABTREE",
        "MCNAMARA",
        "AVALOS",
        "VU",
        "HERRON",
        "BROUSSARD",
        "ABRAHAM",
        "GARLAND",
        "CORBETT",
        "CORBIN",
        "STINSON",
        "CHIN",
        "BURT",
        "HUTCHINS",
        "WOODRUFF",
        "LAU",
        "BRANDON",
        "SINGER",
        "HATCH",
        "ROSSI",
        "SHAFER",
        "OTT",
        "GOSS",
        "GREGG",
        "DEWITT",
        "TANG",
        "POLK",
        "WORLEY",
        "COVINGTON",
        "SALDANA",
        "HELLER",
        "EMERY",
        "SWARTZ",
        "CHO",
        "MCCRAY",
        "ELMORE",
        "ROSENBERG",
        "SIMONS",
        "CLEMONS",
        "BEATTY",
        "HARDEN",
        "HERBERT",
        "BLAND",
        "RUCKER",
        "MANLEY",
        "ZIEGLER",
        "GRADY",
        "LOTT",
        "ROUSE",
        "GLEASON",
        "MCCLELLAN",
        "ABRAMS",
        "VO",
        "ALBRIGHT",
        "MEIER",
        "DUNBAR",
        "ACKERMAN",
        "PADGETT",
        "MAYES",
        "TIPTON",
        "COFFMAN",
        "PERALTA",
        "SHAPIRO",
        "ROE",
        "WESTON",
        "PLUMMER",
        "HELTON",
        "STERN",
        "FRASER",
        "STOVER",
        "FISH",
        "SCHUMACHER",
        "BACA",
        "CURRAN",
        "VINSON",
        "VERA",
        "CLIFTON",
        "ERVIN",
        "ELDRIDGE",
        "LOWRY",
        "CHILDRESS",
        "BECERRA",
        "GORE",
        "SEYMOUR",
        "CHU",
        "FIELD",
        "AKERS",
        "CARRASCO",
        "BINGHAM",
        "STERLING",
        "GREENWOOD",
        "LESLIE",
        "GROVES",
        "MANUEL",
        "SWAIN",
        "EDMONDS",
        "MUNIZ",
        "THOMSON",
        "CROUCH",
        "WALDEN",
        "SMART",
        "TOMLINSON",
        "ALFARO",
        "QUICK",
        "GOLDMAN",
        "MCELROY",
        "YARBROUGH",
        "FUNK",
        "HONG",
        "PORTILLO",
        "LUND",
        "NGO",
        "ELKINS",
        "STROUD",
        "MEREDITH",
        "BATTLE",
        "MCCAULEY",
        "ZAPATA",
        "BLOOM",
        "GEE",
        "GIVENS",
        "CARDONA",
        "SCHAFER",
        "ROBISON",
        "GUNTER",
        "GRIGGS",
        "TOVAR",
        "TEAGUE",
        "SWIFT",
        "BOWDEN",
        "SCHULZ",
        "BLANTON",
        "BUCKNER",
        "WHALEN",
        "PRITCHARD",
        "PIERRE",
        "KANG",
        "BUTTS",
        "METCALF",
        "KURTZ",
        "SANDERSON",
        "TOMPKINS",
        "INMAN",
        "CROWDER",
        "DICKEY",
        "HUTCHISON",
        "CONKLIN",
        "HOSKINS",
        "HOLBROOK",
        "HORNER",
        "NEELY",
        "TATUM",
        "HOLLINGSWORTH",
        "DRAPER",
        "CLEMENT",
        "LORD",
        "REECE",
        "FELDMAN",
        "KAY",
        "HAGEN",
        "CREWS",
        "BOWLES",
        "POST",
        "JEWELL",
        "DALEY",
        "CORDERO",
        "MCKINLEY",
        "VELASCO",
        "MASTERS",
        "DRISCOLL",
        "BURRELL",
        "VALLE",
        "CROW",
        "DEVINE",
        "LARKIN",
        "CHAPPELL",
        "POLLOCK",
        "KIMBALL",
        "LY",
        "SCHMITZ",
        "LU",
        "RUBIN",
        "SELF",
        "BARRIOS",
        "PEREIRA",
        "PHIPPS",
        "MCMANUS",
        "NANCE",
        "STEINER",
        "POE",
        "CROCKETT",
        "JEFFRIES",
        "AMOS",
        "NIX",
        "NEWSOME",
        "DOOLEY",
        "PAYTON",
        "ROSEN",
        "SWENSON",
        "CONNELLY",
        "TOLBERT",
        "SEGURA",
        "ESPOSITO",
        "COKER",
        "BIGGS",
        "HINKLE",
        "THURMAN",
        "DREW",
        "IVEY",
        "BULLARD",
        "BAEZ",
        "NEFF",
        "MAHER",
        "STRATTON",
        "EGAN",
        "DUBOIS",
        "GALLARDO",
        "BLUE",
        "RAINEY",
        "YEAGER",
        "SAUCEDO",
        "FERREIRA",
        "SPRAGUE",
        "LACY",
        "HURTADO",
        "HEARD",
        "CONNELL",
        "STAHL",
        "ALDRIDGE",
        "AMAYA",
        "FORREST",
        "ERWIN",
        "GUNN",
        "SWAN",
        "BUTCHER",
        "ROSADO",
        "GODWIN",
        "HAND",
        "GABRIEL",
        "OTTO",
        "WHALEY",
        "LUDWIG",
        "CLIFFORD",
        "GROVE",
        "BEAVER",
        "SILVER",
        "DANG",
        "HAMMER",
        "DICK",
        "BOSWELL",
        "MEAD",
        "COLVIN",
        "OLEARY",
        "MILLIGAN",
        "GOINS",
        "AMES",
        "DODGE",
        "KAUR",
        "ESCOBEDO",
        "ARREDONDO",
        "GEIGER",
        "WINKLER",
        "DUNHAM",
        "TEMPLE",
        "BABCOCK",
        "BILLINGS",
        "GRIMM",
        "LILLY",
        "WESLEY",
        "MCGHEE",
        "PAINTER",
        "SIEGEL",
        "BOWER",
        "PURCELL",
        "BLOCK",
        "AGUILERA",
        "NORWOOD",
        "SHERIDAN",
        "CARTWRIGHT",
        "COATES",
        "DAVISON",
        "REGAN",
        "RAMEY",
        "KOENIG",
        "KRAFT",
        "BUNCH",
        "ENGEL",
        "TAN",
        "WINN",
        "STEWARD",
        "LINK",
        "VICKERS",
        "BRAGG",
        "PIPER",
        "HUGGINS",
        "MICHEL",
        "HEALY",
        "JACOB",
        "MCDONOUGH",
        "WOLFF",
        "COLBERT",
        "ZEPEDA",
        "HOANG",
        "DUGAN",
        "KILGORE",
        "MEADE",
        "GUILLEN",
        "DO",
        "HINOJOSA",
        "GOODE",
        "ARRINGTON",
        "GARY",
        "SNELL",
        "WILLARD",
        "RENTERIA",
        "CHACON",
        "GALLO",
        "HANKINS",
        "MONTANO",
        "BROWNE",
        "PEACOCK",
        "OHARA",
        "CORNELL",
        "SHERWOOD",
        "CASTELLANOS",
        "THORPE",
        "STILES",
        "SADLER",
        "LATHAM",
        "REDMOND",
        "GREENBERG",
        "COTE",
        "WADDELL",
        "DUKES",
        "DIAMOND",
        "BUI",
        "MADRID",
        "ALONSO",
        "SHEETS",
        "IRVIN",
        "HURT",
        "FERRIS",
        "SEWELL",
        "CARLTON",
        "ARAGON",
        "BLACKMON",
        "HADLEY",
        "HOYT",
        "MCGRAW",
        "PAGAN",
        "LAND",
        "TIDWELL",
        "LOVELL",
        "MINER",
        "DOSS",
        "DAHL",
        "DELATORRE",
        "STANFORD",
        "KAUFFMAN",
        "VELA",
        "GAGNON",
        "WINSTON",
        "GOMES",
        "THACKER",
        "CORONADO",
        "ASH",
        "JARRETT",
        "HAGER",
        "SAMUELS",
        "METZGER",
        "RAINES",
        "SPIVEY",
        "MAURER",
        "HAN",
        "VOSS",
        "HENLEY",
        "CABALLERO",
        "CARUSO",
        "COULTER",
        "NORTH",
        "FINN",
        "CAHILL",
        "LANIER",
        "SOUZA",
        "MCWILLIAMS",
        "DEAL",
        "SCHAFFER",
        "URBAN",
        "HOUSER",
        "CUMMINS",
        "ROMO",
        "CROCKER",
        "BASSETT",
        "KRUSE",
        "BOLDEN",
        "YBARRA",
        "METZ",
        "ROOT",
        "MCMULLEN",
        "CRUMP",
        "HAGAN",
        "GUIDRY",
        "BRANTLEY",
        "KEARNEY",
        "BEAL",
        "TOTH",
        "JORGENSEN",
        "TIMMONS",
        "MILTON",
        "TRIPP",
        "HURD",
        "SAPP",
        "WHITMAN",
        "MESSER",
        "BURGOS",
        "MAJOR",
        "WESTBROOK",
        "CASTLE",
        "SERNA",
        "CARLISLE",
        "VARELA",
        "CULLEN",
        "WILHELM",
        "BERGERON",
        "BURGER",
        "POSEY",
        "BARNHART",
        "HACKETT",
        "MADRIGAL",
        "EUBANKS",
        "SIZEMORE",
        "HILLIARD",
        "HARGROVE",
        "BOUCHER",
        "THOMASON",
        "MELVIN",
        "ROPER",
        "BARNARD",
        "FONSECA",
        "PEDERSEN",
        "QUIROZ",
        "WASHBURN",
        "HOLLIDAY",
        "YEE",
        "RUDOLPH",
        "BERMUDEZ",
        "COYLE",
        "GIL",
        "GOODRICH",
        "PINA",
        "ELIAS",
        "LOCKWOOD",
        "CABRAL",
        "CARRANZA",
        "DUVALL",
        "CORNELIUS",
        "MCCOLLUM",
        "STREET",
        "MCNEAL",
        "CONNORS",
        "ANGEL",
        "PAULSON",
        "HINSON",
        "KEENAN",
        "SHELDON",
        "FARR",
        "EDDY",
        "SAMUEL",
        "LEDBETTER",
        "RING",
        "BETTS",
        "FONTENOT",
        "GIFFORD",
        "HANNAH",
        "HANLEY",
        "PERSON",
        "FOUNTAIN",
        "LEVIN",
        "STUBBS",
        "HIGHTOWER",
        "MURDOCK",
        "KOEHLER",
        "MA",
        "ENGLE",
        "SMILEY",
        "CARMICHAEL",
        "SHEFFIELD",
        "LANGSTON",
        "MCCRACKEN",
        "YOST",
        "TROTTER",
        "STORY",
        "STARKS",
        "LUJAN",
        "BLOUNT",
        "CODY",
        "RUSHING",
        "BENOIT",
        "HERNDON",
        "JACOBSEN",
        "NIETO",
        "WISEMAN",
        "LAYTON",
        "EPPS",
        "SHIPLEY",
        "LEYVA",
        "REEDER",
        "BRAND",
        "ROLAND",
        "FITCH",
        "RICO",
        "NAPIER",
        "CRONIN",
        "MCQUEEN",
        "PAREDES",
        "TRENT",
        "CHRISTIANSEN",
        "PETTIT",
        "SPANGLER",
        "LANGFORD",
        "BENAVIDES",
        "PENN",
        "PAIGE",
        "WEIR",
        "DIETZ",
        "PRATER",
        "BREWSTER",
        "LOUIS",
        "DIEHL",
        "PACK",
        "SPAULDING",
        "AVILES",
        "ERNST",
        "NOWAK",
        "OLVERA",
        "ROCK",
        "MANSFIELD",
        "AQUINO",
        "OGDEN",
        "STACY",
        "RIZZO",
        "SYLVESTER",
        "GILLIS",
        "SANDS",
        "MACHADO",
        "LOVETT",
        "DUONG",
        "HYATT",
        "LANDIS",
        "PLATT",
        "BUSTAMANTE",
        "HEDRICK",
        "PRITCHETT",
        "GASTON",
        "DOBSON",
        "CAUDILL",
        "TACKETT",
        "BATEMAN",
        "LANDERS",
        "CARMONA",
        "GIPSON",
        "URIBE",
        "MCNEILL",
        "LEDFORD",
        "MIMS",
        "ABEL",
        "GOLD",
        "SMALLWOOD",
        "THORNE",
        "MCHUGH",
        "DICKENS",
        "LEUNG",
        "TOBIN",
        "KOWALSKI",
        "MEDEIROS",
        "COPE",
        "KRAUS",
        "QUEZADA",
        "OVERTON",
        "MONTALVO",
        "STALEY",
        "WOODY",
        "HATHAWAY",
        "OSORIO",
        "LAIRD",
        "DOBBS",
        "CAPPS",
        "PUTNAM",
        "LAY",
        "FRANCISCO",
        "ADAIR",
        "BERNSTEIN",
        "HUTTON",
        "BURKETT",
        "RHOADES",
        "RICHEY",
        "YANEZ",
        "BLEDSOE",
        "MCCAIN",
        "BEYER",
        "CATES",
        "ROCHE",
        "SPICER",
        "QUEEN",
        "DOTY",
        "DARLING",
        "DARBY",
        "SUMNER",
        "KINCAID",
        "HAY",
        "GROSSMAN",
        "LACEY",
        "WILKES",
        "HUMPHRIES",
        "PAZ",
        "DARNELL",
        "KEYS",
        "KYLE",
        "LACKEY",
        "VOGT",
        "LOCKLEAR",
        "KISER",
        "PRESLEY",
        "BRYSON",
        "BERGMAN",
        "PEOPLES",
        "FAIR",
        "MCCLENDON",
        "CORLEY",
        "PRADO",
        "CHRISTIE",
        "DELONG",
        "SKAGGS",
        "DILL",
        "SHEARER",
        "JUDD",
        "STAPLETON",
        "FLAHERTY",
        "CASILLAS",
        "PINTO",
        "HAYWOOD",
        "YOUNGBLOOD",
        "TONEY",
        "RICKS",
        "GRANADOS",
        "CRUM",
        "TRIPLETT",
        "SORIANO",
        "WAITE",
        "HOFF",
        "ANAYA",
        "CRENSHAW",
        "JUNG",
        "CANALES",
        "CAGLE",
        "DENNY",
        "MARCUS",
        "BERMAN",
        "MUNSON",
        "OCAMPO",
        "BAUMAN",
        "CORCORAN",
        "KEEN",
        "ZIMMER",
        "FRIEND",
        "ORNELAS",
        "VARNER",
        "PELLETIER",
        "VERNON",
        "BLUM",
        "ALBRECHT",
        "CULVER",
        "SCHUSTER",
        "CUELLAR",
        "MCCORD",
        "SHULTZ",
        "MCRAE",
        "MORELAND",
        "CALVERT",
        "WILLIAM",
        "WHITTINGTON",
        "ECKERT",
        "KEENE",
        "MOHR",
        "HANKS",
        "KIMBLE",
        "CAVANAUGH",
        "CROWELL",
        "RUSS",
        "FELICIANO",
        "CRAIN",
        "BUSCH",
        "MCCORMACK",
        "DRUMMOND",
        "OMALLEY",
        "ALDRICH",
        "LUKE",
        "GRECO",
        "MOTT",
        "OAKES",
        "MALLORY",
        "MCLAIN",
        "BURROWS",
        "OTERO",
        "ALLRED",
        "EASON",
        "FINNEY",
        "WELLER",
        "WALDRON",
        "CHAMPION",
        "JEFFERS",
        "COON",
        "ROSENTHAL",
        "HUDDLESTON",
        "SOLANO",
        "HIRSCH",
        "AKINS",
        "OLIVARES",
        "SONG",
        "SNEED",
        "BENEDICT",
        "BAIN",
        "OKEEFE",
        "HIDALGO",
        "MATOS",
        "STALLINGS",
        "PARIS",
        "GAMEZ",
        "KENNY",
        "QUIGLEY",
        "MARRERO",
        "FAGAN",
        "DUTTON",
        "ATWOOD",
        "PAPPAS",
        "BAGLEY",
        "MCGOVERN",
        "LUNSFORD",
        "MOSELEY",
        "READ",
        "OAKLEY",
        "ASHBY",
        "GRANGER",
        "SHAVER",
        "HOPE",
        "COE",
        "BURROUGHS",
        "HELM",
        "AMBROSE",
        "NEUMANN",
        "MICHAELS",
        "PRESCOTT",
        "LIGHT",
        "DUMAS",
        "FLOOD",
        "STRINGER"
        };

        

        public static string Generate(Gender gender)
        {
            return string.Format("{0} {1}", GenerateFirstName(gender), GenerateLastName());
        }

        public static string GenerateLastName()
        {
            return _lastNames[_random.Next(0, _lastNames.Count)];
        }

        private static string GenerateDudeName()
        {
            return _dudes[_random.Next(0, _dudes.Count)];
        }

        private static string GenerateLadieName()
        {
            return _ladies[_random.Next(0, _ladies.Count)];
        }
        public static string GenerateFirstName(Gender gender)
        {
            if (gender == Gender.Male)
                return GenerateDudeName();
            return GenerateLadieName();
        }
 
        public enum Gender
        {
            Male,
            Female
        }
    }
}
