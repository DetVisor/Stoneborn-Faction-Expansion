using HarmonyLib;
using KCSG;
using RimWorld;
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
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;

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

        static LTS_SFE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LTS_SFE_DefOf));
        }
    }

    public class LTS_SFE_ModExtension : DefModExtension
    {
        public string LTS_TexPathOpen;
        public GenStepDef LTS_GenStepDef;
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
            List<Building> dwarvenVaultEntrances = new List<Building>(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Entrance")).Concat(map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null && building.def.defName.Contains("Entrance"))));


            foreach (Building dwarvenVaultEntrance in dwarvenVaultEntrances)
            {
                //Log.Message("Firing 1");
                if (new System.Random().Next(0, 100) <= 70)
                {
                    //Log.Message("Firing 2");
                    IntVec3 position = dwarvenVaultEntrance.Position;
                    GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntrance, position, map);
                }
                else
                {
                    IntVec3 position = dwarvenVaultEntrance.Position;
                    GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntranceIntermediate, position, map);
                }
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
            this.openGraphicData.texPath = def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_TexPathOpen ?? "Things/Building/AncientHatch/AncientHatch_Open";
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
            if (!this.Hackable.IsHacked)
            {
                reason = "Locked".Translate();
                return false;
            }
            return base.IsEnterable(out reason);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
            if (this.Hackable.IsHacked)
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
                Messages.Message(string.Format(this.Props.useMessage, this.parent.Label, Props.pawnKind.label), pawn, MessageTypeDefOf.NegativeEvent, false);
            }
        }
    }

    public class CompProperties_MonsterBox : CompProperties
    {
        public CompProperties_MonsterBox()
        {
            this.compClass = typeof(CompMonsterBox);
        }

        public List<List<SpawnGroup>> monsterGroups;
        public FactionDef territorialFactionDef;
        public FactionDef attackingFactionDef;
        public int territoryRadius;
    }

    public class SpawnGroup
    {
        public PawnKindDef pawnKind;
        public ThingDef thingDef;
        public IntRange range;
        public FactionDef faction = null;
        public MentalStateDef mentalstateDef = null;
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

            List<SpawnGroup> selectedList = Props.monsterGroups.RandomElement();

            foreach (SpawnGroup spawnGroup in selectedList)
            {
                int numToSpawn = spawnGroup.range.RandomInRange;

                if (spawnGroup.pawnKind != null)
                {
                    for (int i = 0; i < numToSpawn; i++)
                    {
                        PawnKindDef pawnKind = spawnGroup.pawnKind;
                        Faction faction = FactionUtility.DefaultFactionFrom(spawnGroup.faction) ?? null;
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
}
