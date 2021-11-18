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
        List<BountyGrid> BountyQueue = new List<BountyGrid>();
        Dictionary<long, IMyFaction> factions;
        bool bIsServer = false;
        bool binitialized = false;
        int delay = 0;
        public static int BOUNTYTICK = 600;
        public static int MAXBOUNTIESPERCHARACTER = 3;
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
                factions = MyAPIGateway.Session.Factions.Factions;
                BountyGrid[] queue = BountyQueue.ToArray();
                Dictionary<long, int> bountynotifications = new Dictionary<long, int>();
                for (int i = 0; i < queue.Length; i++)
                {

                    //let's add some bounties!
                    Dictionary<long, int> bnotes = AddBounty(queue[i]);
                    foreach (var item in bnotes)
                    {
                        if(!bountynotifications.ContainsKey(item.Key))
                        {
                            bountynotifications.Add(item.Key, item.Value);
                        } else
                        {
                            bountynotifications[item.Key]++;
                        }
                    }
                }
                foreach (var bnotif in bountynotifications)
                {
                    MyVisualScriptLogicProvider.ShowNotification(bnotif.Value + " New Bounties Available!", 5000, "White", bnotif.Key);
                }

                foreach (var bg in Bounties)
                {
                    if (bg.ConditionMet() || bg.GetCharacter()==null)
                    {
                        foreach (var c in bg.contracts)
                        {
                            //should have been completed, if not remove.
                            contractData cd = bg.GetActiveContracts().Find(x => x.contractid == c);
                            if (cd != null)
                            {
                                if (!MyAPIGateway.ContractSystem.RemoveContract(cd.contractid))
                                {
                                    MyAPIGateway.ContractSystem.TryAbandonCustomContract(cd.contractid, cd.playerid);
                                    MyVisualScriptLogicProvider.ShowNotification("Target is Dead, Contract is Void", 5000, "Red", cd.playerid);
                                }
                                else
                                {
                                    MyVisualScriptLogicProvider.ShowNotification("Target is Dead, Contract is Void", 5000, "Red", cd.playerid);
                                }
                            } else
                            {
                                MyAPIGateway.ContractSystem.RemoveContract(c);
                            }
                            

                        }
                        bg.contracts.Clear();
                        bg.GetActiveContracts().Clear();
                        
                    } else
                    {
                        foreach (var c in bg.contracts.ToArray())
                        {
                            //should have been completed, if not remove.
                            contractData cdb = bg.GetActiveContracts().Find(x => x.contractid == c);
                            if (cdb != null)
                            {
                                if (!MyAPIGateway.ContractSystem.IsContractActive(cdb.contractid))
                                {
                                    MyAPIGateway.ContractSystem.RemoveContract(c);
                                    MyVisualScriptLogicProvider.ShowNotification("Contract was void by provider. Removing", 5000, "Red", cdb.playerid);
                                    bg.GetActiveContracts().Remove(cdb);
                                    bg.contracts.Remove(c);
                                }

                            }
                            
                         
                        }


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

            //MyAPIGateway.Session.SessionSettings.EconomyTickInSeconds = 10;
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
            MyAPIGateway.ContractSystem.CustomFinishCondition += CustomConditionFinish;
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
                        if(MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds-bg.lastinsultTime > 5 && !obj.IsDead)
                        {
                            string insult = Insults.insults[_random.Next(1616)];
                            var values = bg.GetActiveContracts();
                            if (values != null)
                            {
                                List<long> pid = new List<long>();
                                foreach (var pair in values)
                                {
                                    if (!pid.Contains(pair.playerid))
                                    {
                                        pid.Add(pair.playerid);
                                    }
                                }
                                
                                foreach (var item in pid)
                                {
                                    MyVisualScriptLogicProvider.SendChatMessageColored(insult, Color.Red, obj.DisplayName, item);
                                }
                            }


                            //MyAPIGateway.Utilities.ShowMessage(obj.DisplayName, insult);
                            bg.lastinsultTime = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
                        }
                        
                        //Do something cool here
                    }
                }
            }
        }

        private void Character_CharacterDied(IMyCharacter obj)
        {
            BountyGrid bg = Bounties.Find(x => x.npc == obj);
            if (bg != null)
            {
                bg.TargetDied();

                string insult = DeathGenerator.deaths[_random.Next(170)];
                var values = bg.GetActiveContracts();
                if (values != null)
                {
                    List<long> pid = new List<long>();
                    foreach (var pair in values)
                    {
                        //Seems like custom conditions finish delegate doesn't work if a player
                        //accepted a contract but didn't finish when server restarts.
                        //so we will update it here as well.

                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        IMyPlayer player = players.Find(x => x.IdentityId == pair.playerid);
                        if (player != null)
                        {

                            if (MeasureDistance(player.GetPosition(), bg.GetCharacter().GetPosition()) <= 3000)
                            {
                                if(MyAPIGateway.ContractSystem.IsContractActive(pair.contractid))
                                {
                                    MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract active");
                                } else 
                                {
                                    MyAPIGateway.Utilities.ShowMessage("Bounty", "Contract not active");
                                }
                                MyAPIGateway.ContractSystem.TryFinishCustomContract(pair.contractid);
                                
                            }
                            else
                            {
                                MyAPIGateway.ContractSystem.TryFailCustomContract(pair.contractid);
                                
                            }
                        }

                        if (!pid.Contains(pair.playerid))
                        {
                            pid.Add(pair.playerid);
                        }



                    }

                    foreach (var item in pid)
                    {
                        
                        MyVisualScriptLogicProvider.SendChatMessageColored(insult, Color.Red, obj.DisplayName, item);
                        MyVisualScriptLogicProvider.ShowNotification(obj.DisplayName + " Died!", 5000, "White", item);
                    }
                }

            }
            delay = 0;
        }



        public bool CustomConditionFinish(long conditionId, long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                

                if(bg.ConditionMet())
                {
                    contractData cd = bg.GetActiveContracts().Find(x => x.contractid == contractId);
                    if (cd != null)
                    {
                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);
                        IMyPlayer player = players.Find(x => x.IdentityId == cd.playerid);
                        if(player!=null)
                        {
                                
                            if (MeasureDistance(player.GetPosition(), bg.GetCharacter().GetPosition()) <= 3000)
                            {
                                MyAPIGateway.ContractSystem.TryFinishCustomContract(contractId);
                                return true;
                            } else
                            {
                                MyAPIGateway.ContractSystem.TryFailCustomContract(contractId);
                                return false;
                            }
                        } 

                    }
                }
                


                //Do something cool here
            }
            delay = 0;
            return false;
                
        }

        private void ContractSystem_CustomConditionFinished(long conditionId, long contractId)
        {
            
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                delay = 0;
                //Do something cool here
            }
        }

        private void ContractSystem_CustomCleanUp(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                //remove npc if dead. otherwise keep in game as a new bounty could be placed on them.
                if(bg.ConditionMet())
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(bg.ownerid);
                    if(faction != null)
                    {
                        MyAPIGateway.Session.Factions.KickMember(faction.FactionId, bg.ownerid);
                    }

                    delay = 0;
                }
                
            }
        }

        private void ContractSystem_CustomTimeRanOut(long contractId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                string targetname = MyVisualScriptLogicProvider.GetPlayersName(bg.ownerid);
                contractData cd = bg.GetActiveContracts().Find(x => x.contractid == contractId);
                if(cd != null)
                {
                    if (bg.GetGPS() != null)
                        MyAPIGateway.Session.GPS.RemoveGps(cd.playerid, bg.GetGPS());

                        MyVisualScriptLogicProvider.ShowNotification("Bounty Failed " + targetname + " got away.", 5000, "Red", cd.playerid);
                }
                delay = 0;
            }
        }

        private void ContractSystem_CustomFailFor(long contractId, long identityId, bool isAbandon)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                if (bg.GetGPS() != null)
                {
                    MyAPIGateway.Session.GPS.RemoveGps(identityId, bg.GetGPS());
                }
                string targetname = MyVisualScriptLogicProvider.GetPlayersName(bg.ownerid);
                MyVisualScriptLogicProvider.ShowNotification("Bounty Failed "+targetname + " has been killed by someone else.", 5000, "Red", identityId);

            }
        }

        public bool UpdateContract(long contractId)
        {
            //set some stuff in the class fields to change this
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {

                
            }
            return true;
        }

        private void ContractSystem_CustomUpdate(long contractId, MyCustomContractStateEnum newState, MyTimeSpan currentTime)
        {

            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                    if (bg.ConditionMet())
                    {
                        contractData cd = bg.GetActiveContracts().Find(x => x.contractid == contractId);
                        if (cd != null)
                        {
                            List<IMyPlayer> players = new List<IMyPlayer>();
                            MyAPIGateway.Players.GetPlayers(players);
                            IMyPlayer player = players.Find(x => x.IdentityId == cd.playerid);
                            if (player != null)
                            {
                                
                                if (MeasureDistance(player.GetPosition(), bg.GetCharacter().GetPosition()) <= 3000)
                                {
                                    MyAPIGateway.ContractSystem.TryFinishCustomContract(contractId);

                                }
                                else
                                {
                                    MyAPIGateway.ContractSystem.TryFailCustomContract(contractId);
                                }
                            }
                            else
                            {
                                MyAPIGateway.ContractSystem.RemoveContract(contractId);
                            }

                        }
                    }
                }
                //Do something cool here
            
        }

        private void ContractSystem_CustomActivateContract(long contractId, long identityId)
        {
            BountyGrid bg = Bounties.Find(x => x.HasBountyId(contractId));
            if (bg != null)
            {
                bg.PlayerAccepted(identityId,contractId);
                string activecontracts = MyAPIGateway.Utilities.SerializeToXML(bg.GetActiveContracts());
                MyVisualScriptLogicProvider.StoreEntityString(bg.targetgrid.Name, "ActiveBounties", activecontracts);
                if(bg.GetGPS()!=null)
                {
                    string name = MyVisualScriptLogicProvider.GetPlayersName(bg.ownerid);
                    Vector3D shippos = bg.targetgrid.GetPosition();
                    Vector3D pos = new Vector3D(shippos.X + _random.Next(0, 500), shippos.Y + _random.Next(0, 600), shippos.Z + _random.Next(0, 500));
                    IMyGps gps = MyAPIGateway.Session.GPS.Create("Last Known Coordinates: " + name, "An anonymous tip came in saying they were spotted in the vicinity.", pos, true);
                    gps.GPSColor = Color.Crimson;
                    bg.SetGPS(gps);
                }
                if (bg.GetGPS() != null)
                {
                    MyAPIGateway.Session.GPS.AddGps(identityId, bg.GetGPS());
                }

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
                delay = 0;
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
                //remove unaccepted contracts on npc
                bg.RemoveUnaccepted();
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
                if(grid.Transparent)
                {
                    return;
                }
                //refresh factions
                factions = MyAPIGateway.Session.Factions.Factions;
                //append any contract blocks we might find

                m_blocks.AddArray(GetContractBlocks(entity).ToArray());

                AddPotentialBountyGrid(grid);
                grid.OnBlockAdded += CheckBlockAdded;
                grid.OnBlockRemoved += Grid_OnBlockRemoved;
                
                //AddBountyContracts(grid);
                delay = 0;
            }
        }

        private void Grid_OnBlockRemoved(IMySlimBlock block)
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
                                m_blocks.Remove(fb);
                            }


                        }
                    }
                }
            }
        }

        private void CheckBlockAdded(IMySlimBlock block)
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
                                if (cp.Pilot != null)
                                    continue;
                                character.SetPosition(cp.GetPosition());
                                character.AimedPoint = cp.GetPosition();
                                character.Use();
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
                                if (cc.Pilot != null)
                                    continue;
                                character.SetPosition(cc.GetPosition());
                                character.AimedPoint = cc.GetPosition();
                                character.Use();
                                
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
                    List<IMyPlayer> players = new List<IMyPlayer>();
                    MyAPIGateway.Players.GetPlayers(players);
                    IMyPlayer player = players.Find(x => x.IdentityId == owner);

                    if (player != null)
                    {
                        string charname = player.DisplayName;
                        if (player.IsBot)
                        {
                            string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(owner);
                            IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);
                            if (gridfaction != null)
                            {
                                bool isMale = true;
                                //check and see if grid owner is faction owner
                                if (gridfaction.FounderId == owner)
                                {

                                    //we don't dont want to add bounty to npc Founder
                                    //make a new NPC
                                    if (_random.Next(0, 1) == 1)
                                    {
                                        charname = NameGenerator.Generate(NameGenerator.Gender.Male);
                                    }
                                    else
                                    {
                                        isMale = false;
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
                                            var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, grid.WorldMatrix.Backward, grid.WorldMatrix.Up);
                                            character = CreateNPCCharacter(owner, charname, pos, isMale);

                                            bool seated = TrySeatCharacter(grid, character);
                                            if (!seated)
                                            {

                                            }
                                            character.CharacterDied += Character_CharacterDied;
                                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Created Character " + charname);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    character = player.Character;
                                    if(character == null)
                                    {
                                        //check if there is a character entity with same name (maybe got disassociated from server reset.)
                                        IMyEntity ent = null;
                                        
                                        MyAPIGateway.Entities.TryGetEntityByName(player.DisplayName, out ent);
                                        if(ent != null)
                                        {
                                            if(ent as IMyCharacter != null)
                                            {
                                                character = (IMyCharacter)ent;
                                                player.SpawnIntoCharacter(character);
                                                character.CharacterDied += Character_CharacterDied;
                                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Found Character " + character.DisplayName);
                                            }
                                        } else
                                        {
                                            var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, grid.WorldMatrix.Backward, grid.WorldMatrix.Up);
                                            character = CreateNPCCharacter(player.IdentityId, player.DisplayName, pos);
                                            bool seated = TrySeatCharacter(grid, character);
                                            if (!seated)
                                            {

                                            }
                                            character.CharacterDied += Character_CharacterDied;
                                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Created Character " + character.DisplayName);
                                            //need to make a character
                                        }
                                    }                                    
                                }
                            }
                        }
                        else
                        {
                            //Is a player
                            character = player.Character;
                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Tracking " + character.DisplayName);

                        }

                        bg = new BountyGrid(grid, owner);
                        if (character != null)
                        {
                            bg.SetCharacter(character);
                        }
                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Tracking: " + grid.DisplayName);
                        Bounties.Add(bg);
                    } else
                    {
                        string charname = MyVisualScriptLogicProvider.GetPlayersName(owner);
                        string faction = MyVisualScriptLogicProvider.GetPlayersFactionName(owner);
                        IMyFaction gridfaction = MyAPIGateway.Session.Factions.TryGetFactionByName(faction);
                        if (gridfaction != null)
                        {
                            bool isMale = true;
                            //check and see if grid owner is faction owner
                            if (gridfaction.FounderId == owner)
                            {

                                //we don't dont want to add bounty to npc Founder
                                //make a new NPC
                                if (_random.Next(0, 1) == 1)
                                {
                                    charname = NameGenerator.Generate(NameGenerator.Gender.Male);
                                }
                                else
                                {
                                    isMale = false;
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
                                        var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, grid.WorldMatrix.Backward, grid.WorldMatrix.Up);
                                        character = CreateNPCCharacter(owner, charname, pos, isMale);

                                        bool seated = TrySeatCharacter(grid, character);
                                        if (!seated)
                                        {

                                        }
                                        character.CharacterDied += Character_CharacterDied;
                                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Created Character " + charname);
                                        break;
                                    }
                                }
                            }
                            else
                            {                               
                                if (character == null)
                                {
                                    //check if there is a character entity with same name (maybe got disassociated from server reset.)
                                    IMyEntity ent = null;
                                    charname = MyVisualScriptLogicProvider.GetPlayersName(owner);
                                    MyAPIGateway.Entities.TryGetEntityByName(charname, out ent);
                                    if (ent != null)
                                    {
                                        if (ent as IMyCharacter != null)
                                        {
                                            character = (IMyCharacter)ent;
                                            character.CharacterDied += Character_CharacterDied;
                                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Found Character " + character.DisplayName);
                                        }
                                    }
                                    else
                                    {
                                        var pos = new MyPositionAndOrientation(grid.PositionComp.WorldAABB.Center + grid.WorldMatrix.Backward * 2.5, grid.WorldMatrix.Backward, grid.WorldMatrix.Up);
                                        character = CreateNPCCharacter(owner, charname, pos);
                                        bool seated = TrySeatCharacter(grid, character);
                                        if (!seated)
                                        {

                                        }
                                        character.CharacterDied += Character_CharacterDied;
                                        //MyAPIGateway.Utilities.ShowMessage("Bounty", "Created Character " + character.DisplayName);
                                        //need to make a character
                                    }
                                }
                            }
                            bg = new BountyGrid(grid, owner);
                            if (character != null)
                            {
                                bg.SetCharacter(character);
                            }
                            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Tracking: " + grid.DisplayName);
                            List<long> b = MyVisualScriptLogicProvider.LoadEntityLongList(bg.targetgrid.Name, "Bounties");
                            string activecontracts = MyVisualScriptLogicProvider.LoadEntityString(bg.targetgrid.Name, "ActiveBounties");
                            
                            var acontracts = MyAPIGateway.Utilities.SerializeFromXML<List<contractData>>(activecontracts);

                            if (acontracts != null)
                            {
                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Reloaded Active Contracts: " + acontracts.Count);
                                bg.SetActiveContracts(acontracts);
                            }
                            if (b!= null)
                            {
                                bg.contracts = b;

                            }
                            BountyQueue.Add(bg);
                            
                        }
                    }
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

        public List<long> GetContractBlocks()
        {
            List<long> blockids = new List<long>();

            foreach (var block in m_blocks)
            {

                        blockids.Add(block.EntityId);

            }

            return blockids;
        }

        public List<long> GetFactionContractBlocks(long factionid)
        {
            List<long> blockids = new List<long>();

            foreach (var block in m_blocks)
            {
                IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(block.OwnerId);
                if (f != null)
                {
                    if (factionid == f.FactionId)
                    {

                        blockids.Add(block.EntityId);

                    }

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

        public IMyCharacter CreateNPCCharacter(long id, string name, MyPositionAndOrientation csys, bool isMale = true)
        {
            string stype = "Default_Astronaut";
            if(!isMale)
            {
                stype = "Default_Astronaut_Female";
            }
            var ob = new MyObjectBuilder_Character()
            {
                Name = name,
                DisplayName = name,
                SubtypeName = stype,
                EntityId = 0,
                AIMode = true,
                JetpackEnabled = false,
                EnableBroadcasting = true,
                NeedsOxygenFromSuit = false,
                OxygenLevel = 1,
                MovementState = MyCharacterMovementEnum.Sitting,
                PersistentFlags = MyPersistentEntityFlags2.InScene | MyPersistentEntityFlags2.Enabled,
                PositionAndOrientation = csys,
                Health = 1000,
                OwningPlayerIdentityId = id,
                ColorMaskHSV = new Vector3(_random.NextDouble(), _random.NextDouble(), _random.NextDouble()),            
            };
            var npc = MyEntities.CreateFromObjectBuilder(ob, true) as IMyCharacter;
            if (npc != null)
            {
                MyEntities.Add((MyEntity)npc, true);
            } else
            {
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Failed to create NPC.");
            }
            return npc;
        }

        public Dictionary<long, int> AddBounty(BountyGrid bgrid)
        {
            Dictionary<long,int> bountynotifications = new Dictionary<long,int>();
            
            //bounty is finished do not try to add
            if (bgrid.ConditionMet())
                return bountynotifications;

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
                        List<long> blockids = GetContractBlocks();
                        if (blockids.Count > 0)
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
                                            int relation = MyAPIGateway.Session.Factions.GetReputationBetweenFactions(pfaction.FactionId, enemy.FactionId);
                                            if (relation>=-500)
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
                                                            //so apparently.. it wont add a contract if you dont have the money for it. So we need to switch the faction on the block momentarily to the faction that is 
                                                            //offering the bounty

                                                            List<IMyPlayer> players = new List<IMyPlayer>();
                                                            MyAPIGateway.Players.GetPlayers(players);
                                                            IMyPlayer player = players.Find(x => x.IdentityId == storeblock.OwnerId);
                                                            long oldbalance = 0;
                                                            if(player!=null)
                                                            {
                                                                player.RequestChangeBalance(reward * 2);
                                                            }
                                                            MyAddContractResultWrapper cw = MyAPIGateway.ContractSystem.AddContract(hunter);

                                                            if (cw.Success)
                                                            {
                                                                
                                                                if(!bountynotifications.ContainsKey(storeblock.OwnerId))
                                                                {
                                                                    bountynotifications.Add(storeblock.OwnerId, 1);
                                                                } else
                                                                {
                                                                    bountynotifications[storeblock.OwnerId]++;
                                                                }
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
                                                                //try to store contracts into ship

                                                            }
                                                            else
                                                            {
                                                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "contract failed " + storeblock.CubeGrid.DisplayName+" for "+ bgrid.GetCharacter().DisplayName);
                                                                if (bgrid.GetCharacter().IsDead)
                                                                {
                                                                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Dead.. " + name);
                                                                }
                                                            }
                                                            if (player != null)
                                                            {
                                                                player.RequestChangeBalance(-(reward * 2));
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
                                                                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Spawned " + name);
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
                                            } 
                                        }
                                    }

                                } else
                                {
                                    
                                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "Store block error");
                                }


                            }
                        }
                    }
                }
            }
            /*
            if(bountyblocks.Count>0)
            {
                Dictionary<long, int> bountynotification = new Dictionary<long, int>();
                foreach (var block in bountyblocks)
                {
                    if(!bountynotification.ContainsKey(block.OwnerId))
                    {
                        bountynotification.Add(block.OwnerId, 1);
                    } else
                    {
                        bountynotification[block.OwnerId] += 1;
                    }
                    //MyAPIGateway.Utilities.ShowMessage("Bounty", "New Bounties Available at "+ block.CubeGrid.DisplayName);
                }
                foreach (var notify in bountynotification)
                {
                    MyVisualScriptLogicProvider.ShowNotification(notify.Value + " New Bounties Available!", 5000, "Green", notify.Key);
                }
                BountyQueue.Remove(bgrid);
                Bounties.Add(bgrid);
                return true;
            }
            */
            BountyQueue.Remove(bgrid);
            Bounties.Add(bgrid);
            return bountynotifications;
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
            //MyAPIGateway.Utilities.ShowMessage("Bounty", "Trying to accept contract : ");
            return MyActivationCustomResults.Success;
        }



 

        public List<IMyFaction> GetEnemyFactions(IMyFaction faction)
        {
            List<IMyFaction> enemyFactions = new List<IMyFaction>();
            int enemycount = 0;
            foreach (var item in factions)
            {
                
                if (MyAPIGateway.Session.Factions.GetReputationBetweenFactions(faction.FactionId, item.Value.FactionId)<0)               
                {
                    enemyFactions.Add(item.Value);
                    enemycount++;
                }
                if (enemycount > MAXBOUNTIESPERCHARACTER)
                    break;
            }

            return enemyFactions;
        }

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

    [System.Serializable]
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
        public Vector3D deathlocation = Vector3D.Zero;
        public double lastinsultTime = 0;

        public Vector3D GetDeathLocation()
        {
            return deathlocation;
        }

        

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
            //MyAPIGateway.ContractSystem.TryFailCustomContract(contract);
            MyAPIGateway.ContractSystem.RemoveContract(contract);
            
        }

        public Dictionary<long,long> GetActiveContractsDictionary()
        {
            Dictionary<long,long> acontracts = new Dictionary<long,long>();
            foreach (var item in playersAccepted)
            {
                acontracts.Add(item.contractid, item.playerid);
            }
            return acontracts;
        }
        public List<contractData> GetActiveContracts()
        {
            return playersAccepted;
        }

        public void SetActiveContracts(List<contractData> cd)
        {
            playersAccepted = cd;
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
            deathlocation = npc.GetPosition();
        }

        public bool IsTargetAlive()
        {
            return !npc.IsDead;
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



    
}