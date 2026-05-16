using KCSG;
using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using static UnityEngine.GraphicsBuffer;

namespace LTS_StonebornSiteGeneration
{
    [DefOf]
    public static class LTS_SFE_DefOf
    {
        public static SitePartDef LTS_StonebornRuinSite;
        public static SitePartDef LTS_StonebornVaultSite;
        public static GenStepDef LTS_StonebornVault;
        static LTS_SFE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LTS_SFE_DefOf));
        }
    }

    public class LTS_SFE_ModExtension : DefModExtension
    {
        public string LTS_TexPathOpen;
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
        public override void Generate(Map map, GenStepParams parms)
        {
            if (!MapGenerator.PlayerStartSpotValid)
            {
                MapGenerator.PlayerStartSpot = map.listerBuildings.allBuildingsNonColonist.Where(building => building?.def?.portal != null).First().Position;
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
                yield return new GenStepWithParams(LTS_SFE_DefOf.LTS_StonebornVault, new GenStepParams
                {
                    layout = this.layout
                });
            }
            else
            {
                yield return new GenStepWithParams(LTS_SFE_DefOf.LTS_StonebornVault, default(GenStepParams));
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
            IEnumerator<Gizmo> enumerator = null;
            yield break;
            yield break;
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
            Log.Message("Tick");
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
}
