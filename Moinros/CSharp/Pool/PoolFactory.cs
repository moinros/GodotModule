
using Godot;
using Moinros.CSharp.Util;

namespace Moinros.CSharp.Pool
{
    /// <summary>
    /// 对象池模版接口,使用对象池必须先实现此接口
    /// </summary>
    public interface IPoolTemplate
    {
        /// <summary>
        /// 判断对象是否可用
        /// </summary>
        /// <returns>true:可用, false:不可用</returns>
        bool IsReady();

        /// <summary>
        /// 设置对象是否可用
        /// </summary>
        /// <param name="ready">true:可用, false:不可用</param>
        void SetReady(bool ready);

        /// <summary>
        /// 判断对象是否还在场景树中
        /// </summary>
        /// <returns>true:还在场景树</returns>
        bool InScene();

        /// <summary>
        /// 重置对象参数
        /// </summary>
        void Reset();

        /// <summary>
        /// 释放对象
        /// </summary>
        void Release();

        /// <summary>
        /// 回收对象
        /// </summary>
        void Recycle();

        /// <summary>
        /// 设置对象在池里的初始坐标
        /// </summary>
        /// <param name="pos">坐标x</param>
        void SetPoolPosition(Vector2 pos);
    }

    /// <summary>
    /// 对象池工厂,公用的对象池生成方法.使用时继承此类,并实现IPoolTemplate接口即可使用对象池
    /// </summary>
    /// <typeparam name="Mark">对象分组标记类型</typeparam>
    /// <typeparam name="T">对象类型</typeparam> 
    public abstract class PoolFactory<Mark, T> where T : IPoolTemplate
    {
        /// <summary>
        /// 对象分组
        /// </summary>
        protected class Group
        {
            /// <summary>
            /// 标记
            /// </summary>
            public Mark mark;
            /// <summary>
            /// 对象列表
            /// </summary>
            public LinkList<T> list;
        }

        protected class PoolIterator : IIterator<T>
        {
            public bool Meet(T item)
            {
                return item.IsReady();
            }
        }

        protected class GroupCompare : ICompare<Group, Mark>
        {
            public bool Compare(Group self, Mark param)
            {
                return self.mark.Equals(param);
            }
        }

        protected readonly LinkList<Group> _Prefabs = new();
        private readonly IIterator<T> pi = new PoolIterator();
        private readonly ICompare<Group, Mark> gi = new GroupCompare();

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="mark">对象分组标记</param>
        /// <returns>如果没有可用对象则返回null</returns>
        protected virtual T GetObject(Mark mark)
        {
            Group group = _Prefabs.FindValue(gi, mark);
            if (group is null)
            {
                return default;
            }
            LinkList<T> link = group.list;
            T obj = link.FindValue(pi);
            obj?.SetReady(false);
            return obj;
        }

        /// <summary>
        /// 添加对象
        /// </summary>
        /// <param name="mark">对象组标记</param>
        /// <param name="obj">对象</param>
        /// <returns>对象自身</returns>
        protected virtual T AddObject(Mark mark, T obj)
        {
            // 获取对象组
            Group group = _Prefabs.FindValue(gi, mark);
            if (group == null)
            {
                // 新建对象组
                group = new()
                {
                    mark = mark,
                    list = new LinkList<T>()
                };
                _Prefabs.AddLast(group);
            }
            group.list.AddLast(obj);
            return obj;
        }
    }


}