using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Carnivale
{
    public class WorkGiver_HaulToCarrier : WorkGiver
    {
        public override bool ShouldSkip(Pawn pawn)
        {
            return !pawn.IsCarny();
        }

        public override Job NonScanJob(Pawn pawn)
        {
            var lord = pawn.GetLord();

            if (lord.LordJob is LordJob_EntertainColony)
            {
                var info = pawn.Map.GetComponent<CarnivalInfo>();
                if (info.thingsToHaul.Any())
                {
                    var haulable = info.thingsToHaul.Pop();
                    foreach (var carrier in info.pawnsWithRole[CarnivalRole.Carrier])
                    {
                        if (carrier.HasSpaceFor(haulable))
                        {

                        }
                    }
                }
            }

            return null;
        }

        

    }
}
