using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.QuestGen;

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
}
