using HarmonyLib;
using KCSG;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;
using static RimWorld.ColonistBar;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Scripting.GarbageCollector;

namespace LTS_StonebornSiteGeneration
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("rimworld.LTS.StonebornFactionExpansion");
            //Harmony.DEBUG = true;
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]//this should probably be done with a prefix.
    public static class PawnRenderer_BodyAngle_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result, Pawn ___pawn)
        {
            if (___pawn.kindDef == LTS_SFE_DefOf.DV_Mimic_HermitCrate && !___pawn.Dead)
            {
                __result = 0f;
            }
        }
    }





    [DefOf]
    public static class LTS_SFE_DefOf
    {
        public static SitePartDef LTS_StonebornRuinSite;
        public static SitePartDef LTS_StonebornVaultSite;
        public static GenStepDef LTS_StonebornVault;
        public static ThingDef DV_DwarvenCrate;
        public static ThingDef DV_DwarvenCrate_Mimic;
        public static PawnKindDef DV_Mimic_HermitCrate;
        public static ThingDef LTS_StonebornVaultEntrance;
        public static ThingDef LTS_StonebornVaultEntranceIntermediate;
        public static ThingDef LTS_StonebornVaultExitOuter;
        public static ThingDef DV_Dwarven_Minecart;
        public static ThingDef DV_Dwarven_Minecart_Steel;
        public static ThingDef DV_Dwarven_Minecart_Jade;
        public static ThingDef DV_Dwarven_Minecart_Gold;

        static LTS_SFE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LTS_SFE_DefOf));
        }
    }

    public class LTS_SFE_ModExtension : DefModExtension
    {
        public string LTS_TexPath;
        public GenStepDef LTS_GenStepDef;
        public int LTS_ticks;
        public FactionDef LTS_faction;
        public int LTS_spawnWeight;
    }





    public class QuestNode_Root_Loot_AncientComplex_Stoneborn : QuestNode_Root_Loot_AncientComplex
    {
        //protected override LayoutDef LayoutDef
        //{
        //    get
        //    {
        //        return LayoutDefOf.AncientComplex_Mechanitor_Loot;
        //    }
        //}

        protected override SitePartDef SitePartDef
        {
            get
            {
                //return SitePartDefOf.AncientComplex_Mechanitor;
                return LTS_SFE_DefOf.LTS_StonebornRuinSite;
            }
        }

        //protected override bool BeforeRunInt()
        //{
        //    return ModLister.CheckBiotech("Ancient mechanitor complex");
        //}

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool flag;
            if (!slate.TryGet<bool>("discovered", out flag, false))
            {
                slate.Set<bool>("discovered", false, false);
            }
            base.RunInt();
        }
    }

    public class QuestNode_Root_Loot_AncientVault_Stoneborn : QuestNode_Root_Loot_AncientComplex
    {
        protected override SitePartDef SitePartDef
        {
            get
            {
                return LTS_SFE_DefOf.LTS_StonebornVaultSite;
            }
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            bool flag;
            if (!slate.TryGet<bool>("discovered", out flag, false))
            {
                slate.Set<bool>("discovered", false, false);
            }
            base.RunInt();
        }
    }

    public class LTS_GenStep_FindStart : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms)//returns the position of the first building that has a 'portal' and has 'Exit' in it's defName
        {
            if (!MapGenerator.PlayerStartSpotValid)
            {
                MapGenerator.PlayerStartSpot = map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Exit")).First().Position;
            }
        }
    }

    public class LTS_GenStep_HermitCrates : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            //Log.Message("Firing LTS_GenStep_HermitCrates");

            List<Building> dwarvenCrates = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => building.def == LTS_SFE_DefOf.DV_DwarvenCrate).Concat(map.listerBuildings.allBuildingsColonist.Where(building => building.def == LTS_SFE_DefOf.DV_DwarvenCrate)));

            

            //Log.Message(dwarvenCrates.ToArray());

            foreach (Building dwarvenCrate in dwarvenCrates)
            {
                //Log.Message("Firing 1");
                if (new System.Random().Next(0, 25) == 0)
                {
                    //Log.Message("Firing 2");
                    IntVec3 position = dwarvenCrate.Position;
                    dwarvenCrate.Destroy();
                    GenSpawn.Spawn(LTS_SFE_DefOf.DV_DwarvenCrate_Mimic, position, map);
                }
            }


        }
    }

    public class LTS_GenStep_MinecartContents : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms) 
        {

            List<ThingDef> cartTypes = new List<ThingDef> { LTS_SFE_DefOf.DV_Dwarven_Minecart, LTS_SFE_DefOf.DV_Dwarven_Minecart_Steel, LTS_SFE_DefOf.DV_Dwarven_Minecart_Jade, LTS_SFE_DefOf.DV_Dwarven_Minecart_Gold };

            if (ModsConfig.IsActive("det.sbdelights"))
                cartTypes.Add(ThingDef.Named("DV_Dwarven_Minecart_Glimmerquartz"));
            if (ModsConfig.IsActive("det.epochspyrinth"))
                cartTypes.Add(ThingDef.Named("DV_Dwarven_Minecart_Pyrinth"));

            List<Building> Minecarts = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => cartTypes.Contains(building.def)).Concat(map.listerBuildings.allBuildingsColonist.Where(building => cartTypes.Contains(building.def))));

            int totalWeight = 0;

            foreach (ThingDef cartType in cartTypes) 
            {
                totalWeight += cartType.GetModExtension<LTS_SFE_ModExtension>()?.LTS_spawnWeight ?? 1;
            }
            
            foreach (Building Minecart in Minecarts)//for each building on the map
            {
                int remainingWeight = new System.Random().Next(0, totalWeight);
                foreach (ThingDef cartType in cartTypes)//foreach type of cart the cart could become
                {
                    if ((cartType.GetModExtension<LTS_SFE_ModExtension>()?.LTS_spawnWeight ?? 1) <= remainingWeight)
                    {
                        remainingWeight -= (cartType.GetModExtension<LTS_SFE_ModExtension>()?.LTS_spawnWeight ?? 1);
                    }
                    else
                    {
                        GenSpawn.Spawn(cartType, Minecart.Position, map, Minecart.Rotation);
                        break;
                    }

                }
            }
        }
    }

    public class LTS_GenStep_ExtraIntermediateLevelChance : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            //should only be 1
            //List<Building> dwarvenVaultEntrances = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Entrance")).Concat(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Entrance"))));
            List<Building> dwarvenVaultEntrances = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => building.def.defName.Contains("Entrance")).Concat(map.listerBuildings.allBuildingsNonColonist.Where(building => building.def.defName.Contains("Entrance"))));


            foreach (Building dwarvenVaultEntrance in dwarvenVaultEntrances)
            {
                if (new System.Random().Next(0, 100) <= SFE_Settings.extraIntermediateFloorChance)
                {
                    IntVec3 position = dwarvenVaultEntrance.Position;
                    GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntranceIntermediate, position, map);
                }
                else
                {
                    IntVec3 position = dwarvenVaultEntrance.Position;
                    GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntrance, position, map);
                }
            }


        }
    }

    public class LTS_GenStep_FirstFloorLadder : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms)
        {
            //should only be 1
            List<Building> dwarvenVaultExits = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Exit")).Concat(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Exit"))));


            foreach (Building dwarvenVaultExit in dwarvenVaultExits)
            {
                IntVec3 position = dwarvenVaultExit.Position;
                GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultExitOuter, position, map);
            }


        }
    }

    [StaticConstructorOnStartup]
    public class LTS_VaultHatch : MapPortal
    {
        private CompHackable Hackable
        {
            get
            {
                CompHackable result;
                if ((result = this.hackableInt) == null)
                {
                    result = (this.hackableInt = base.GetComp<CompHackable>());
                }
                return result;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //Scribe_Values.Look<TileMutatorWorker_Stockpile.StockpileType>(ref this.stockpileType, "stockpileType", TileMutatorWorker_Stockpile.StockpileType.Medicine, false);
            Scribe_Defs.Look<LayoutDef>(ref this.layout, "layout");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.openGraphicData = new GraphicData();
            this.openGraphicData.CopyFrom(this.def.graphicData);
            this.openGraphicData.texPath = def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_TexPath ?? "Things/Building/AncientHatch/AncientHatch_Open";
        }

        public override void Print(SectionLayer layer)
        {
            string text;
            if (this.IsEnterable(out text))
            {
                this.openGraphicData.Graphic.Print(layer, this, 0f);
                return;
            }
            this.Graphic.Print(layer, this, 0f);
        }

        protected override IEnumerable<GenStepWithParams> GetExtraGenSteps()
        {
            if (this.layout != null)
            {
                yield return new GenStepWithParams(def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_GenStepDef ?? LTS_SFE_DefOf.LTS_StonebornVault, new GenStepParams
                //yield return new GenStepWithParams(def.GetModExtension<LTS_SFE_ModExtension>().LTS_GenStepDef, new GenStepParams
                {
                    layout = this.layout
                });
            }
            else
            {
                //Log.Message("mod extension def: " + def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_GenStepDef.defName);
                //Log.Message("used def: "+ (def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_GenStepDef ?? LTS_SFE_DefOf.LTS_StonebornVault).defName);
                
                yield return new GenStepWithParams(def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_GenStepDef ?? LTS_SFE_DefOf.LTS_StonebornVault, default(GenStepParams));
                //yield return new GenStepWithParams(def.GetModExtension<LTS_SFE_ModExtension>().LTS_GenStepDef, default(GenStepParams));
            }
            yield break;
        }

        public override bool IsEnterable(out string reason)
        {
            if (!this.Hackable?.IsHacked ?? false)
            {
                reason = "Locked".Translate();
                return false;
            }
            return base.IsEnterable(out reason);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
            if (this.Hackable?.IsHacked ?? true)
            {
                stringBuilder.AppendLineIfNotEmpty();
                stringBuilder.Append("HatchUnlocked".Translate());
            }
            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            //IEnumerator<Gizmo> enumerator = null;
            yield break;
            //yield break;
        }

        //public TileMutatorWorker_Stockpile.StockpileType stockpileType;
        public LayoutDef layout;
        private CompHackable hackableInt;
        private GraphicData openGraphicData;
    }
    
    public class CompProperties_GasVent : CompProperties
    {
        public CompProperties_GasVent()
        {
            this.compClass = typeof(CompGasVent);
        }

        public GasType gasType;
        public float cellsToFill;
        public EffecterDef effecterReleasing;
    }

    public class CompGasVent : ThingComp
    {
        public CompProperties_GasVent Props
        {
            get
            {
                return (CompProperties_GasVent)this.props;
            }
        }

        private int TotalGas
        {
            get
            {
                //return Mathf.CeilToInt(this.Props.cellsToFill * 255f);
                return Mathf.CeilToInt(this.Props.cellsToFill);
            }
        }

        private float GasReleasedPerTick
        {
            get
            {
                return (float)this.TotalGas / 60f;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Effecter effecter = this.effecter;
            if (effecter != null)
            {
                effecter.Cleanup();
            }
            this.effecter = null;
        }
        public override void CompTick()
        {
            //Log.Message("Tick");
            base.CompTick();
            if (this.parent.MapHeld == null)
            {
                return;
            }
            if (this.Props.effecterReleasing != null)
            {
                if (this.effecter == null)
                {
                    this.effecter = this.Props.effecterReleasing.Spawn(parent.Position, parent.Map, 1f);
                }
                this.effecter.EffectTick(parent, TargetInfo.Invalid);
            }
            if (this.parent.IsHashIntervalTick(ReleaseGasInterval))
            {
                GasUtility.AddGas(this.parent.PositionHeld, this.parent.MapHeld, this.Props.gasType, this.GasReleasedPerTick);
            }
        }
        [Unsaved(false)]
        private Effecter effecter;
        private const int ReleaseGasInterval = 30;
    }

    public class CompProperties_UseEffectSpawnPawn : CompProperties_UseEffect
    {
        public CompProperties_UseEffectSpawnPawn()
        {
            this.compClass = typeof(CompUseEffect_SpawnPawn);
        }
        public PawnKindDef pawnKind;
        //public Type lordJob;
        public string useMessage;
        public bool tryBondingToUser = false;
        public MessageTypeDef messagetype;

    }

    public class CompUseEffect_SpawnPawn : CompUseEffect
    {
        public CompProperties_UseEffectSpawnPawn Props
        {
            get
            {
                return (CompProperties_UseEffectSpawnPawn)this.props;
            }
        }

        public override void DoEffect(Pawn user)
        {
            //user.health.AddHediff(this.Props.hediffDef, null, null, null);
            PawnKindDef pawnKind = this.Props.pawnKind;
            Faction faction = this.parent.Faction;
            PawnGenerationContext context = PawnGenerationContext.NonPlayer;
            float? fixedBiologicalAge = new float?(0f);
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, context, null, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, fixedBiologicalAge, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
            GenSpawn.Spawn(pawn, this.parent.Position, this.parent.Map, WipeMode.VanishOrMoveAside);

            if (this.Props.useMessage != null)
            {
                Messages.Message(string.Format(this.Props.useMessage, this.parent.Label, Props.pawnKind.label, user.Name), pawn, Props.messagetype, false);
            }

            if (this.Props.tryBondingToUser)
            {
                pawn.SetFaction(user.Faction);
                pawn.relations.AddDirectRelation(PawnRelationDefOf.Bond, user);//goes both ways, even if it doesn't show up in
            }//add the intelligent attack skill to the goreflea
        }
    }

    public class CompProperties_MonsterBox : CompProperties
    {
        public CompProperties_MonsterBox()
        {
            this.compClass = typeof(CompMonsterBox);
        }

        //public List<List<SpawnGroup>> monsterGroups;
        public List<Encounter> encounters;
        public FactionDef territorialFactionDef;
        public FactionDef attackingFactionDef;
        public int territoryRadius;
    }

    public class Encounter
    {
        public List<SpawnGroup> spawnGroups;
        public int spawnWeight = 1;
    }
    
    public class SpawnGroup
    {
        public PawnKindDef pawnKind;
        public ThingDef thingDef;
        public IntRange range;
        public FactionDef faction;
        public MentalStateDef mentalstateDef = null;
        public float initialPlantGrowth = -1;

        //public bool factionless = false;
    }

    public class CompMonsterBox : ThingComp
    {
        public CompProperties_MonsterBox Props
        {
            get
            {
                return (CompProperties_MonsterBox)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            //pick random List<Spawngroup> from List<List<Spawngroup>>, then spawn foreach Spawngroup in selected List<Spawngroup>

            Faction lordDefenderFaction = Find.FactionManager.FirstFactionOfDef(Props.territorialFactionDef) ?? this.parent.Map.ParentFaction;
            Faction lordAttackerFaction = Find.FactionManager.FirstFactionOfDef(Props.attackingFactionDef) ?? this.parent.Map.ParentFaction;

            Lord lordDefender = LordMaker.MakeNewLord(lordDefenderFaction, new LordJob_DefendPoint(this.parent.Position, Props.territoryRadius), this.parent.Map, null);
            Lord lordAttacker = LordMaker.MakeNewLord(lordAttackerFaction, new LordJob_DefendPoint(this.parent.Position, Props.territoryRadius), this.parent.Map, null);

            //List<SpawnGroup> selectedEncounter = Props.monsterGroups.RandomElement();
            int totalWeight = 0;

            foreach (Encounter encounter in Props.encounters)
            {
                totalWeight += encounter.spawnWeight;
            }

            int remainingWeight = new System.Random().Next(0, totalWeight);
            Encounter selectedEncounter = Props.encounters.First();

            foreach (Encounter encounter in Props.encounters)
            {
                if (encounter.spawnWeight <= remainingWeight)
                    remainingWeight -= encounter.spawnWeight;
                else
                {
                    selectedEncounter = encounter;
                    break;
                }

            }

            foreach (SpawnGroup spawnGroup in selectedEncounter.spawnGroups)
            {
                int numToSpawn = spawnGroup.range.RandomInRange;

                if (spawnGroup.pawnKind != null)
                {
                    for (int i = 0; i < numToSpawn; i++)
                    {
                        PawnKindDef pawnKind = spawnGroup.pawnKind;
                        Faction faction = FactionUtility.DefaultFactionFrom(spawnGroup.faction ?? null) ?? null;
                        PawnGenerationContext context = PawnGenerationContext.NonPlayer;
                        //float? fixedBiologicalAge = new float?(0f);
                        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, context, null, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                        CellFinder.TryFindRandomCellNear(this.parent.Position, this.parent.Map, Props.territoryRadius, c => c.GetFirstBuilding(this.parent.Map) == null && c.InBounds(this.parent.Map) && c.IsValid, out var validCell);
                        GenSpawn.Spawn(pawn, validCell, this.parent.Map, WipeMode.VanishOrMoveAside);

                        if (faction == lordDefenderFaction)
                            lordDefender.AddPawn(pawn);
                        else if (faction == lordAttackerFaction)
                            lordAttacker.AddPawn(pawn);
                    }
                }
                else
                {
                    for (int i = 0; i < numToSpawn; i++)
                    {
                        Thing thing = ThingMaker.MakeThing(spawnGroup.thingDef);
                        if (spawnGroup.faction != null)
                            thing.SetFaction(FactionUtility.DefaultFactionFrom(spawnGroup.faction));
                        CellFinder.TryFindRandomCellNear(this.parent.Position, this.parent.Map, Props.territoryRadius, c => c.GetFirstBuilding(this.parent.Map) == null && c.InBounds(this.parent.Map) && c.IsValid, out var validCell);
                        GenPlace.TryPlaceThing(thing, validCell, this.parent.Map, ThingPlaceMode.Direct);

                        if ((spawnGroup.initialPlantGrowth != -1) && (thing is Plant plant))
                        {
                            plant.Growth = spawnGroup.initialPlantGrowth;
                        }
                    }
                }
            }

            parent.Destroy();
        }
    }

    public class CompProperties_GrowIntoThing : CompProperties
    {
        public CompProperties_GrowIntoThing()
        {
            this.compClass = typeof(CompGrowIntoThing);
        }

        public ThingDef thing;
        public int ticksToGrow;
        public Vector2 finalDrawSize;
        public int updateGraphicTicksInterval;
    }

    public class CompGrowIntoThing : ThingComp
    {
        public CompProperties_GrowIntoThing Props
        {
            get
            {
                return (CompProperties_GrowIntoThing)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (Find.TickManager.TicksGame - parent.TickSpawned >= Props.ticksToGrow)
            {
                Thing thing = ThingMaker.MakeThing(Props.thing);
                thing.SetFaction(parent.Faction);
                IntVec3 position = parent.Position;
                Map map = parent.Map;
                
                GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Direct);
            }
            if (parent.Map != null && parent.IsHashIntervalTick(Props.updateGraphicTicksInterval))
            {
                parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
            }
        }
    }

    public class Graphic_Single_Growing : Graphic_Single
    {
        public Vector2 getDrawSize(Thing thing)
        {
            //return (float)(Find.TickManager.TicksGame - thing.TickSpawned);
            CompGrowIntoThing comp = (thing as ThingWithComps).GetComp<CompGrowIntoThing>();
            float drawSizeMultiplier = (float)(Find.TickManager.TicksGame - thing.TickSpawned) / comp.Props.ticksToGrow;
            //Log.Message("ticks spawned: " + (Find.TickManager.TicksGame - thing.TickSpawned));
            //Log.Message("ticksToGrow: " + comp.Props.ticksToGrow);
            //Log.Message("drawSizeMultiplier: " + drawSizeMultiplier);

            return thing.DrawSize + (comp.Props.finalDrawSize - thing.DrawSize) * drawSizeMultiplier;
        }
        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            //Log.Error(getDrawSize(thing).y.ToString());
            Vector2 vector;
            bool flag;
            if (this.ShouldDrawRotated)
            {
                vector = getDrawSize(thing);
                flag = false;
            }
            else
            {
                if (!thing.Rotation.IsHorizontal)
                {
                    vector = getDrawSize(thing);
                }
                else
                {
                    vector = getDrawSize(thing).Rotated();
                }
                flag = ((thing.Rotation == Rot4.West && this.WestFlipped) || (thing.Rotation == Rot4.East && this.EastFlipped));
            }
            if (thing.MultipleItemsPerCellDrawn())
            {
                vector *= 0.8f;
            }
            float num = this.AngleFromRot(thing.Rotation) + extraRotation;
            if (flag && this.data != null)
            {
                num += this.data.flipExtraRotation;
            }
            Vector3 center = thing.TrueCenter() + this.DrawOffset(thing.Rotation);
            Material mat = this.MatAt(thing.Rotation, thing);
            Vector2[] uvs;
            Color32 color;
            Graphic.TryGetTextureAtlasReplacementInfo(mat, thing.def.category.ToAtlasGroup(), flag, true, out mat, out uvs, out color);
            Printer_Plane.PrintPlane(layer, center, vector, mat, num, flag, uvs, new Color32[]
            {
                    color,
                    color,
                    color,
                    color
            }, 0.01f, 0f);
            Graphic_Shadow shadowGraphic = this.ShadowGraphic;
            if (shadowGraphic == null)
            {
                return;
            }
            shadowGraphic.Print(layer, thing, 0f);
        }
    }

    public class Building_TrapDamager_SelfRearming : Building_TrapDamager
    {
        public override void Print(SectionLayer layer)
        {
            if (ticksUntilArmed == 0)
            {
                this.Graphic.Print(layer, this, 0f);
                return;
            }
            this.triggeredGraphicData.Graphic.Print(layer, this, 0f);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.triggeredGraphicData = new GraphicData();
            this.triggeredGraphicData.CopyFrom(this.def.graphicData);
            this.triggeredGraphicData.texPath = def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_TexPath ?? "Things/Building/Ruins/Traps/SpikeTrap_triggered";
            this.SetFaction(Find.FactionManager.FirstFactionOfDef(def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_faction) ?? this.Map.ParentFaction); 
        }
        protected override void Tick()
        {
            base.Tick();
            if (ticksUntilArmed > 0)
            {
                ticksUntilArmed--;
            }
            //Log.Message(ticksUntilArmed);
        }
        protected override void SpringSub(Pawn p)
        {
            //Log.Message("SpringSubbed: " + (def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_ticks ?? 300));
            base.SpringSub(p);
            
            ticksUntilArmed = def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_ticks ?? 300;
        }
        //public override bool IsDangerousFor(Pawn p)
        //{
        //    return base.IsDangerousFor(p);
        //}

        protected override float SpringChance(Pawn p)
        {
            if (ticksUntilArmed == 0)
            {
                return base.SpringChance(p);
            }
            return 0;
        }
        private GraphicData triggeredGraphicData;
        public int ticksUntilArmed = 0;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksUntilArmed, "ticksUntilArmed", 0);
        }
    }

    public class Projectile_SpawnsThingLauncherColoured : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing, blockedByShield);
            IntVec3 loc = base.Position;
            if (this.def.projectile.tryAdjacentFreeSpaces && base.Position.GetFirstBuilding(map) != null)
            {
                foreach (IntVec3 intVec in GenAdjFast.AdjacentCells8Way(base.Position))
                {
                    if (intVec.GetFirstBuilding(map) == null && intVec.Standable(map))
                    {
                        loc = intVec;
                        break;
                    }
                }
            }
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(this.def.projectile.spawnsThingDef, null), loc, map, WipeMode.Vanish);
            if (thing.def.CanHaveFaction)
            {
                thing.SetFaction(base.Launcher.Faction, null);
            }
            thing.TryGetComp<CompGlower>().GlowColor = colorInt;
        }
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

            Color color = (launcher as Pawn).apparel.WornApparel.Where(apparel => apparel.def.defName == "LTS_Apparel_FlarePack").First().TryGetComp<CompColorable>().Color;
            float vibrancy = 2.5f;

            this.TryGetComp<CompColorable>().SetColor(color);

            if (color.r == color.g && color.g == color.b)
                colorInt = new ColorInt(Color.white);
            else
            {
                while (color.r > 0 && color.r < 1 && color.g > 0 && color.g < 1 && color.b > 0 && color.b < 1)
                {
                    float average = (color.r+ color.g+ color.b) / 3;
                    color.r = average + ((color.r - average) * vibrancy);
                    color.g = average + ((color.g - average) * vibrancy);
                    color.b = average + ((color.b - average) * vibrancy);
                }

                colorInt = new ColorInt(color);

            }
            
            //this.TryGetComp<CompColorable>().SetColor(colorInt.ToColor);
            //this.TryGetComp<CompColorable>().Notify_ColorChanged();
        }
        private ColorInt colorInt;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<ColorInt>(ref colorInt, "colorInt", default(ColorInt), false);
        }
    }

    public class CompProperties_DeathEffects : CompProperties
    {
        public CompProperties_DeathEffects()
        {
            this.compClass = typeof(CompDeathEffects);
        }

        public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();
        public int pawnSpawnLaunchDistance;
        public ThingDef filthDef = null;
        public IntRange filthLayersRange;
        public float filthRadius;
        public List<ItemSpawningInfo> extraItemList = new List<ItemSpawningInfo>();
        public bool vanish = false;
        public EffecterDef effecterDef;
        
    }

    public class ItemSpawningInfo
    {
        public ThingDef thingDef;
        public IntRange countRange;
        public int dropChancePercent = 100;
        public string dropMessage;
        public string exclusionaryTag;
    }

    public class CompDeathEffects : ThingComp
    {
        public CompProperties_DeathEffects Props
        {
            get
            {
                return (CompProperties_DeathEffects)this.props;
            }
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            //spawn pawns
            foreach (SpawnGroup spawnGroup in Props.spawnGroups)
            {
                PawnKindDef pawnKind = spawnGroup.pawnKind;
                Faction faction = FactionUtility.DefaultFactionFrom(spawnGroup.faction ?? null) ?? parent.Faction;
                Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(this.parent.Position, 5), prevMap, null);

                int numToSpawn = spawnGroup.range.RandomInRange;
                for (int i = 0; i < numToSpawn; i++)
                {
                    
                    PawnGenerationContext context = PawnGenerationContext.NonPlayer;
                    float? fixedBiologicalAge = new float?(0f);
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(pawnKind, faction, context, null, true, false, false, true, false, 1f, false, true, false, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, fixedBiologicalAge, null, null, null, null, null, null, false, false, false, false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, false, false, false, -1, 0, false));
                    GenSpawn.Spawn(pawn, parent.Position, prevMap, WipeMode.VanishOrMoveAside);
                    if (Props.pawnSpawnLaunchDistance > 0)
                    {
                        //CellFinder.TryFindRandomCellNear(this.parent.Position, prevMap, Props.pawnSpawnLaunchDistance, c => c.GetFirstBuilding(this.parent.Map) == null && c.InBounds(this.parent.Map) && c.IsValid, out var validCell);
                        if (RCellFinder.TryFindRandomCellNearWith(parent.Position, (IntVec3 c) => !c.Fogged(prevMap) && c.Standable(prevMap) && c.GetFirstPawn(prevMap) == null && GenSight.LineOfSight(parent.Position, c, prevMap, true, null, 0, 0), prevMap, out var validCell, 5, Props.pawnSpawnLaunchDistance))
                        {
                            pawn.rotationTracker.FaceCell(validCell);
                            PawnFlyer pawnFlyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer_Stun, pawn, validCell, null, null, false, null, null, default(LocalTargetInfo));
                            if (pawnFlyer != null)
                            {
                                GenSpawn.Spawn(pawnFlyer, parent.Position, prevMap, WipeMode.VanishOrMoveAside);
                            }
                        }
                    }
                    lord.AddPawn(pawn);
                }
            }
            //spawn filth
            if (Props.filthDef != null)
            {
                foreach (IntVec3 position in GenRadial.RadialCellsAround(parent.Position, Props.filthRadius, true))
                {
                    //for (int layers = 0; layers < Props.filthLayersRange.RandomInRange; layers++)
                    //{
                    //    FilthMaker.TryMakeFilth(position, prevMap, Props.filthDef);
                    //}
                    FilthMaker.TryMakeFilth(position, prevMap, Props.filthDef, Props.filthLayersRange.RandomInRange);
                }
            }
            //spawn effect
            if (this.Props.effecterDef != null)
            {
                this.Props.effecterDef.Spawn(parent.Position, prevMap).Cleanup();
            }
            //spawn items
            List<string> exclusionaryTags = new List<string>();
            foreach (ItemSpawningInfo itemSpawningInfo in Props.extraItemList)
            {
                if (new System.Random().Next(0, 100) < itemSpawningInfo.dropChancePercent)
                {
                    if (!(itemSpawningInfo.exclusionaryTag != null && exclusionaryTags.Contains(itemSpawningInfo.exclusionaryTag)))
                    {
                        exclusionaryTags.Add(itemSpawningInfo.exclusionaryTag);

                        Thing thing = ThingMaker.MakeThing(itemSpawningInfo.thingDef);
                        thing.stackCount = itemSpawningInfo.countRange.RandomInRange;
                        GenPlace.TryPlaceThing(thing, parent.Position, prevMap, ThingPlaceMode.Near);
                        if (itemSpawningInfo.dropMessage != null)
                        {
                            Messages.Message(string.Format(itemSpawningInfo.dropMessage, parent.Label, thing.Label), thing, MessageTypeDefOf.PositiveEvent, false);
                        }
                    }
                    
                }
            }
            //destroy corpse
            if (Props.vanish && !(parent.ParentHolder as Thing).Destroyed)
            {
                (parent.ParentHolder as Thing).Destroy();
            }
        }
    }

    public class Verb_CastAbilityDash : Verb_CastAbilityJump
    {
        public override ThingDef JumpFlyerDef
        {
            get
            {
                return ThingDef.Named("LTS_SFE_DashPawnFlier");
            }
        }
    }

    public class CompProperties_RoamingEncounterLeader : CompProperties
    {
        public CompProperties_RoamingEncounterLeader()
        {
            this.compClass = typeof(CompRoamingEncounterLeader);
        }

        public int roamIntervalTicks = 3600; //3600 = 1 minute
        public float positionRadius = 6;
        public TraverseMode traverseMode = TraverseMode.NoPassClosedDoors;
    }

    public class CompRoamingEncounterLeader : ThingComp
    {
        public CompProperties_RoamingEncounterLeader Props
        {
            get
            {
                return (CompProperties_RoamingEncounterLeader)this.props;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (parent.IsHashIntervalTick(Props.roamIntervalTicks) && !(parent as Pawn).MentalStateDef.IsAggro)
            {
                CellRect cellRect = parent.Map.BoundsRect(10);
                IntVec3 destination = parent.Position;
                int attempts = 0;
                while (destination == parent.Position)
                {
                    if (attempts == 200)//give up after 200 attempts
                    {
                        return;
                    }
                    attempts++;

                    destination = cellRect.RandomCell;

                    if (destination.Standable(parent.Map) && parent.Map.reachability.CanReach(destination, parent.Position, PathEndMode.OnCell, Props.traverseMode))
                    {
                        break;
                    }
                    else
                        destination = parent.Position;
                }

                List<Pawn> leaderGroup = (parent as Pawn).lord.ownedPawns;

                foreach (Pawn pawn in leaderGroup)//clear all previous lords
                {
                    if (pawn.lord != null)
                    {
                        pawn.lord.Cleanup();
                    }
                }

                Lord lordLeader = LordMaker.MakeNewLord(parent.Faction, new LordJob_DefendPoint(destination, Props.positionRadius), this.parent.Map, null);
                lordLeader.AddPawn(parent as Pawn);
                leaderGroup.Remove(parent as Pawn);
                Lord lordEscort = LordMaker.MakeNewLord(parent.Faction, new LordJob_EscortPawn(parent as Pawn), this.parent.Map, leaderGroup);

            }
        }
    }





    public class StonebornFactionExpansionMod : Mod
    {
        public static SFE_Settings settings;
        public StonebornFactionExpansionMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<SFE_Settings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Content.Name;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappableAttribute]
    public class SFE_Settings : ModSettings
    {
        public static int extraIntermediateFloorChance = 30;
        private static Vector2 scrollPosition = Vector2.zero;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref extraIntermediateFloorChance, "extraIntermediateFloorChance", 30);
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect viewRect = inRect.ContractedBy(10f);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            listing_Standard.Begin(viewRect);
            //sliders:
            extraIntermediateFloorChance = (int)listing_Standard.SliderLabeled("Extra intermediate vault floor per floor chance: " + extraIntermediateFloorChance + "%", extraIntermediateFloorChance, 0, 100);
            //reset button:
            if (listing_Standard.ButtonText("Reset".Translate(), widthPct: 0.3f))
            {
                extraIntermediateFloorChance = 30;
            }

            listing_Standard.End();
            Widgets.EndScrollView();
        }
    }
}
