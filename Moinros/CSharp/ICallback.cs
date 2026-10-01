namespace Moinros.CSharp
{
    /// <summary>
    /// 自定义回调接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICallback<T>
    {

        /// <summary>
        /// 回调方法
        /// </summary>
        /// <param name="temp">参数</param>
        void CallMethod(T temp);
    }

    /// <summary>
    /// 自定义双参数回调接口
    /// </summary>
    /// <typeparam name="P1"></typeparam>
    /// <typeparam name="P2"></typeparam>
    public interface ICallback<P1, P2>
    {
        /// <summary>
        /// 回调方法
        /// </summary>
        /// <param name="param1">参数1</param>
        /// <param name="param2">参数2</param>
        /// <returns></returns>
        void CallMethod(P1 param1, P2 param2);
    }

}