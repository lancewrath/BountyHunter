using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace RazMods.Hunter.Spawns
{
    public class BountySpawner
    {
        List<MyPlanet> planets = new List<MyPlanet>();
        List<MySpawnGroupDefinition> pirates = new List<MySpawnGroupDefinition>();
        List<MySpawnGroupDefinition> cargoships = new List<MySpawnGroupDefinition>();
        List<MySpawnGroupDefinition> encounters = new List<MySpawnGroupDefinition>();
        Random _random = new Random();

        public void CheckPlanets(IMyEntity entity)
        {
            if (entity as MyPlanet != null)
            {
                MyPlanet planet = entity as MyPlanet;
                planets.Add(planet);
            }
        }

        public void SetupSpawns()
        {
            
            var mySpawnGroups = MyDefinitionManager.Static.GetSpawnGroupDefinitions();

            foreach(var spawnGroup in mySpawnGroups)
            {
                MyLog.Default.WriteLineAndConsole("Spawn Group: " + spawnGroup.DisplayNameString + " id: " + spawnGroup.Id);
                if(spawnGroup.Enabled==false) continue;

                if(spawnGroup.IsPirate)
                    pirates.Add(spawnGroup);
                if(spawnGroup.IsCargoShip)
                    cargoships.Add(spawnGroup);
                if(spawnGroup.IsEncounter)
                    encounters.Add(spawnGroup);
            }


        }


        public IMyCharacter CreateNPCCharacter(long id, string name, MyPositionAndOrientation csys, bool isMale = true)
        {
            string stype = "Default_Astronaut";
            if (!isMale)
            {
                stype = "Default_Astronaut_Female";
            }
            var ob = new MyObjectBuilder_Character()
            {
                Name = name,
                DisplayName = name,
                SubtypeName = stype,
                EntityId = 0,
                AIMode = false,
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
                IsPersistenceCharacter = true,

            };
            var npc = MyEntities.CreateFromObjectBuilder(ob, true) as IMyCharacter;
            if (npc != null)
            {
                MyEntities.Add((MyEntity)npc, true);
                //npc.CharacterDied += Character_CharacterDied;
                return npc;
            }
            else
            {
                //MyAPIGateway.Utilities.ShowMessage("Bounty", "Failed to create NPC.");
            }

            return null;

        }

        public List<IMyCubeGrid> SpawnBounty(IMyFaction faction, Action callback, string shipname = "Bounty")
        {
            //Choose some random player
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            List<IMyPlayer> realplayers = players.FindAll(x => !x.IsBot && x.Character != null);
            int playerindex = _random.Next(0, realplayers.Count - 1);
            //get players position
            Vector3D playerPosition = realplayers[playerindex].Character.GetPosition();
            //find closest planet
            MyPlanet closestPlanet = FindClosestPlanet(playerPosition);
            SpawnEnvironment env = GetSpawnEnvironment(closestPlanet, realplayers[playerindex]);
            MySpawnGroupDefinition.SpawnGroupPrefab prefab = RandomShip(env);
            //choose random position -15km to 15km away
            Vector3D shipPos = Vector3D.Zero;
            Vector3D upvec = Vector3D.Zero;
            Vector3D forwardvec = Vector3D.Zero;
            shipPos = new Vector3D(_random.Next((int)playerPosition.X - 15000, (int)playerPosition.X + 15000), _random.Next((int)playerPosition.Y - 15000, (int)playerPosition.Y + 15000), _random.Next((int)playerPosition.Z - 15000, (int)playerPosition.Z + 15000));

            if (env == SpawnEnvironment.MOONGROUND || env == SpawnEnvironment.ATMOPLANETGROUND)
            {

                if (closestPlanet!=null)
                {
                    shipPos = closestPlanet.GetClosestSurfacePointGlobal(shipPos);
                    upvec = Vector3D.Normalize(shipPos);
                    upvec.CalculatePerpendicularVector(out forwardvec);

                }
                

            } else
            {
                //shipPos = GetClearPosition(prefab, playerPosition);
                upvec = Vector3D.Normalize(playerPosition - shipPos);
                upvec.CalculatePerpendicularVector(out forwardvec);
            }
            List<IMyCubeGrid> newgrid = new List<IMyCubeGrid>();
            MyAPIGateway.PrefabManager.SpawnPrefab(newgrid, prefab.SubtypeId, shipPos, forwardvec, upvec,Vector3.Zero,Vector3.Zero, shipname, SpawningOptions.SpawnRandomCargo, faction.FounderId, true, callback);
            return newgrid;

        }

        public Vector3D GetClearPosition(MySpawnGroupDefinition.SpawnGroupPrefab prefab, Vector3D pos)
        {
            
            Vector3D shipPos = new Vector3D(_random.Next((int)pos.X - 15000, (int)pos.X + 15000), _random.Next((int)pos.Y - 15000, (int)pos.Y + 15000), _random.Next((int)pos.Z - 15000, (int)pos.Z + 15000));
            if(MyAPIGateway.Entities.IsInsideWorld(shipPos))
            {
                return GetClearPosition(prefab, pos);
            }
            
            return shipPos;
        }



        public MySpawnGroupDefinition.SpawnGroupPrefab RandomShip(SpawnEnvironment environment)
        {
            int category = _random.Next(0, 99);
            int spawnIndex = 0;
            List<MySpawnGroupDefinition.SpawnGroupPrefab> prefabs;

            if (category < 32)
            {
                //pirate
                spawnIndex = _random.Next(0, pirates.Count - 1);
                prefabs = pirates[spawnIndex].Prefabs;
            } else if(category >= 32 && category < 65)
            {
                //cargo
                spawnIndex = _random.Next(0, cargoships.Count - 1);
                prefabs = cargoships[spawnIndex].Prefabs;
            } else
            {
                //encounter
                spawnIndex = _random.Next(0, encounters.Count - 1);
                prefabs = cargoships[spawnIndex].Prefabs;
            }

            int prefabIndex = _random.Next(0, prefabs.Count - 1);

            return prefabs[prefabIndex];
            


        }



        public SpawnEnvironment GetSpawnEnvironment(MyPlanet closestPlanet, IMyPlayer player)
        {

            Vector3D playerPosition = player.GetPosition();
            if (closestPlanet != null)
            {
                //determine if player is on planet or not
                
                float planetsize = closestPlanet.AverageRadius;
                double dist = BountyHunter.MeasureDistance(playerPosition, closestPlanet.PositionComp.WorldAABB.Center);
                if(dist <= planetsize)
                {
                    //player is underground. Spawn something on surface or in Air
                    if (closestPlanet.HasAtmosphere)
                    {
                        if (_random.Next(0, 100) > 50)
                        {
                            return SpawnEnvironment.ATMOPLANETAIR;
                        }
                        else
                        {
                            return SpawnEnvironment.ATMOPLANETGROUND;
                        }
                    } 
                    else
                    {
                        if (_random.Next(0, 100) > 50)
                        {
                            return SpawnEnvironment.MOONAIR;
                        }
                        else
                        {
                            return SpawnEnvironment.MOONGROUND;
                        }
                    }

                } else
                {
                    //get if player within gravity
                    BoundingBoxD bb = new BoundingBoxD(playerPosition, playerPosition + 1);
                    if(closestPlanet.IntersectsWithGravityFast(ref bb))
                    {
                        //player is underground. Spawn something on surface or in Air
                        if (closestPlanet.HasAtmosphere)
                        {
                            if (_random.Next(0, 100) > 50)
                            {
                                return SpawnEnvironment.ATMOPLANETAIR;
                            }
                            else
                            {
                                return SpawnEnvironment.ATMOPLANETGROUND;
                            }
                        }
                        else
                        {
                            if (_random.Next(0, 100) > 50)
                            {
                                return SpawnEnvironment.MOONAIR;
                            }
                            else
                            {
                                return SpawnEnvironment.MOONGROUND;
                            }
                        }
                    }
                    else
                    {
                        //Player is outside of planet gravity. Spawn something in space
                        return SpawnEnvironment.SPACE;
                    }
                }

            }

            return SpawnEnvironment.SPACE;


        }


        public MyPlanet FindClosestPlanet(Vector3D position)
        {
            MyPlanet planet = null;
            double distance = 0;
            foreach (var p in planets)
            {
                
                double dist = BountyHunter.MeasureDistance(position, p.PositionComp.WorldAABB.Center);
                if (planet == null)
                {
                    planet = p;
                    distance = dist;
                } else
                {   
                    if(dist<distance)
                    {
                        planet = p;
                        distance = dist;
                    }

                }
            }
            return planet;
        }


    }

    public enum SpawnEnvironment
    {
        ATMOPLANETAIR,
        ATMOPLANETGROUND,
        MOONAIR,
        MOONGROUND,
        SPACE
    }
}
