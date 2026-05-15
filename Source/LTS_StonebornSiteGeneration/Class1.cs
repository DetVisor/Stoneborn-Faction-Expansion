using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;

namespace LTS_StonebornSiteGeneration
{
    [DefOf]
    public static class LTS_SFE_DefOf
    {
        public static SitePartDef LTS_StonebornRuinSite;
        static LTS_SFE_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LTS_SFE_DefOf));
        }
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
}
