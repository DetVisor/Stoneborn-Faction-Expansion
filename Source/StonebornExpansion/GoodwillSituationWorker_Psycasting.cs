using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace StonebornExpansion
{
    public class GoodwillSituationWorker_Psycasting : GoodwillSituationWorker
    {
        public override int GetNaturalGoodwillOffset(Faction other)
        {
            PsycastHateExtension extension = other.def.GetModExtension<PsycastHateExtension>();

            if (extension == null)
            {
                return 0;
            }

            int levels = 0;
            List<Pawn> pawns = PawnsFinder.AllMaps_FreeColonists;

            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pawns[i];
                Hediff_Psylink psylink = pawn.health.hediffSet.GetFirstHediff<Hediff_Psylink>();

                if (psylink != null)
                {
                    levels += psylink.level;
                }
            }

            return levels * extension.goodwillPerLevel;
        }
    }

    public class PsycastHateExtension : DefModExtension
    {
        public int goodwillPerLevel = -10;
    }
}
