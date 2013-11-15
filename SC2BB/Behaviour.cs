using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SC2BB2
{
    public static class Behaviour
    {
        public static void BehaviourProcess(Entity e, Snapshot s)
        {
            if (e.State == EnityState.Producing)
            {
                var productionEntity = s.Entities.Find(o => o.Id == e.ProductionEntityId);
                productionEntity.Process -= 1;

                if (productionEntity.Process <= 0)
                {
                    e.State = EnityState.Idle;
                    e.ProductionEntityId = -1;
                    productionEntity.State = EnityState.Idle;
                    productionEntity.AttachedToId = e.Id;
                }
            }
        }        
    }
}
