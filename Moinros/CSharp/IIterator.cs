namespace Moinros.CSharp
{
    /// <summary>
    /// 自定义迭代器接口
    /// </summary>
    public interface IIterator<T>
    {
        /// <summary>
        /// 判断元素是否满足条件
        /// </summary>
        /// <param name="item">元素自身</param>
        /// <returns>满足条件返回true,否则返回false</returns>
        bool Meet(T item);
    }
}