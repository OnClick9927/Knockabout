using System.Collections.Generic;

namespace Lockstep.Nav
{
    /// <summary>
    /// A* 开放集使用的节点索引最小堆。
    /// fScore 数组由外部 SearchContext 持有，positions 允许已有节点在分数降低后 O(log n) 上浮。
    /// 分数相同时按节点索引打破平局，保证锁步端得到稳定搜索顺序。
    /// </summary>
    class MinHeap
    {
        private readonly List<int> heap = new List<int>();
        private LFloat[] fScore;
        private int[] positions;

        public int Count => heap.Count;

        /// <summary>绑定本次搜索的评分数组，并清除上次堆位置。</summary>
        public void Init(LFloat[] scores)
        {
            if (positions == null || positions.Length != scores.Length)
            {
                positions = new int[scores.Length];
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = -1;
            }
            else
            {
                for (int i = 0; i < heap.Count; i++)
                    positions[heap[i]] = -1;
            }

            heap.Clear();
            if (heap.Capacity < scores.Length)
                heap.Capacity = scores.Length;
            fScore = scores;
        }

        /// <summary>插入新节点；节点已在堆中时执行分数降低后的上浮。</summary>
        public void Push(int node)
        {
            int position = positions[node];
            if (position >= 0)
            {
                SiftUp(position);
                return;
            }

            positions[node] = heap.Count;
            heap.Add(node);
            SiftUp(heap.Count - 1);
        }

        public int Pop()
        {
            int result = heap[0];
            int lastIndex = heap.Count - 1;
            int lastNode = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            positions[result] = -1;

            if (heap.Count > 0)
            {
                heap[0] = lastNode;
                positions[lastNode] = 0;
                SiftDown(0);
            }
            return result;
        }

        private bool Less(int lhs, int rhs)
        {
            if (fScore[lhs] != fScore[rhs])
                return fScore[lhs] < fScore[rhs];
            return lhs < rhs;
        }

        private void SiftUp(int position)
        {
            while (position > 0)
            {
                int parent = (position - 1) >> 1;
                if (!Less(heap[position], heap[parent])) break;
                Swap(position, parent);
                position = parent;
            }
        }

        private void SiftDown(int position)
        {
            while (true)
            {
                int left = (position << 1) + 1;
                int right = left + 1;
                int smallest = position;
                if (left < heap.Count && Less(heap[left], heap[smallest]))
                    smallest = left;
                if (right < heap.Count && Less(heap[right], heap[smallest]))
                    smallest = right;
                if (smallest == position) break;
                Swap(position, smallest);
                position = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            int temp = heap[a];
            heap[a] = heap[b];
            heap[b] = temp;
            positions[heap[a]] = a;
            positions[heap[b]] = b;
        }
    }
}
