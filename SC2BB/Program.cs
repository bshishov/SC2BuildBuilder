using System;
using System.IO;

namespace SC2BB2
{
    class Program
    {
        static void Main(string[] args)
        {            
            var s = new StreamReader("sc2terrans.csv");
            while (!s.EndOfStream)
            {
                var line = s.ReadLine();
                string[] vals = line.Split(',');

                string type = vals[0];
                BaseEntity e = new BaseEntity();
                e.Name = type;
                e.MineralsCost = Convert.ToInt32(vals[1]);
                e.VespenCost = Convert.ToInt32(vals[2]);
                e.SupplyCost = Convert.ToInt32(vals[3]);
                e.ProduceTime = Convert.ToInt32(vals[4]);
                e.MaxEnergy = Convert.ToInt32(vals[7]);

                var requireStr = vals[5];
                string[] requires = requireStr.Split('/');

                foreach (var req in requires)
                    e.Requires(req);

                var health = Convert.ToInt32(vals[6]);                
                BuildBuilder.Entities.Add(e.Name, e);
            }
            
            BuildBuilder.Targets.Add(540, new Target("CommandCenter", 20));
            BuildBuilder.Init("test.db3");
            BuildBuilder.Terrans();
            BuildBuilder.Run();
        }
    }
}
