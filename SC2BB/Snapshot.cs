using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;

namespace SC2BB2
{ 
    /// <summary>
    /// Current game state
    /// </summary>
    class Snapshot
    {
        public int Id;
        public double Minerals;
        public double Vespen;
        public int Supply;
        public int Time;
        public bool Active;

        public List<Entity> Entities = new List<Entity>();
        public List<Path> Paths = new List<Path>();

        /// <summary>
        /// Root snapshot cinstructor
        /// </summary>        
        public Snapshot(int id)
        {
            Id = id;
            Active = true;
        }

        /// <summary>
        /// Constructor for child snapshot
        /// </summary>        
        public Snapshot(Snapshot parent, int id)
        {
            Active = true;
            Id = id;

            foreach (var e in parent.Entities)            
                Entities.Add(new Entity(e));

            Minerals = parent.Minerals;
            Vespen = parent.Vespen;
            Supply = parent.Supply;
        }

        /// <summary>
        /// Selects database rows relative to this snapshot
        /// and parses it to Entities
        /// </summary>
        public void FetchEntities()
        {
            var reader = BuildBuilder.Driver.Query(string.Format("Select * FROM Entities WHERE snapshot_id = {0};", Id));
            while (reader.Read())
            {
                Entity e = new Entity(BuildBuilder.Entities[reader.GetString(3)], reader.GetInt32(1));                
                e.TimeCreated = reader.GetInt32(2);
                e.ProductionEntityId = reader.GetInt32(4);
                e.AttachedToId = reader.GetInt32(5);
                e.State = (EnityState)reader.GetInt16(6);
                e.Process = reader.GetInt16(7);
                Entities.Add(e);
            }
        }

        /// <summary>
        /// Save current state and its entities to database
        /// </summary>
        public void Save()
        {   
            // Save snapshot data
            string insertSnapshot = String.Format(@"
            INSERT INTO Snapshots 
            (id, time_created, minerals, vespen, supply, active) VALUES
            ({0},{1},          {2},      {3},    {4},    '{5}')"
            , Id, Time, Minerals.ToString(CultureInfo.InvariantCulture), Vespen.ToString(CultureInfo.InvariantCulture), Supply, Active);
            BuildBuilder.Driver.NonExecuteQuery(insertSnapshot);

            // Save snapshot entities
            foreach (var e in Entities)
            {
                string insertEntity = String.Format(@"
                INSERT INTO Entities 
                (snapshot_id, entity_id, time, base, production_entity_id, attached_to, state, process) VALUES
                ({0}, {1}, {2}, '{3}', {4}, {5}, {6}, {7})
            ", Id, e.Id, e.TimeCreated, e.Base.Name, e.ProductionEntityId, e.AttachedToId, (int)e.State, e.Process);
                BuildBuilder.Driver.NonExecuteQuery(insertEntity);
            }            
        }       

        /// <summary>
        /// "Can we build this entity?"
        /// </summary>        
        bool IsAvailable(BaseEntity ent)
        {
            // Chech limit
            if (Entities.FindAll(o => o.Base.Name == ent.Name).Count() >= ent.Limit) 
                return false;

            // Pass the requirements or not?
            foreach (var e in ent.Require)
            {
                if (Entities.Find(c => c.Base.Name == e) == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check ways of future development
        /// </summary>
        public void CheckActions()
        {
            foreach (var e in BuildBuilder.Entities.Values)
            {
                if (IsAvailable(e))
                    Paths.Add(new Path(e));
            }
        }

        /// <summary>
        /// Checks targets reached or not
        /// </summary>        
        public bool CheckTargets(int time)
        {
            if (BuildBuilder.Targets.ContainsKey(time))
            {
                var target = BuildBuilder.Targets[time];

                if (Entities.FindAll(e => e.Base.Name == target.Name).Count < target.Count)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns max entity id
        /// </summary>        
        public int MaxId()
        {
            int max = -1;
            foreach (var s in Entities)            
                max = Math.Max(max,s.Id);            
            return max;
        }

        public bool TryBuild(BaseEntity e, int time)
        {
            if (Minerals < e.MineralsCost) return false;
            if (Vespen < e.VespenCost) return false;
            if (Supply + e.SupplyCost > 0) return false;            

            Snapshot s = new Snapshot(this, BuildBuilder.SnapshotId++);            

            // Выбрать простаивающие сущности которые могу произвести данный юнит / здание
            var builders = s.Entities.FindAll(ent => ent.Base.CanProduce.Find(t => t == e.Name) != null && ent.State == EnityState.Idle);

            // Если строить предложенный юнит некому то выходим
            if (builders.Count == 0) return false;

            s.Minerals -= e.MineralsCost;
            s.Vespen -= e.VespenCost;
            s.Supply += e.SupplyCost;
            s.Time = time;

            Entity entity = new Entity(e, s.MaxId() + 1, time);
            entity.Process = e.ProduceTime;
            s.Entities.Add(entity);

            // Задаем первому попавшемуся строителю новое задание)
            builders[0].ProductionEntityId = entity.Id;
            builders[0].State = EnityState.Producing;

            s.Save();
            return true;
        }

        /// <summary>
        /// Main processing method
        /// </summary>
        /// <param name="time"></param>
        public void Step(int time)
        {
            while (Paths.Count > 0)
            {
                if (!CheckTargets(time))
                {
                    Active = false;
                    return;
                }               
                
                foreach (var e in Entities)
                    Behaviour.BehaviourProcess(e, this);

                foreach (var p in Paths)
                    p.Completed = TryBuild(p.Ent, time);

                Paths.RemoveAll(p => p.Completed);
                time += 1;
            }            
            
            Active = false;
        }
    }
}