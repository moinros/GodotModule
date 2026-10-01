using System;
using Godot;

namespace Moinros.CSharp.Util
{
    /// <summary>
    /// 处理数据分页的工具类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class DataPaging<T>
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="size">每页显示的数据数量</param>
        /// <param name="array">数据集</param>
        public DataPaging(int size, T[] array)
        {
            DataSize = size;
            DataArray = array;
        }

        /// <summary>
        /// 构造函数, 初始化指定每页显示的数据数量
        /// </summary>
        /// <param name="size">每页显示的数据数量</param>
        public DataPaging(int size)
        {
            DataSize = size;
        }

        /// <summary>
        /// 每页显示的数据条数
        /// </summary>
        public int DataSize;

        /// <summary>
        /// 所有数据
        /// </summary>
        public T[] DataArray;

        /// <summary>
        /// 用于显示分页数据的对象.包含当前页数, 总页数, 每页显示的数据条数, 当前页数据
        /// </summary>
        public class Page
        {
            /// <summary>
            /// 当前页数
            /// </summary>
            public int PageNumber;

            /// <summary>
            /// 总页数
            /// </summary>
            public int PageSize;

            /// <summary>
            /// 每页显示的数据条数
            /// </summary>
            public int DataSize;
            /// <summary>
            /// 当前页数据
            /// </summary>
            public T[] List;
        }

        /// <summary>
        /// 获取指定页码的指定位置数据
        /// </summary>
        /// <param name="number"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetPageValue(int number, int index)
        {
            if (DataArray == null)
            {
                return default;
            }
            // // 输入校验
            if (number < 1) { number = 1; }
            if (DataSize < 1) { DataSize = 1; }

            // 计算总页数
            int totalPages = (DataArray.Length + DataSize - 1) / DataSize;
            if (number > totalPages) { number = totalPages; }

            // 动态计算实际需要的数组大小
            int startIndex = (number - 1) * DataSize;
            int endIndex = Math.Min(startIndex + DataSize, DataArray.Length);
            // 填充数组
            int ind = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (index == ind)
                {
                    return DataArray[i];
                }
                ind++;
            }
            return default;
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <param name="number">指定页码</param>
        /// <returns>分页后的数据</returns>
        public Page GetPageList(int number)
        {
            // 空引用检查
            if (DataArray == null || DataArray.Length == 0)
            {
                return new Page
                {
                    PageNumber = 1,
                    PageSize = 1,
                    DataSize = DataSize < 1 ? 1 : DataSize,
                    List = null
                };
            }
           
            // // 输入校验
            if (number < 1) { number = 1; }
            if (DataSize < 1) { DataSize = 1; }

            // 计算总页数
            int totalPages = (DataArray.Length + DataSize - 1) / DataSize;
            if (number > totalPages) { number = totalPages; }

            // 动态计算实际需要的数组大小
            int startIndex = (number - 1) * DataSize;
            int endIndex = Math.Min(startIndex + DataSize, DataArray.Length);

            // 动态调整数组大小
            T[] pagedList = new T[endIndex - startIndex];
            // 填充数组
            int index = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                pagedList[index] = DataArray[i];
                index++;
            }

            // 返回结果
            return new Page
            {
                PageNumber = number,
                PageSize = totalPages,
                DataSize = DataSize,
                List = pagedList,
            };
        }


    }
}