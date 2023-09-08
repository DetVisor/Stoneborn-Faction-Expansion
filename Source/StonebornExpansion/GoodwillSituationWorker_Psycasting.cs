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
    }

    public class PsycastHateExtension : DefModExtension
    {
        public int goodwillPerLevel = -5;
    }
}
