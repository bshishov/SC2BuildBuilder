using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC2BB2
{   
    enum EnityState
    {
        Building,
        Idle,
        Producing,
    }

    class BaseEntity
    {
        public string EType = "";
        public int MineralsCost;
        public int VespenCost;
        public int EnergyCost;
        public int Supply;
        public int ProduceTime;
        public int MaxEnergy;
        public int MaxUnits;
        public List<string> Require = new List<string>();        
        public List<string> CanProduce = new List<string>();

        public void Produces(string t) { CanProduce.Add(t); }
        public void Requires(string t) { Require.Add(t); }
        public override string ToString(){return EType.ToString();}
    }

    class Entity
    {
        public int Id;
        public int ProductionEntityId;
        public int AttachedToId;

        public BaseEntity Base;  
        public int Process;        
        public int TimeCreated;        
        public EnityState State;
   
        public Entity(BaseEntity b, int id, int time = 0)
        {
            Id = id;
            Base = b;
            TimeCreated = time;
            State = EnityState.Building;            
            ProductionEntityId = -1;
            AttachedToId = -1;
        }

        public Entity(Entity source)
        {
            Id = source.Id;
            ProductionEntityId = source.ProductionEntityId; // this fails
            AttachedToId = source.AttachedToId; // this fails
            TimeCreated = source.TimeCreated;
            Base = source.Base;
            State = source.State;
            Process = source.Process;
        }        

        public override string ToString()
        {
            return TimeCreated.ToString() + ":" + Base.EType.ToString();
        }
    }
}
