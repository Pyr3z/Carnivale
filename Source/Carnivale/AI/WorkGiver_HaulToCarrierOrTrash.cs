﻿//using RimWorld;
//using System.Linq;
//using Verse;
//using Verse.AI;
//using Verse.AI.Group;

//namespace Carnivale
//{
//    // Perhaps this is not favourable over using a duty
//    public class WorkGiver_HaulToCarrierOrTrash : WorkGiver
//    {
//        public override bool ShouldSkip(Pawn pawn)
//        {
//            return !pawn.IsCarny();
//        }

//        public override Job NonScanJob(Pawn pawn)
//        {
//            var lord = pawn.GetLord();

//            if (lord.LordJob is LordJob_EntertainColony)
//            {
//                var info = pawn.Map.GetComponent<CarnivalInfo>();
//                if (info.thingsToHaul.Any())
//                {
//                    var haulable = info.thingsToHaul.Pop();
                    
//                    if (haulable != null)
//                    {
//                        return new Job(_DefOf.Job_HaulToCarrierOrTrash, haulable)
//                        {
//                            lord = lord
//                        };
//                    }
//                }
//            }

//            return null;
//        }

        

//    }
//}