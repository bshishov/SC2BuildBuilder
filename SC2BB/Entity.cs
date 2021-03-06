﻿using System.Collections.Generic;

namespace SC2BB2
{   
    enum EnityState
    {
        Building, // Entity is not ready yet
        Idle, // Doing nothing
        Producing, // Entity producing another entity
    }

    // Describes base parameters from csv file
    class BaseEntity
    {
        public string Name = "";
        public int MineralsCost;
        public int VespenCost;
        public int EnergyCost;
        public int SupplyCost;
        public int ProduceTime;
        public int MaxEnergy;
        public int Limit;
        public List<string> Require = new List<string>();        
        public List<string> CanProduce = new List<string>();

        public void Produces(string t) { CanProduce.Add(t); }
        public void Requires(string t) { Require.Add(t); }
        public override string ToString(){return Name.ToString();}
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
            return TimeCreated.ToString() + ":" + Base.Name.ToString();
        }
    }
}
