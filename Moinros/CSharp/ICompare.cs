namespace Moinros.CSharp
{
    /// <summary>
    /// 自定义比较器接口
    /// </summary>
    public interface ICompare<T, P>
    {
        /// <summary>
        /// 比较两个元素的指定值是否相同
        /// </summary>
        /// <param name="self">元素自身</param>
        /// <param name="param">比较参数</param>
        /// <returns>满足条件返回true,否则返回false</returns>
        bool Compare(T self, P param);
    }

}