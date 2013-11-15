using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;

namespace SC2BB2
{
    class Path
    {
        public BaseEntity Ent;
        public bool Completed;

        public Path(BaseEntity e)
        {
            Completed = false;
            Ent = e;
        }
    }

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

        public Snapshot(int id)
        {
            Id = id;
            Active = true;
        }

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

        public void Save()
        {
            //BuildBuilder.Driver.Query("BEGIN TRANSACTION IMMEDIATE;");
            string insertSnapshot = String.Format(@"
            INSERT INTO Snapshots 
            (id, time_created, minerals, vespen, supply, active) VALUES
            ({0},{1},          {2},      {3},    {4},    '{5}')"
            , Id, Time, Minerals.ToString(CultureInfo.InvariantCulture), Vespen.ToString(CultureInfo.InvariantCulture), Supply, Active);
            BuildBuilder.Driver.NonExecuteQuery(insertSnapshot);

            foreach (var e in Entities)
            {
                string insertEntity = String.Format(@"
                INSERT INTO Entities 
                (snapshot_id, entity_id, time, base, production_entity_id, attached_to, state, process) VALUES
                ({0}, {1}, {2}, '{3}', {4}, {5}, {6}, {7})
            ", Id, e.Id, e.TimeCreated, e.Base.EType, e.ProductionEntityId, e.AttachedToId, (int)e.State, e.Process);
                BuildBuilder.Driver.NonExecuteQuery(insertEntity);
            }

            //BuildBuilder.Driver.Query("COMMIT TRANSACTION;");
        }       

        bool IsAvailable(BaseEntity ent)
        {
            // Если больше чем нужно то не предлагать
            if (Entities.FindAll(o => o.Base.EType == ent.EType).Count() >= ent.MaxUnits) 
                return false;

            // Pass the requirements or not?
            foreach (var e in ent.Require)
            {
                if (Entities.Find(c => c.Base.EType == e) == null)
                    return false;
            }

            return true;
        }

        public void CheckActions()
        {
            foreach (var e in BuildBuilder.Entities.Values)
            {
                if (IsAvailable(e))
                    Paths.Add(new Path(e));
            }
        }

        public bool CheckTargets(int time)
        {
            if (BuildBuilder.Targets.ContainsKey(time))
            {
                var target = BuildBuilder.Targets[time];

                if (Entities.FindAll(e => e.Base.EType == target.EType).Count < target.Count)
                    return false;
            }

            return true;
        }

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
            if (Supply + e.Supply > 0) return false;            

            Snapshot s = new Snapshot(this, BuildBuilder.Id++);            

            // Выбрать простаивающие сущности которые могу произвести данный юнит / здание
            var builders = s.Entities.FindAll(ent => ent.Base.CanProduce.Find(t => t == e.EType) != null && ent.State == EnityState.Idle);

            // Если строить предложенный юнит некому то выходим
            if (builders.Count == 0) return false;

            s.Minerals -= e.MineralsCost;
            s.Vespen -= e.VespenCost;
            s.Supply += e.Supply;
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

        public void Step(int time)
        {
            while (Paths.Count > 0)
            {
                if (!CheckTargets(time))
                {
                    Active = false;
                    return;
                }

                var workers = Entities.FindAll(w => w.Base.EType == "SCV");
                var freeworkers = workers.FindAll(w => w.State == EnityState.Idle && w.AttachedToId == -1);
                var ccList = new List<int>();
                var refList = new List<int>();

                foreach (var w in freeworkers)
                    w.AttachedToId = Entities.Find(e => e.Base.EType == "CommandCenter").Id;
                
                foreach (var e in Entities)
                {
                    if (e.Base.EType == "CommandCenter")
                        ccList.Add(e.Id);

                    if (e.Base.EType == "Refinery")
                        refList.Add(e.Id);

                    if (e.State == EnityState.Producing)
                    {
                        var productionEntity = Entities.Find(o => o.Id == e.ProductionEntityId);
                        productionEntity.Process -= 1;

                        if (productionEntity.Process <= 0)
                        {
                            e.State = EnityState.Idle;
                            e.ProductionEntityId = -1;
                            productionEntity.State = EnityState.Idle;                            
                        }  
                    }
                }

                Minerals += workers.FindAll(w => ccList.Contains(w.AttachedToId)).Count * 0.64f;
                Vespen += workers.FindAll(w => refList.Contains(w.AttachedToId)).Count * 1;
                                
                foreach (var p in Paths)
                    p.Completed = TryBuild(p.Ent, time);

                Paths.RemoveAll(p => p.Completed);
                time += 1;
            }            
            
            Active = false;
        }
    }
}