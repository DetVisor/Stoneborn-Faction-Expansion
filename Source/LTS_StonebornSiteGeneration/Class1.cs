using HarmonyLib;
using KCSG;
using Mono.Cecil;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Resources;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using VEF;
using VEF.AnimalBehaviours;
using VEF.Apparels;
using VEF.Weapons;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;
using static RimWorld.ColonistBar;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Scripting.GarbageCollector;
using static VEF.Graphics.TaggedDefProperties;

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

    [HarmonyPatch(typeof(VEF.Apparels.Apparel_Shield), nameof(VEF.Apparels.Apparel_Shield.DrawShield))]//patch for drawing energy shields on regular shields
    class VEF_Apparels_Apparel_Shield_DrawShield_Patch //patch to make shields draw their energy shields
    {
        [HarmonyPostfix]
        public static void VEF_Apparels_Apparel_Shield_DrawShield_Postfix(VEF.Apparels.CompShield comp, Vector3 drawPos, Rot4 rot4, Apparel_Shield __instance)
        {
            if (!ModsConfig.IsActive("LTS.PS"))//skip if power shields is active, as it's the same Postfix.It should probably be that mod that has to check, with this mod having the custom shield option... Actually, I suppose I could just add that code to spacer shields too, instead.
            {
                CompShieldBubble compShieldBubble = __instance.TryGetComp<CompShieldBubble>();

                if (compShieldBubble != null)//if this shield has an energy shield, draws the CompShieldBubble's ShieldBubble
                {
                    if (compShieldBubble.ShieldState == ShieldState.Active && compShieldBubble.Energy > 0f)//if the energy shield should be drawn
                    {
                        HoldOffset holdOffset = comp.Props.offHandHoldOffset.Pick(rot4);

                        var getAimingVector = AccessTools.Method(typeof(Apparel_Shield), "GetAimingVector");
                        Vector3 aimingVector = (Vector3)getAimingVector.Invoke(__instance, new object[] { drawPos, rot4 });
                        Vector3 loc = aimingVector + holdOffset.offset + new Vector3(0f, holdOffset.behind ? -0.0390625f : 0.0390625f, 0f);

                        __instance.ShieldGraphic.Draw(loc, holdOffset.flip ? rot4.Opposite : rot4, __instance, 0f);

                        //float scale = Mathf.Lerp(compShieldBubble.Props.minShieldSize, compShieldBubble.Props.maxShieldSize, compShieldBubble.Energy);
                        float scale = 1.6f;
                        Vector3 vectorScale = new Vector3(scale, 1f, scale);
                        Matrix4x4 matrix = default(Matrix4x4);
                        //matrix.SetTRS(loc, Quaternion.AngleAxis(rot4.AsAngle, Vector3.up), scale);
                        matrix.SetTRS(loc, Quaternion.AngleAxis(0, Vector3.up), vectorScale);

                        Material bubbleMat = MaterialPool.MatFrom(compShieldBubble.Props.shieldTexPath, ShaderDatabase.Transparent, compShieldBubble.Props.shieldColor);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, bubbleMat, 0);

                    }
                }

                CompShieldField compShieldField = __instance.TryGetComp<CompShieldField>();

                if (compShieldField != null)//similarly to above, but for CompShieldField
                {
                    bool shouldShowShield = __instance.Wearer.Faction != Faction.OfPlayer || __instance.Wearer.Drafted || compShieldField.Energy != compShieldField.MaxEnergy;

                    if (compShieldField.active && (compShieldField.Energy > 0f || compShieldField.Indestructible) && shouldShowShield)
                    {
                        Vector3 vector = __instance.Wearer.DrawPos;
                        vector.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                        float angle;
                        if (__instance.def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_spinning ?? false)
                            angle = (float)Rand.Range(0, 45);
                        else
                            angle = 0;

                        Vector3 s = new Vector3(1 + 2 * compShieldField.ShieldRadius, 1f, 1 + 2 * compShieldField.ShieldRadius);

                        Matrix4x4 matrix = default(Matrix4x4);
                        matrix.SetTRS(vector, Quaternion.AngleAxis(angle, Vector3.up), s);

                        Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(__instance.def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_TexPath ?? "Other/ForceField", ShaderDatabase.Transparent, new Color(1, 1, 1, __instance.def.GetModExtension<LTS_SFE_ModExtension>()?.LTS_opacity ?? 0.3f)), 0, null, 0, new MaterialPropertyBlock());
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ApparelLayerDef), nameof(ApparelLayerDef.IsUtilityLayer), MethodType.Getter)]
    public static class ApparelLayerDef_IsUtilityLayer_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, ApparelLayerDef __instance)
        {
            if (__instance == LTS_SFE_DefOf.LTS_Necklace)
            {
                __result = true;
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
        public static ThingDef DV_Raid_DrillPod;
        public static ThingDef DV_Ethereal_DrillPod;
        public static ApparelLayerDef LTS_Necklace;

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
        public float LTS_opacity;
        public bool LTS_spinning;
        public List<ThingWithWeight> LTS_ThingsWithWeights;
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

    public class LTS_GenStep_FindStartPortalMap : GenStep
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
                IntVec3 position = dwarvenVaultEntrance.Position;
                if (new System.Random().Next(0, 100) <= SFE_Settings.extraIntermediateFloorChance)
                {
                    GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntranceIntermediate, position, map);
                }
                else
                {
                    //GenSpawn.Spawn(LTS_SFE_DefOf.LTS_StonebornVaultEntrance, position, map);

                    List<ThingWithWeight> exitList = this.def.GetModExtension<LTS_SFE_ModExtension>().LTS_ThingsWithWeights;
                    int totalWeight = 0;

                    foreach (ThingWithWeight exit in exitList)
                    {
                        totalWeight += exit.weight;
                    }

                    int remainingWeight = new System.Random().Next(0, totalWeight);
                    ThingWithWeight selectedExit = exitList.First();

                    foreach (ThingWithWeight exit in exitList)
                    {
                        if (exit.weight <= remainingWeight)
                            remainingWeight -= exit.weight;
                        else
                        {
                            selectedExit = exit;
                            break;
                        }

                    }

                    //List<IntVec3> cellsToClear = GenAdjFast.AdjacentCells8Way(position);
                    //cellsToClear.Add(position);//no idea if this is redundant, though it shouldn't really matter.
                    //foreach (IntVec3 cell in cellsToClear)
                    //{
                    //    foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(cell))
                    //    {
                    //        thing.Destroy(DestroyMode.Vanish);
                    //    }
                    //}

                    GenSpawn.Spawn(selectedExit.thingdef, position, map);
                }
            }


        }
    }

    public class ThingWithWeight
    {
        public ThingDef thingdef;
        public int weight = 1;
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
            
            if (parent.IsHashIntervalTick(Props.roamIntervalTicks) && !(parent as Pawn).MentalStateDef.IsAggro) //at interval, if not currently in combat, find a new place to move to
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

    public class BookOutcomeProperties_GiveQuestFromList : BookOutcomeProperties_GiveQuest
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(LTS_StonebornSiteGeneration.BookOutcomeDoer_GiveQuestFromList);
            }
        }
        public List<QuestScriptDef> questScriptDefs;

    }

    [StaticConstructorOnStartup]
    public class BookOutcomeDoer_GiveQuestFromList : BookOutcomeDoer_GiveQuest
    {
        public new BookOutcomeProperties_GiveQuestFromList Props
        {
            get
            {
                return (BookOutcomeProperties_GiveQuestFromList)this.props;
            }
        }

        private bool QuestGiven
        {
            get
            {
                return this.quest != null;
            }
        }

        public override void OnBookGenerated(Pawn author = null)
        {
            if (!ModsConfig.OdysseyActive)
            {
                return;
            }
            this.hasQuest = Rand.Chance(this.Props.questChance);
            Log.Message("hasQuest: "+ hasQuest);
            if (this.hasQuest)
            {
                this.questDef = this.GetQuestDef();
                Log.Message("questDef" + questDef.defName);
            }
            if (this.questDef == null)
            {
                this.hasQuest = false;
            }
        }

        private QuestScriptDef GetQuestDef()
        {
            //IEnumerable<QuestScriptDef> giverQuests = QuestUtility.GetGiverQuests(QuestGiverTag.Reading);
            IEnumerable<QuestScriptDef> giverQuests = Props.questScriptDefs;
            if (giverQuests.EnumerableNullOrEmpty<QuestScriptDef>())
            {
                return null;
            }
            return giverQuests.RandomElementByWeight((QuestScriptDef q) => q.rootSelectionWeight);
        }

        public override void OnReadingTick(Pawn reader, float factor)
        {
            if (!this.hasQuest || this.QuestGiven)
            {
                return;
            }
            if (Rand.MTBEventOccurs(12500f, 1f, 1f) || this.giveNext)
            {
                this.GenerateQuest(reader);
            }
        }

        private void GenerateQuest(Pawn reader)
        {
            Slate slate = new Slate();
            slate.Set<float>("points", StorytellerUtility.DefaultThreatPointsNow(reader.Map), false);
            slate.Set<TaggedString>("discoveryMethod", "QuestDiscoveredFromBook".Translate(base.Book.Named("BOOK"), reader.Named("READER")), false);
            if (this.questDef == null)
            {
                this.questDef = this.GetQuestDef();
            }
            if (this.questDef == null)
            {
                return;
            }
            this.quest = QuestUtility.GenerateQuestAndMakeAvailable(this.questDef, slate);
            Messages.Message("MessageBookGaveQuest".Translate(this.quest.name, base.Book.Named("BOOK"), reader.Named("READER")), MessageTypeDefOf.PositiveEvent, true);
            if (!this.quest.hidden && this.quest.root.sendAvailableLetter)
            {
                QuestUtility.SendLetterQuestAvailable(this.quest, slate.Get<string>("discoveryMethod", null, false));
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.hasQuest, "hasQuest", false, false);
            Scribe_Defs.Look<QuestScriptDef>(ref this.questDef, "questDef");
            Scribe_References.Look<Quest>(ref this.quest, "quest", false);
        }

        private bool hasQuest;

        private QuestScriptDef questDef;

        private Quest quest;

        private bool giveNext;

        private const int ReceiveQuestMTBTicks = 12500;

        private static readonly Texture2D ViewQuestCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest", true);
    }

    public class CompProperties_Chemlight : CompProperties
    {
        public CompProperties_Chemlight()
        {
            this.compClass = typeof(CompChemlight);
        }

        public bool inheritAngle;
        public bool inheritColour;
        public ThingDef filthDef;
    }

    public class CompChemlight : ThingComp
    {
        public CompProperties_Chemlight Props
        {
            get
            {
                return (CompProperties_Chemlight)this.props;
            }
        }
        private int WarnTick
        {
            get
            {
                return this.destroyDelayedComp.DestructionTick - DestroyWarningTicks;
            }
        }
        private float WarnAlpha
        {
            get
            {
                return Mathf.InverseLerp((float)this.WarnTick, (float)this.destroyDelayedComp.DestructionTick, (float)Find.TickManager.TicksGame);
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.glowComp = this.parent.GetComp<CompGlower>();
            this.destroyDelayedComp = this.parent.GetComp<CompDestroyAfterDelay>();
            if (respawningAfterLoad)
            {
                return;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame >= this.WarnTick)
            {
                float warnAlpha = this.WarnAlpha;
                ColorInt glowColor = this.glowComp.GlowColor;
                glowColor.a = (int)((byte)Mathf.Lerp((float)glowColor.a, 0f, warnAlpha));
                this.glowComp.GlowRadius = Mathf.Lerp(this.parent.def.GetCompProperties<CompProperties_Glower>().glowRadius, 0.1f, warnAlpha*0.8f);
                this.glowComp.GlowColor = glowColor;
            }
            //Log.Message("Parent rotation: " + parent.Graphic.DrawRotatedExtraAngleOffset);
            //Log.Message("Parent rotation: " + parent.Graphic);
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            //spawn filthDef thing
            //inherit angle
            //inherit colour

            Thing filth = ThingMaker.MakeThing(Props.filthDef);

            //filth.Graphic.color = parent.TryGetComp<CompGlower>().GlowColor.ToColor;

            //filth.DrawColor = parent.TryGetComp<CompGlower>().GlowColor.ToColor;

            //Log.Message("Rotation: " + filth.Graphic.DrawRotatedExtraAngleOffset);
            //Log.Message("Parent rotation: " + parent.DrawColor);
            

            //filth.Rotation = parent.Rotation;






            //filth.SetColor(parent.TryGetComp<CompGlower>().GlowColor.ToColor);

            //(filth as Filth).SetOverrideDrawPositionAndRotation(parent.DrawPos, parent.Graphic.DrawRotatedExtraAngleOffset);

            GenPlace.TryPlaceThing(filth, parent.Position, previousMap, ThingPlaceMode.Direct);

            //filth.Graphic.color = parent.TryGetComp<CompGlower>().GlowColor.ToColor;
            //filth.DrawColor = parent.TryGetComp<CompGlower>().GlowColor.ToColor;
            //(filth as Filth).SetOverrideDrawPositionAndRotation(parent.DrawPos, parent.Graphic.DrawRotatedExtraAngleOffset);

            //(filth as Filth).DirtyMapMesh(previousMap);

            //Log.Message("New rotation: " + filth.Graphic.DrawRotatedExtraAngleOffset);

            //FilthMaker.TryMakeFilth(parent.Position, previousMap, Props.filthDef, 1, FilthSourceFlags.None, false);

            //thing.
        }

        private const int DestroyWarningTicks = 600;
        private CompDestroyAfterDelay destroyDelayedComp;
        private CompGlower glowComp;
    }

    public class LTS_CompProperties_ThornApparel : CompProperties
    {
        public LTS_CompProperties_ThornApparel()
        {
            this.compClass = typeof(LTS_CompThornApparel);
        }

        public float damageFloat;
        public DamageDef damageDef;
        public SoundDef soundDef = null;
        public bool affectsRangedAttacks = false;
        public float damagePenetration;
    }

    public class LTS_CompThornApparel : ThingComp
    {
        public LTS_CompProperties_ThornApparel Props
        {
            get
            {
                return (LTS_CompProperties_ThornApparel)this.props;
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            Pawn pawn = (parent as Apparel).Wearer;
            if (pawn.Dead)
            {
                return;
            }
            if (dinfo.Instigator != null && (Props.affectsRangedAttacks || pawn.Position.InHorDistOf(dinfo.Instigator.Position, 1.5f)))//attacker next to user
            {
                if(Props.soundDef != null) Props.soundDef.PlayOneShot(pawn);
                dinfo.Instigator.TakeDamage(new DamageInfo(Props.damageDef, Props.damageFloat, Props.damagePenetration));
            }
        }
    }

    // ---------------------------------  !!!BE WARNED!!!  ---------------------------------
    
    // Beyond this point lies the sanity-crumbling tangle of code for the cave shuttle quest

    // This place is not a place of competency. 
    // No highly refined code is written here. 
    // Nothing valued is here.

    public enum MiningQuotaQuestState
    {
        AwaitingAcceptance,
        
        ColonyShuttleArriving,
        ColonyShuttleReady,
        ColonyShuttleLeaving,

        CaveShuttleArriving,
        CaveShuttleReady,
        CaveShuttleLeaving,

        ReturnShuttleArriving,
        ReturnShuttleUnloading,
        ReturnShuttleLeaving,

        Complete,
        Failed
    }

    public class QuestNode_Mission_MiningQuota : QuestNode
    {
        public List<ThingDef> resourceDefs;
        public SimpleCurve resourceRequirementsPerQuestPointsCurve; //quest points to required amount should probably be on a plateuing quadratic curve
        //public ThingDef incomingShuttleDef;
        public ThingDef shuttleDef;
        //public ThingDef outgoingShuttleDef;

        public int pocketMapSize;
        public MapGeneratorDef pocketMapGenerator;
        public IEnumerable<GenStepWithParams> extraGenStepDefs;



        public Thing shuttle;//might be best to keep

        protected override bool TestRunInt(Slate slate)
        {
            if (resourceDefs == null || resourceDefs.Count == 0)
            {
                Log.Error("QuestNode_Mission_MiningQuota: resourceDefs not set");
                return false;
            }
            else if (shuttleDef == null)
            {
                Log.Error("QuestNode_Mission_MiningQuota: shuttle ThingDef not set");
                return false;
            }
            else if (pocketMapGenerator == null)
            {
                Log.Error("QuestNode_Mission_MiningQuota: pocketMapGenerator not set");
                return false;
            }
            else if (Find.FactionManager.FirstFactionOfDef(FactionDef.Named("OutlanderRoughStoneborn")) == null)
            {
                Log.Error("QuestNode_Mission_MiningQuota: OutlanderRoughStoneborn faction not found");
                return false;
            }
            return true;
        }
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            float points = QuestGen.slate.Get("points", 0f);
            ThingDef resourceDef = resourceDefs.RandomElement();
            int resourceAmount = (int)(resourceRequirementsPerQuestPointsCurve.Evaluate(points) / resourceDef.BaseMarketValue);
            Map map = QuestGen_Get.GetMap(false, null, true);
            Slate slate = QuestGen.slate;
            string resourceTotalValue = (resourceAmount * resourceDef.BaseMarketValue).ToStringMoney();

            slate.Set("resourceDef", resourceDef);
            slate.Set("resourceAmount", resourceAmount);
            slate.Set("resourceTotalValue", resourceTotalValue);



            slate.Set<Map>("map", map, false);
            float x = slate.Get<float>("points", 0f, false);
            Pawn asker = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("OutlanderRoughStoneborn")).leader;

            QuestPart_MiningQuota questPart = new QuestPart_MiningQuota//QuestParts are for ongoing stuff. everything else in the questnode is instantly read.
            {
                resourceDef = resourceDef,//This, surprisingly, works.
                resourceAmount = resourceAmount,
                colonyMap = map,
                //incomingShuttleDef = incomingShuttleDef,
                shuttleDef = shuttleDef,
                //outgoingShuttleDef = outgoingShuttleDef,

                pocketMapSize = pocketMapSize,
                pocketMapGenerator = pocketMapGenerator,
                extraGenStepDefs = extraGenStepDefs,
                //state = MiningQuotaQuestState.AwaitingAcceptance,


                shuttle = shuttle,

                inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID("Initiate"),//this sets the signal that will enable the questpart and it's ticking.

                shuttleInventory = new ThingOwner<Thing>(),

                //drillShuttleEmergeTicks = ((shuttleDef.comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).incomingShuttleDef) as GroundSpawner).,
                //drillShuttleSubmergeTicks = (shuttleDef.comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).LTS_DrillShuttleOutgoing,
                drillShuttleEmergeTicks = 1650,
                drillShuttleSubmergeTicks = 1650,
                drillShuttleTravelTicks = 6000,
            };
            quest.AddPart(questPart);

            
            


            //quest.SpawnThing(map, shuttle = ThingMaker.MakeThing(shuttleDef), null, null, QuestGenUtility.HardcodedSignalWithQuestID("Initiate"), true, true, null, null);//spawn shuttle after quest accepted.
            //quest.SpawnThing(map, shuttle = ThingMaker.MakeThing(incomingShuttleDef), asker.Faction, null, QuestGenUtility.HardcodedSignalWithQuestID("Initiate"), true, true, null, null);//spawn emerging shuttle after quest accepted.
            quest.SpawnThing(map, ThingMaker.MakeThing((shuttleDef.comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).incomingShuttleDef), asker.Faction, null, QuestGenUtility.HardcodedSignalWithQuestID("Initiate"), true, true, null, null);//spawn emerging shuttle after quest accepted.
            quest.SpawnThing(map, shuttle = ThingMaker.MakeThing(shuttleDef), asker.Faction, shuttle.Position, QuestGenUtility.HardcodedSignalWithQuestID("ShuttleArrived"), true, true, null, null, true);//spawn shuttle after shuttle emerged.
            //quest.QuestSelectTargets.AddItem(shuttle);
            
            //quest.SpawnThing(map, ThingMaker.MakeThing(outgoingShuttleDef), asker.Faction, shuttle.Position, QuestGenUtility.HardcodedSignalWithQuestID("ShuttleLaunched"), true, true, null, null);

            
            
            

            //quest end:

            quest.Delay(120, delegate
            {
                QuestScriptDefOf.Util_GetDefaultRewardValueFromPoints.Run();//this is not necessary, but seemingly sets the rewardValue to an appropriate number.
                float rewardValue = slate.Get<float>("rewardValue", 0f, false);
                RewardsGeneratorParams parms = new RewardsGeneratorParams
                {
                    rewardValue = rewardValue,
                    allowGoodwill = true,
                };

                quest.GiveRewards(parms, "SuccessSignal", null, null, null, null, null, null, null, false, asker, false, false, null);
            }, null, null, null, false, null, null, false, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, true);
            quest.End(QuestEndOutcome.Success, 0, null, "SuccessSignal", QuestPart.SignalListenMode.OngoingOnly, true, false);
            quest.End(QuestEndOutcome.Fail, 0, null, "FailSignal", QuestPart.SignalListenMode.OngoingOnly, true, false);

            slate.Set<Pawn>("asker", asker, false);
            slate.Set<bool>("askerIsNull", asker == null, false);
        }
    }

    public class QuestPart_MiningQuota : QuestPartActivable
    {
        public ThingDef resourceDef;
        public int resourceAmount;
        public Map colonyMap;
        public Map pocketMap;
        //public ThingDef incomingShuttleDef;
        public ThingDef shuttleDef;
        //public ThingDef outgoingShuttleDef;
        public Thing incomingShuttle;
        public Thing shuttle;
        public Thing outgoingShuttle;
        public int pocketMapSize;
        public MapGeneratorDef pocketMapGenerator;
        public IEnumerable<GenStepWithParams> extraGenStepDefs;
        public MiningQuotaQuestState state;
        public ThingOwner<Thing> shuttleInventory;

        
        private int ticksAtLaunch = -1;
        public int drillShuttleEmergeTicks;
        public int drillShuttleSubmergeTicks;
        public int drillShuttleTravelTicks;

        private IntVec3 shuttlePosition;

        public bool returning = false;

        public Faction OutlanderRoughStoneborn = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("OutlanderRoughStoneborn"));

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref resourceDef, "resourceDef");
            Scribe_Values.Look(ref resourceAmount, "resourceAmount", 500);
            Scribe_References.Look(ref colonyMap,"colonyMap");
            Scribe_References.Look(ref pocketMap, "pocketMap");
            Scribe_References.Look(ref incomingShuttle, "incomingShuttle");
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_References.Look(ref outgoingShuttle, "outgoingShuttle");

            Scribe_Values.Look(ref drillShuttleEmergeTicks, "drillShuttleEmergeTicks", 120);
            Scribe_Values.Look(ref drillShuttleSubmergeTicks, "drillShuttleSubmergeTicks", 120);
            Scribe_Values.Look(ref drillShuttleTravelTicks, "drillShuttleTravelTicks", 120);

            Scribe_Values.Look(ref state, "state");
            Scribe_Deep.Look(ref shuttleInventory, "shuttleInventory");
            //Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);

            //Scribe_Values.Look(ref state, "state");
            Scribe_Values.Look(ref returning, "returning", false);
        }

        public void LaunchDrillShuttle(CompDrillShuttle compDrillShuttle)
        {
            ticksAtLaunch = Find.TickManager.TicksGame;
            if (true)//check if we're leaving a pocketmap
            {
                //check if we succeeded or failed, then send the respective signal to the quest.
            }
        }

        public override void QuestPartTick()
        {
            base.QuestPartTick();
            //Log.Warning("QuestPart Ticking");


            if (ticksAtLaunch > -1)
            {
                
                if (!returning)
                {
                    if (ticksAtLaunch + drillShuttleSubmergeTicks + drillShuttleTravelTicks == Find.TickManager.TicksGame)//after launch, submergence animation and travel
                    {
                        //ThingDef mineableDef = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(def => def.defName.Contains("Mineable" + resourceDef.defName));//upgrade this to deal with resources with mod prefixes
                        ThingDef mineableDef = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(def => def.defName.Contains("Mineable") && def.defName.Contains(char.ToUpper(resourceDef.label[0]) + resourceDef.label.Substring(1)));
                        //, StringComparison.OrdinalIgnoreCase
                        GenStep_ScatterLumpsMineable genStep_ExtraOresForQuest = new GenStep_ScatterLumpsMineable
                        {
                            count = Rand.RangeInclusive(5, 8),
                            nearMapCenter = false,
                            forcedDefToScatter = mineableDef,
                            forcedLumpSize = (int)(resourceAmount / mineableDef.building.mineableYield)
                        };

                        pocketMap = PocketMapUtility.GeneratePocketMap(new IntVec3(pocketMapSize, 1, pocketMapSize), pocketMapGenerator, extraGenStepDefs, colonyMap);//generating pocket map similarly to a portal

                        genStep_ExtraOresForQuest.Generate(pocketMap, new GenStepParams());
                        new GenStep_Fog().Generate(pocketMap, new GenStepParams());

                        CellFinder.TryFindRandomCell(pocketMap, c => CanFitBuilding(c, pocketMap, ThingDef.Named("LTS_DrillShuttle")) && c.IsValid, out var shuttleCell);
                        shuttlePosition = shuttleCell;
                        GenSpawn.Spawn((ThingDef.Named("LTS_DrillShuttle").comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).incomingShuttleDef, shuttlePosition, pocketMap);
                        CameraJumper.TryJump(new GlobalTargetInfo(shuttleCell, pocketMap));//jump to incoming shuttle on pocketmap
                    }
                    if (ticksAtLaunch + drillShuttleSubmergeTicks + drillShuttleTravelTicks + drillShuttleEmergeTicks == Find.TickManager.TicksGame)//after launch, submergence animation, travel and re-emergence animation
                    {
                        shuttle = GenSpawn.Spawn(ThingDef.Named("LTS_DrillShuttle"), shuttlePosition, pocketMap);
                        
                        //Log.Warning("QuestLookTargets.EnumerableCount: "+ QuestLookTargets.EnumerableCount());
                        //QuestLookTargets.Add(shuttle);
                        //this.loo
                        //Log.Warning("QuestLookTargets.EnumerableCount: " + QuestLookTargets.EnumerableCount());
                        //QuestLookTargets.AddItem(shuttle);
                        //shuttle.TryGetComp<CompTransporter>().GetDirectlyHeldThings().TryAddRangeOrTransfer(shuttleInventory, true, true);
                        //shuttle.TryGetComp<CompTransporter>().innerContainer.TryDropAll(shuttle.Position, shuttle.Map, ThingPlaceMode.Near);
                        shuttleInventory.TryDropAll(shuttle.Position, shuttle.Map, ThingPlaceMode.Near);//dump stuff straight from the quest inventory at the feet of the shuttle.

                        ticksAtLaunch = -1;
                    }
                }
                else
                {
                    if (ticksAtLaunch + drillShuttleSubmergeTicks == Find.TickManager.TicksGame)//after launch and submergence animation
                    {
                        //pocketMap.Dispose();
                        PocketMapUtility.DestroyPocketMap(pocketMap);
                    }
                    if (ticksAtLaunch + drillShuttleSubmergeTicks + drillShuttleTravelTicks == Find.TickManager.TicksGame)//after launch, submergence animation and travel
                    {
                        GenSpawn.Spawn((shuttleDef.comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).incomingShuttleDef, shuttlePosition = DropCellFinder.GetBestShuttleLandingSpot(colonyMap, OutlanderRoughStoneborn), colonyMap);
                        //CameraJumper.TryJump(new GlobalTargetInfo(shuttleCell, pocketMap));
                        
                    }
                    if (ticksAtLaunch + drillShuttleSubmergeTicks + drillShuttleTravelTicks + drillShuttleEmergeTicks == Find.TickManager.TicksGame)//after launch, submergence animation, travel and re-emergence
                    {
                        GenSpawn.Spawn((shuttleDef.comps.Where(comp => comp is CompProperties_DrillShuttle).First() as CompProperties_DrillShuttle).outgoingShuttleDef, shuttlePosition, colonyMap);

                        bool successful = false;

                        if (shuttleInventory.TotalStackCountOfDef(resourceDef) >= resourceAmount)
                        {
                            int resourceAmountToRemove = resourceAmount;
                            foreach (Thing thing in shuttleInventory.Where(t => t.def == resourceDef).ToList())
                            {
                                int amountFromStack = Mathf.Min(thing.stackCount, resourceAmountToRemove);
                                thing.SplitOff(amountFromStack).Destroy();
                                resourceAmountToRemove -= amountFromStack;
                                if (resourceAmountToRemove <= 0)
                                    break;
                            }
                            successful = true;
                            //quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".SuccessSignal"));
                        }

                        shuttleInventory.TryDropAll(shuttlePosition, colonyMap, ThingPlaceMode.Near);

                        if (successful)
                        {
                            quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".SuccessSignal"));
                        }
                        else
                        {
                            quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".FailSignal"));
                        }
                        
                    }
                    //if (ticksAtLaunch + 2 * drillShuttleSubmergeTicks + drillShuttleTravelTicks + drillShuttleEmergeTicks == Find.TickManager.TicksGame)//after launch, submergence animation, travel, re-emergence and departure
                    //{
                    //    //clean up and terminate all quest processes
                    //}
                }



            }
        }
        public static bool CanFitBuilding(IntVec3 cell, Map map, ThingDef buildingDef)//checks if a cell can have a building spawned on it.
        {
            CellRect footprint = GenAdj.OccupiedRect(cell, Rot4.North, buildingDef.size);
            foreach (IntVec3 checkCell in footprint)
            {
                if (!checkCell.InBounds(map) || !checkCell.Standable(map) || checkCell.GetFirstBuilding(map) != null || checkCell.GetFirstItem(map) != null)
                    return false;
            }
            return true;
        }
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo target in base.QuestLookTargets)
                    yield return target;

                if (shuttle != null && !base.QuestLookTargets.Contains(shuttle))
                    yield return shuttle;
            }
        }
        //public override void QuestPartTick()
        //{
        //    base.QuestPartTick();
        //    if (state == MiningQuotaQuestState.Complete)
        //        return;

        //}

        //public override void Notify_QuestSignalReceived(Signal signal)
        //{
        //    base.Notify_QuestSignalReceived(signal);
        //    //Log.Warning(signal.tag);
        //    //Log.Warning("Quest" + quest.id + ".Initiate");
        //    if (signal.tag == "Quest" + quest.id + ".Initiate")//I should probably make a function to prefix the '"Quest" + quest.id + "." + '
        //    {
        //        QuestAccepted();
        //    }

        //    //switch (signal.tag)
        //    //{
        //    //    case QuestAcceptedSignal:
        //    //        QuestAccepted();
        //    //        break;
        //    //    case "inSignal":
        //    //        QuestAccepted();
        //    //        break;

        //    //}
        //}



        //public void QuestAccepted()
        //{
        //    if (colonyMap == null)
        //        colonyMap = Find.CurrentMap;
        //    state = MiningQuotaQuestState.ColonyShuttleArriving;
        //    SpawnIncomingShuttle(colonyMap, out shuttle);
        //}





        //private void SpawnIncomingShuttle(Map map, out Thing shuttle)
        //{
        //    IntVec3 cell = DropCellFinder.GetBestShuttleLandingSpot(map, OutlanderRoughStoneborn);//should probably finda way to get this from the quest giver
        //    shuttle = ThingMaker.MakeThing(incomingShuttleDef);
        //    //GenSpawn.Spawn(shuttle, cell, map, WipeMode.Vanish);
        //    Log.Warning("1");
        //    quest.SpawnSkyfaller(map, incomingShuttleDef, Gen.YieldSingle<Thing>(shuttle), OutlanderRoughStoneborn, cell, null, false, false, null, null);
        //    Log.Warning("2");
        //}
    }

    public class CompProperties_DrillShuttle : CompProperties
    {
        public ThingDef incomingShuttleDef;
        public ThingDef outgoingShuttleDef;
        public CompProperties_DrillShuttle()
        {
            compClass = typeof(CompDrillShuttle);
        }
    }

    public class CompDrillShuttle : ThingComp
    {
        //public Quest quest;
        public CompProperties_DrillShuttle Props
        {
            get
            {
                return (CompProperties_DrillShuttle)props;
            }
        }
        private CompTransporter cachedCompTransporter;
        public CompTransporter compTransporter
        {
            get
            {
                CompTransporter compTransporter;
                if ((compTransporter = this.cachedCompTransporter) == null)
                {
                    compTransporter = (this.cachedCompTransporter = this.parent.GetComp<CompTransporter>());
                }
                return compTransporter;
            }
        }
        public void GetChildHolders(List<IThingHolder> outChildren) //check for stuff held in a pawns' inventories
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, compTransporter.innerContainer);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;
            //IEnumerator<Gizmo> enumerator = null;

            //foreach (Gizmo gizmo3 in QuestUtility.GetQuestRelatedGizmos(this.parent))
            //{
            //    yield return gizmo3;
            //}
            //enumerator = null;
            //yield break;


            yield return new Command_Action
            {
                defaultLabel = "Launch CaveShuttle",
                defaultDesc = "Launch the expedition caveshuttle and everything aboard it.",
                icon = ContentFinder<Texture2D>.Get("UI/LaunchDrillShuttle"),
                action = delegate ()
                {
                    Quest quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.QuestLookTargets.Contains(parent));
                    //if (parent.Map.IsPocketMap)
                    //    quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".ShuttleLaunched"));
                    //else
                    //    quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".ReturnShuttleLaunched"));
                    QuestPart_MiningQuota questPart = quest.PartsListForReading.FirstOrDefault(p => p.GetType() == typeof(QuestPart_MiningQuota)) as QuestPart_MiningQuota;
                    questPart.LaunchDrillShuttle(this);
                    if (parent.Map.IsPocketMap)
                    {
                        questPart.returning = true;
                    }
                        
                    GenSpawn.Spawn(Props.outgoingShuttleDef, parent.Position, parent.Map);

                    //questPart.shuttle = parent;//Just in case...
                    //parent.GetComp<CompTransporter>().loa
                    compTransporter.TryRemoveLord(parent.Map);//end shuttle loading task
                                                              //ThingOwner shuttleInventory = compTransporter.GetDirectlyHeldThings();
                                                              //activeTransporter.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, true, true);
                                                              //ThingOwner a.inner // .TryAddRangeOrTransfer(shuttleInventory, true, true);

                    questPart.shuttleInventory.TryAddRangeOrTransfer(compTransporter.GetDirectlyHeldThings(), true, true);//moves everything in the shuttle to the QuestPart_MiningQuota
                                                                                                                          //questPart.shuttleInventory.TryAddRangeOrTransfer(null, true, true);

                    compTransporter.CleanUpLoadingVars(parent.Map);
                    parent.Destroy(DestroyMode.Vanish);

                    //parent.DeSpawn(DestroyMode.Vanish);
                }

            };
            //else//if the shuttle's preparing to return
            //{
            //    yield return new Command_Action
            //    {
            //        defaultLabel = "Return to colony",
            //        defaultDesc = "Return the expedition shuttle " + "and everything aboard it to the colony.",
            //        icon = TexCommand.GatherSpotActive,
            //        //action = ReturnToColony
            //    };
            //}


        }
        //public virtual AcceptanceReport CanLaunch()
        //{
        //    //if (parent.GetComp<CompTransporter>().innerContainer.)
        //    if (false)
        //    {
        //        return "can't launch becuse...";//actually, launching without any pawns should probably just work but trigger a quest failiure and cancel submap creation.
        //    }
        //    return true;
        //}
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)//this lets you right click on the vehicle to load the pawn directly.
        {
            if (!selPawn.CanReach(this.parent, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
            {
                yield break;
            }
            string text = "EnterShuttle".Translate();

            yield return new FloatMenuOption(text, delegate ()
            {
                if (!this.compTransporter.LoadingInProgressOrReadyToLaunch)
                {
                    TransporterUtility.InitiateLoading(Gen.YieldSingle<CompTransporter>(this.compTransporter));
                }
                Job job = JobMaker.MakeJob(JobDefOf.EnterTransporter, this.parent);
                selPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            yield break;
        }        
    }

    public class EmergingDrillShuttle : GroundSpawner
    {
        protected override void Spawn(Map map, IntVec3 loc)
        {
            if (!map.IsPocketMap)
            {
                Quest quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.QuestLookTargets.Contains(this));
                if (quest != null)//if this isn't the return shuttle
                {
                    quest.Notify_SignalReceived(new Signal("Quest" + quest.id + ".ShuttleArrived"));//when emerging done, send signal for quest to spawn shuttle
                }
            }
            else
            {
                //find quest
                //Spawn shuttle stored in quest
            }
        }
    }

    // -------------------------------------------------------------------------------------

    public class LTS_GenStep_FindStartShuttleMap : GenStep
    {
        public override int SeedPart
        {
            get
            {
                return 1568957891;
            }
        }
        public override void Generate(Map map, GenStepParams parms)//find a random space that can fit the drill shuttle
        {
            if (!MapGenerator.PlayerStartSpotValid)
            {
                CellFinder.TryFindRandomCell(map, c => c.IsValid, out var validCell);
                MapGenerator.PlayerStartSpot = validCell;

                
                //emergingDrillShuttle.

                //CameraJumper.TryJump(new GlobalTargetInfo(validCell, map));
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







    //Edited version of Thekiborg's commissioned drillpod code:
    
    public class PawnsArrivalModeWorker_Excavation : PawnsArrivalModeWorker
    {
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            //List<Pawn> tempList = [.. pawns];
            List<Pawn> tempList = new List<Pawn>(pawns);
            int timesToLoop = CalculateHowManyDrillPodsToSpawn(pawns.Count);

            int indexOnList = 0;
            for (int i = 0; i < timesToLoop; i++)
            {
                ThingClass_ExcavationGroundSpawner groundSpawner = (ThingClass_ExcavationGroundSpawner)ThingMaker.MakeThing(LTS_SFE_DefOf.DV_Ethereal_DrillPod);
                //CellFinder.TryFindRandomCellNear(parms.spawnCenter, map, 5, c => GenAdjFast.AdjacentCells8Way(c).All(c => c.GetFirstBuilding(map) == null && c.InBounds(map) && c.IsValid), out var validCell);

                CellFinder.TryFindRandomCellNear(parms.spawnCenter, map, 5, c => c.GetFirstBuilding(map) == null && c.InBounds(map) && c.IsValid, out var validCell);
                //CellFinder.TryFindRandomCellNear(parms.spawnCenter, map, 5, c => c.GetFirstBuilding(map) == null && c.InBounds(map) && c.IsValid && GenAdjFast.AdjacentCells8Way(c).All(ac => ac.GetFirstBuilding(map).def != LTS_SFE_DefOf.DV_Ethereal_DrillPod), out var validCell);
                //CellFinder.TryFindRandomCellNear(parms.spawnCenter, map, 5, c => GenAdjFast.AdjacentCells8Way(c).All(d => d.GetFirstBuilding(map)?.def != LTS_SFE_DefOf.DV_Ethereal_DrillPod && c.InBounds(map) && c.IsValid), out var validCell);

                ThingClass_DrillPod drillPod = (ThingClass_DrillPod)ThingMaker.MakeThing(LTS_SFE_DefOf.DV_Raid_DrillPod);
                groundSpawner.drillPod = drillPod;

                // The count of pawns to get the range of pawns for each drill pod
                // Calculated by multiplying the count of all pawns by the index of the drill pod we're in right now (+1 because indexes start at 0)
                // We divide that by the total amount of drop pods that will be spawned and we substract the pawns already spawned (0 at first iteration)
                int countOfPawns = (tempList.Count * (i + 1) / timesToLoop) - indexOnList;
                drillPod.pawns = tempList.GetRange(indexOnList, countOfPawns);
                GenSpawn.Spawn(groundSpawner, validCell, map);

                //Thing groundSpawnerThing = ThingMaker.MakeThing((Thing)groundSpawner);
                //GenPlace.TryPlaceThing(groundSpawner, validCell, prevMap, ThingPlaceMode.Near);

                //parms.letterHyperlinkThingDefs.Add((Thing)groundSpawner);

                indexOnList += countOfPawns;
            }
        }

        /// <summary>
        /// Finds the raid spawn center using the same logic the insectoids have to spawn.
        /// </summary>
        /// <param name="parms"></param>
        /// <returns></returns>
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            parms.spawnCenter = FindRootTunnelLoc(map, true, true);
            parms.spawnRotation = Rot4.Random;
            return true;
        }

        /// <summary>
        /// Code from infestationutility
        /// </summary>
        /// <param name="map"></param>
        /// <param name="spawnAnywhereIfNoGoodCell"></param>
        /// <param name="ignoreRoofIfNoGoodCell"></param>
        /// <returns></returns>
        private static IntVec3 FindRootTunnelLoc(Map map, bool spawnAnywhereIfNoGoodCell = false, bool ignoreRoofIfNoGoodCell = false)
        {
            if (InfestationCellFinder.TryFindCell(out var cell, map))
            {
                return cell;
            }
            if (!spawnAnywhereIfNoGoodCell)
            {
                return IntVec3.Invalid;
            }
            Func<IntVec3, bool, bool> validator = delegate (IntVec3 x, bool canIgnoreRoof)
            {
                if (!x.Standable(map) || x.Fogged(map))
                {
                    return false;
                }
                if (!canIgnoreRoof)
                {
                    bool flag = false;
                    int num = GenRadial.NumCellsInRadius(3f);
                    for (int i = 0; i < num; i++)
                    {
                        IntVec3 c = x + GenRadial.RadialPattern[i];
                        if (c.InBounds(map))
                        {
                            RoofDef roof = c.GetRoof(map);
                            if (roof != null && roof.isThickRoof)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return false;
                    }
                }
                return true;
            };
            if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => validator(x, arg2: false), map, out cell))
            {
                return cell;
            }
            if (ignoreRoofIfNoGoodCell && RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith((IntVec3 x) => validator(x, arg2: true), map, out cell))
            {
                return cell;
            }
            return IntVec3.Invalid;
        }

        private int CalculateHowManyDrillPodsToSpawn(int countOfPawns)
        {
            //if (countOfPawns <= 8)
            //{
            //    return 1;
            //}
            //else if (countOfPawns <= 12)
            //{
            //    return 2;
            //}
            //else
            //{
            //    return 3;
            //}

            //return (countOfPawns / 2); //2 not including oveflow, so, 3 in most with 2 in the remainder
            return (countOfPawns / 3);
        }
    }

    /// <summary>
    /// Spawns a raider every 250 ticks, won't do anything when there's any raiders left to spawn in the pod. Presumably destroys itself with a comp
    /// </summary>
    public class ThingClass_DrillPod : Building
    {
        internal List<Pawn> pawns = new List<Pawn>();

        public override void TickRare()
        {
            base.TickRare();
            Pawn pawn;
            if (!pawns.NullOrEmpty())
            {
                pawn = (Pawn)GenSpawn.Spawn(pawns[0], Position, Map);
                pawns.Remove(pawn);
            }
            else
            {
                

                GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null), base.Position, Map, ThingPlaceMode.Near, null, null, null, 1);
                if (this.def.soundOpen != null)
                {
                    this.def.soundOpen.PlayOneShot(new TargetInfo(base.Position, Map, false));
                }
                this.Destroy(DestroyMode.Vanish);
            }
        }
    }

    /// <summary>
    /// Will spawn the drillPod once the animation finishes
    /// </summary>
    public class ThingClass_ExcavationGroundSpawner : GroundSpawner
    {
        internal Thing drillPod;
        protected override void Spawn(Map map, IntVec3 loc)
        {
            GenSpawn.Spawn(drillPod, loc, map);
            base.Spawn(map, loc);
        }
    }
}