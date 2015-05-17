
namespace UnionFind
{
    /// <summary>
    /// Based on Sedgewick's union find implementation.
    /// </summary>
    public class UnionFindWithPathCompression
    {
        private int[] _id;    // id[i] = parent of i
        private int _count;   // number of components

        // Create an empty union find data structure with N isolated sets.
        // Initially each item is in its own disjoint set.
        public UnionFindWithPathCompression(int N)
        {
            _count = N;

            _id = new int[N];
            for (int i = 0; i < N; i++)
            {
                _id[i] = i;
            }
        }

        // Return the number of disjoint sets.
        public int Count()
        {
            return _count;
        }

        // Return component identifier for component containing p
        public int Find(int p)
        {
            // Find the root under which p is stored.
            int root = p;
            while (root != _id[root])
            {
                root = _id[root];
            }

            // Perform path compression by linking every
            // parent of p directly to the root.
            while (p != root)
            {
                int newp = _id[p];
                _id[p] = root;
                p = newp;
            }

            return root;
        }

        // Are objects p and q in the same set?
        public bool Connected(int p, int q)
        {
            return Find(p) == Find(q);
        }

        // Replace sets containing p and q with their union.
        public void Union(int p, int q)
        {
            int i = Find(p);
            int j = Find(q);
            if (i == j)
            {
                // Already in same set
                return;
            }

            // Combine sets
            _id[i] = j;
            _count--;
        }
    }
}
