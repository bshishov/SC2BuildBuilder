using System;

namespace SC2BB2
{
    /// <summary>
    /// Way of development
    /// </summary>
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
}

