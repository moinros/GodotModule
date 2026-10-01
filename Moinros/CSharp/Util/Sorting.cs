using System.Collections.Generic;

namespace Moinros.CSharp.Util
{

    /// <summary>
    /// 自定义排序规则接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISortCompare<T>
    {
        bool Compare(T a, T b);
    }

    // 排序算法
    public class Sorting<T>
    {
        private ISortCompare<T> _compare;

        public Sorting(ISortCompare<T> compare)
        {
            _compare = compare;
        }
        // ==============================   快速排序算法   =============================== //
        //	
        // 	调用方式:
        // 	int[] array = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        // 	Func<int, int, bool> compare = (a, b) => a.Value > b.Value;
        // 	Sort(array, compare);
        //
        /// <summary>
        /// - 快速排序算法 - 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">>需要排序的数组</param>
        /// <param name="compare">自定义比较方法</param>
        public void Sort(T[] array)
        {
            QuickSort(array, 0, array.Length - 1);
        }

        /// <summary>
        /// - 快速排序算法 -
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">需要排序的List,</param>
        /// <param name="compare">自定义比较方法</param>
        public void Sort(List<T> list)
        {
            Sort(list.ToArray());
        }

        /// <summary>
        /// 倒序排序
        /// </summary>
        public void ReversalSort(T[] array)
        {
            Sort(array);
            int left = 0;
            int right = array.Length - 1;
            while (left < right)
            {
                // 交换左右两边的元素
                (array[right], array[left]) = (array[left], array[right]);
                // 移动索引
                left++;
                right--;
            }
        }

        /// <summary>
        /// 倒序排序
        /// </summary>
        public void ReversalSort(List<T> array)
        {
            ReversalSort(array.ToArray());
        }

        /// <summary>
        /// 快速排序
        /// </summary>
        private void QuickSort(T[] array, int left, int right)
        {
            if (left < right)
            {
                // 找到中间元素的索引
                int pivotIndex = Partition(array, left, right);
                // 递归地对左侧和右侧子数组进行排序
                QuickSort(array, left, pivotIndex - 1);
                QuickSort(array, pivotIndex + 1, right);
            }
        }

        // 分区方法，用于找到中间元素的索引
        private int Partition(T[] array, int left, int right)
        {
            // 选择最右侧的元素作为基准值
            T pivot = array[right];
            // 初始化一个索引，小于该索引的元素都比基准值小
            int i = left - 1;
            for (int j = left; j < right; j++)
            {
                // 如果当前元素小于或等于基准值
                //   if (array[j] <= pivot)
                if (_compare.Compare(array[j], pivot))
                {
                    // 增加索引
                    i++;
                    // 交换元素
                    Swap(array, i, j);
                }
            }
            // 将基准值放到正确的位置
            Swap(array, i + 1, right);
            // 返回基准值的索引
            return i + 1;
        }

        // 交换数组中的两个元素
        private void Swap(T[] array, int i, int j)
        {
            // int temp = array[i];
            // array[i] = array[j];
            // array[j] = temp;
            (array[j], array[i]) = (array[i], array[j]);
        }

        // ==============================   快速排序算法   =============================== //
        /// <summary>
        /// 数组扩容
        /// </summary>
        /// <param name="array">需要扩容的泛型数组</param>
        /// <param name="count">指定扩容的长度</param>
        /// <returns>新的数组</returns>
        public T[] ArrayExpanded(T[] array, int count)
        {
            T[] values = new T[array.Length + count];
            for (int i = 0; i < array.Length; i++)
            {
                values[i] = array[i];
            }
            return values;
        }

    }
}