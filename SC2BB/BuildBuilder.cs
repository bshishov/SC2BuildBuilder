using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using SQLiteTools;
using System;

namespace SC2BB2
{
    static class BuildBuilder
    {
        public static Dictionary<string, BaseEntity> Entities = new Dictionary<string, BaseEntity>();
        public static Dictionary<int, Target> Targets = new Dictionary<int, Target>();
        public static SQLiteDriver Driver;
        public static int Id = 0;

        public static void AddTarget(int time, string etype, int count)
        {
            Targets.Add(time, new Target(etype, count));
        }

        public static void AddLimit(string etype, int limit)
        {
            Entities[etype].MaxUnits = limit;
        }

        public static void Terrans()
        {
            var cc = BuildBuilder.Entities["CommandCenter"];
            var scv = BuildBuilder.Entities["SCV"];        

            var s = new Snapshot(BuildBuilder.Id++);
            s.Minerals = 50;
            s.Supply = -11 + 6;
            s.Vespen = 0;
            s.Entities.Add(new Entity(cc, 0));
            s.Entities.Add(new Entity(scv, 1));
            s.Entities.Add(new Entity(scv, 2));
            s.Entities.Add(new Entity(scv, 3));
            s.Entities.Add(new Entity(scv, 4));
            s.Entities.Add(new Entity(scv, 5));
            s.Entities.Add(new Entity(scv, 6));
            
            foreach (var e in s.Entities)            
                e.State = EnityState.Idle;

            s.Time = 0;
            s.Active = true;
            s.Save();        
        }

        public static void Run()
        {
            string fileName = "test.db";
            if (File.Exists(fileName))
                File.Delete(fileName);

            Driver = new SQLiteDriver(fileName);

            string initQuery = @"
                CREATE TABLE IF NOT EXISTS Snapshots   ( 	
	                id              INTEGER PRIMATY KEY,
                    time_created    INTEGER,	                
	                minerals        REAL,
	                vespen          REAL,
	                supply          INTEGER,
                    active          BOOLEAN                  
                );
                
                CREATE TABLE IF NOT EXISTS Entities (
                    snapshot_id             INTEGER,                    
	                entity_id               INTEGER,
                    time                    INTEGER,
	                base                    INTEGER,
	                production_entity_id    INTEGER,
	                attached_to             INTEGER,
	                state                   INTEGER,
	                process                 INTEGER
                );

                --CREATE INDEX S1 ON Snapshots ( active );
                --CREATE INDEX E1 ON Entities ( snapshot_id );

                PRAGMA synchronous = OFF;
                PRAGMA journal_mode = MEMORY;
                PRAGMA temp_store = MEMORY;
                --PRAGMA foreign_keys = ON;
            ";            
            Driver.Query(initQuery);

            Terrans();

            int maxTime = 0;
            foreach(var t in Targets.Keys)
                if (t > maxTime) maxTime = t;

            for (int time = 0; time <= maxTime; time++)
            {
                var reader = Driver.Query(String.Format("SELECT * FROM Snapshots WHERE time_created = {0}", time));
                var snapshots = new List<Snapshot>();
                while (reader.Read())
                {                    
                    Snapshot s = new Snapshot(Convert.ToInt32(reader["id"]));                    
                    s.Time = Convert.ToInt32(reader["time_created"]);
                    s.Minerals = Convert.ToDouble(reader["minerals"]);
                    s.Vespen = Convert.ToDouble(reader["vespen"]);
                    s.Supply = Convert.ToInt32(reader["supply"]);
                    s.FetchEntities();
                    s.CheckActions();
                    snapshots.Add(s);                   
                }
                reader.Close();
                                
                Parallel.ForEach(snapshots, s =>
                {
                    s.Step(time);
                    if (!s.Active)
                    {
                        Driver.Query(String.Format("DELETE FROM Snapshots WHERE id = {0};", s.Id));
                        Driver.Query(String.Format("DELETE FROM Entities WHERE snapshot_id = {0};", s.Id));
                    }
                });

                /*
                foreach (var s in snapshots)
                {
                    s.Step(time);
                    if (!s.Active)
                    {
                        Driver.Query(String.Format("DELETE FROM Snapshots WHERE id = {0};", s.Id));
                        Driver.Query(String.Format("DELETE FROM Entities WHERE snapshot_id = {0};", s.Id));
                    }

                }*/
                Console.WriteLine(time + " : " + snapshots.Count);
            }           

            Driver.Close();
        }
    }
}

