namespace Moinros.CSharp.Util
{

    /// <summary>
    /// 链表接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    interface ILink<T>
    {
        /// <summary>
        /// 链表长度
        /// </summary>
        int Length();

        /// <summary>
        /// 把元素添加到链表头部
        /// </summary>
        void AddHead(T value);

        /// <summary>
        /// 把元素添加到链表尾部
        /// </summary>
        void AddLast(T value);

        /// <summary>
        /// 把元素数组添加到链表尾部
        /// </summary>
        /// <param name="array">元素数组</param>
        void AddArray(T[] array);

        /// <summary>
        /// 移除指定元素
        /// </summary>
        void Remove(T value);

        /// <summary>
        /// 移除指定位置的元素
        /// </summary>
        /// <param name="index"></param>
        void Remove(int index);

        /// <summary>
        /// 移除指定元素,并返回移除的元素
        /// </summary>
        /// <typeparam name="P">匹配的参数类型</typeparam>
        /// <param name="compare">比较器</param>
        /// <param name="param">比较的参数</param>
        /// <returns>移除的元素</returns>
        T Remove<P>(ICompare<T, P> compare, P param);

        /// <summary>
        /// 移除指定元素,并在回调中做后续处理
        /// </summary>
        /// <typeparam name="P">匹配的参数类型</typeparam>
        /// <param name="call">回调函数</param>
        /// <param name="compare">比较器</param>
        /// <param name="param">比较的参数</param>
        void Remove<P>(ICallback<T> call, ICompare<T, P> compare, P param);

        /// <summary>
        /// 移除所有元素
        /// </summary>
        void RemoveAll();

        /// <summary>
        /// 移除所有元素,并在回调中做后续处理
        /// </summary>
        /// <param name="call">回调函数</param>
        void RemoveAll(ICallback<T> call);

        /// <summary>
        /// 获取第一个元素
        /// </summary>
        /// <returns>T</returns>
        T GetHead();

        /// <summary>
        /// 获取最后一个元素
        /// </summary>
        /// <returns>T</returns>
        T GetLast();

        /// <summary>
        /// 获得指定元素的上一个元素
        /// </summary>
        T ValuePrevious(ICompare<T, T> compare, T value);

        /// <summary>
        /// 获得指定元素的下一个元素
        /// </summary>
        T ValueNext(ICompare<T, T> compare, T value);

        /// <summary>
        /// 获取上一个元素,如果到达头部则返回default(T)并重置游标到尾部
        /// </summary>
        /// <returns>T</returns>
        T Previous();

        /// <summary>
        /// 获取下一个元素,如果到达末尾则返回default(T)并重置游标到头部
        /// </summary>
        T Next();

        /// <summary>
        /// 重置游标到默认位置
        /// </summary>
        void RestoreCursor();

        /// <summary>
        /// 判断链表是否包含指定元素
        /// </summary>
        /// <param name="value">T</param>
        /// <returns>bool</returns>
        bool FindValue(T value);

        /// <summary>
        /// 根据索引查找元素
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>T</returns>
        T FindValue(int index);

        // T Meet(IIterator<T> temp);
        /// <summary>
        /// 使用迭代器查找元素
        /// </summary>
        /// <param name="iterator">自定义迭代器接口实现</param>
        /// <returns>符合接口定义的元素T</returns>
        T FindValue(IIterator<T> iterator);

        /// <summary>
        /// 使用比较器查找元素
        /// </summary>
        /// <typeparam name="P">比较链表元素的参数类型</typeparam>
        /// <param name="compare">比较器接口</param>
        /// <param name="param">比较参数</param>
        /// <returns>T</returns>
        T FindValue<P>(ICompare<T, P> compare, P param);

        /// <summary>
        /// 使用比较器查找元素列表
        /// </summary>
        /// <typeparam name="P">比较链表元素的参数类型</typeparam>
        /// <param name="compare">比较器接口</param>
        /// <param name="param">比较参数</param>
        /// <returns></returns>
        T[] FindValueList<P>(ICompare<T, P> compare, P param);

        /// <summary>
        /// 更新元素值
        /// </summary>
        bool UpdateValue<P>(ICallback<T, T> callback, ICompare<T, P> compare, T value, P param);

        /// <summary>
        /// 交换两个元素的位置
        /// </summary>
        /// <param name="compare"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        void ExchangeValuePosition(ICompare<T, T> compare, T value1, T value2);
    }


    /// <summary>
    /// 链表
    /// </summary>
    /// <typeparam name="T">存储类型</typeparam>
    public class LinkList<T> : ILink<T>
    {
        readonly ILink<T> link;

        /// <summary>
        /// 构造函数。设置是否关闭缓存(true: 关闭缓存.false:不关闭缓存
        /// </summary>
        /// <param name="offCache">是否关闭缓存</param>
        public LinkList(bool offCache = false)
        {

            if (offCache)
            {
                link = new LinkArray<T>();
            }
            else
            {
                link = new CacheLink<T>();
            }
        }

        /// <summary>
        /// 链表长度
        /// </summary>
        public int Length()
        {
            return link.Length();
        }

        /// <summary>
        /// 把链表转换并返回数组
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            link.RestoreCursor();
            T[] array = new T[link.Length()];
            for (int i = 0; i < link.Length(); i++)
            {
                array[i] = link.Next();
            }
            return array;
        }

        public void AddHead(T value)
        {
            link.AddHead(value);
        }

        public void AddLast(T value)
        {
            link.AddLast(value);
        }

        public T GetHead()
        {
            return link.GetHead();
        }

        public T GetLast()
        {
            return link.GetLast();
        }

        public T Previous()
        {
            return link.Previous();
        }

        public T Next()
        {
            return link.Next();
        }
        public void RestoreCursor()
        {
            link.RestoreCursor();
        }



        public bool FindValue(T value)
        {
            return link.FindValue(value);
        }



        public T FindValue(IIterator<T> iterator)
        {
            return link.FindValue(iterator);
        }

        public virtual T FindValue<C>(ICompare<T, C> compare, C value)
        {
            return link.FindValue(compare, value);
        }

        public void Remove(int index)
        {
            link.Remove(index);
        }

        public void Remove(T value)
        {
            link.Remove(value);
        }

        public void RemoveAll()
        {
            link.RemoveAll();
        }

        public T Remove<P>(ICompare<T, P> compare, P param)
        {
            return link.Remove(compare, param);
        }

        public void Remove<P>(ICallback<T> call, ICompare<T, P> compare, P param)
        {
            link.Remove(call, compare, param);
        }

        public void RemoveAll(ICallback<T> call)
        {
            link.RemoveAll(call);
        }

        public T[] FindValueList<P>(ICompare<T, P> compare, P param)
        {
            return link.FindValueList(compare, param);
        }

        public T FindValue(int index)
        {
            return link.FindValue(index);
        }

        public void AddArray(T[] array)
        {
            link.AddArray(array);
        }

        public bool UpdateValue<P>(ICallback<T, T> callback, ICompare<T, P> compare, T value, P param)
        {
            return link.UpdateValue(callback, compare, value, param);
        }

        public void ExchangeValuePosition(ICompare<T, T> compare, T value1, T value2)
        {
            link.ExchangeValuePosition(compare, value1, value2);
        }

        public T ValuePrevious(ICompare<T, T> compare, T value)
        {
            return link.ValuePrevious(compare, value);
        }

        public T ValueNext(ICompare<T, T> compare, T value)
        {
            return link.ValueNext(compare, value);
        }
    }

    /// <summary>
    /// 链表数组
    /// </summary>
    public class LinkArray<T> : PresetLink<T>
    {
        protected override void DeleteNode(Node node)
        {
            if (node != null)
            {
                node.Previous = null;
                node.Next = null;
                node.Value = default;
            }
        }

        protected override Node GetNode(T value)
        {
            return new Node(value);
        }
    }

    /// <summary>
    /// 缓存链表
    /// </summary>
    public class CacheLink<T> : PresetLink<T>
    {
        Node _cache = null;
        protected override void DeleteNode(Node node)
        {
            // 确保 node 不为 null
            if (node == null) { return; }

            // 清空节点的所有引用
            node.Value = default;
            node.Previous = null;

            // 将 node 添加到缓存中
            Node temp = _cache;
            _cache = node;
            node.Next = temp;
        }

        protected override Node GetNode(T value)
        {
            if (_cache == null)
            {
                return new Node(value);
            }

            Node temp = _cache;
            _cache = temp.Next;

            // 清理 temp 的引用
            temp.Previous = null;
            temp.Next = null;
            temp.Value = value;

            return temp;
        }
    }


    /// <summary>
    /// 自定义链表公共属性和操作
    /// </summary>
    public abstract class PresetLink<T> : ILink<T>
    {
        /// <summary>
        /// 链表节点
        /// </summary>
        public class Node(T value)
        {
            /// <summary>
            /// 链表元素
            /// </summary>
            public T Value = value;
            /// <summary>
            /// 上一个结点
            /// </summary>
            public Node Previous;
            /// <summary>
            /// 下一个结点
            /// </summary>
            public Node Next;
        }

        /// <summary>
        /// 链表头结点
        /// </summary>
        protected Node _head;

        /// <summary>
        /// 链表尾结点
        /// </summary>
        protected Node _last;

        /// <summary>
        /// 游标
        /// </summary>
        protected Node _cursor;

        // 链表长度
        protected int _count = 0;

        /// <summary>
        /// 获取一个空节点
        /// </summary>
        protected abstract Node GetNode(T value);

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="node">需要删除的节点</param>
        /// <returns>T</returns>
        protected abstract void DeleteNode(Node node);

        public int Length() { return _count; }

        public virtual void AddHead(T value)
        {
            if (value == null)
            {
                throw new System.Exception($" -> :: {nameof(value)} 链表参数不能为空！！");
            }
            Node node = GetNode(value);
            if (_head == null)
            {
                _head = node;
                _last = node;
            }
            else
            {
                _head.Previous = node;
                node.Next = _head;
                _head = node;
            }
            _count++;
        }

        public virtual void AddLast(T value)
        {
            if (value == null)
            {
                throw new System.Exception($" -> :: {nameof(value)} 链表参数不能为空！！");
            }
            Node node = GetNode(value);
            if (_head == null)
            {
                _head = node;
                _last = node;
            }
            else
            {
                _last.Next = node;
                node.Previous = _last;
                _last = node;
            }
            _count++;
        }

        public void AddArray(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }
            foreach (var item in array)
            {
                AddLast(item);
            }
        }

        public virtual T GetHead()
        {
            return _head == null ? default : _head.Value;
        }

        public virtual T GetLast()
        {
            return _last == null ? default : _last.Value;
        }

        public virtual T Previous()
        {
            if (_cursor == null)
            {
                _cursor = _last;
            }
            else
            {
                _cursor = _cursor.Previous;
            }

            return _cursor is null ? default : _cursor.Value;
        }

        public virtual T Next()
        {
            if (_cursor == null)
            {
                _cursor = _head;
            }
            else
            {
                _cursor = _cursor.Next;
            }
            return _cursor is null ? default : _cursor.Value;
        }

        public void RestoreCursor()
        {
            _cursor = null;
        }

        public virtual bool FindValue(T value)
        {
            Node node = _head;
            while (node != null)
            {
                if (node.Value.Equals(value))
                {
                    return true;
                }
                node = node.Next;
            }
            return false;
        }

        public virtual T FindValue(IIterator<T> iterator)
        {
            Node node = _head;
            while (node != null)
            {
                if (iterator.Meet(node.Value))
                {
                    return node.Value;
                }
                node = node.Next;
            }
            return default;
        }

        public virtual T FindValue<P>(ICompare<T, P> compare, P param)
        {
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, param))
                {
                    return node.Value;
                }
                node = node.Next;
            }
            return default;
        }

        public T[] FindValueList<P>(ICompare<T, P> compare, P param)
        {
            System.Collections.Generic.List<T> list = [];
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, param))
                {
                    list.Add(node.Value);
                }
                node = node.Next;
            }
            return list.ToArray();
        }

        public virtual void Remove(T value)
        {
            if (value == null) return;
            Node node = _head;
            while (node != null)
            {
                if (node.Value.Equals(value))
                {
                    RemoveNode(node); return;
                }
                node = node.Next;
            }
        }

        public virtual void RemoveAll()
        {
            Node node = _head;
            while (node != null)
            {
                Node temp = node.Next;
                DeleteNode(node);
                node = temp;
            }
            _head = null;
            _last = null;
            _cursor = null;
            _count = 0;
        }

        public virtual T Remove<P>(ICompare<T, P> compare, P param)
        {
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, param))
                {
                    T temp = node.Value;
                    RemoveNode(node);
                    return temp;
                }
                node = node.Next;
            }
            return default;
        }

        public virtual void Remove<P>(ICallback<T> call, ICompare<T, P> compare, P param)
        {
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, param))
                {
                    T temp = node.Value;
                    RemoveNode(node);
                    call.CallMethod(temp);
                    return;
                }
                node = node.Next;
            }
        }

        public virtual void RemoveAll(ICallback<T> call)
        {
            Node node = _head;
            while (node != null)
            {
                T value = node.Value;
                Node temp = node.Next;
                DeleteNode(node);
                node = temp;
                call.CallMethod(value);
            }
            _head = null;
            _last = null;
            _cursor = null;
            _count = 0;
        }

        public T FindValue(int index)
        {
            if (index < 0 || index >= _count)
            {
                return default;
            }
            Node node = _head;
            int i = 0;
            while (node != null)
            {
                if (i == index)
                {
                    return node.Value;
                }
                i++;
                node = node.Next;

            }
            return default;
        }

        /// <summary>
        /// 统一摘链：维护 _head/_last/_cursor/_count 并回收节点
        /// </summary>
        void RemoveNode(Node node)
        {
            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            else
            {
                _head = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
            else
            {
                _last = node.Previous;
            }

            if (node == _cursor) { _cursor = node.Previous; }

            _count--;
            DeleteNode(node);
        }

        public void Remove(int index)
        {
            if (index < 0 || index >= _count) return;
            Node node = _head;
            for (int i = 0; i < index; i++) node = node.Next;
            RemoveNode(node);
        }

        public bool UpdateValue<P>(ICallback<T, T> callback, ICompare<T, P> compare, T value, P param)
        {
            if (value == null || param == null) return false;
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, param))
                {
                    callback.CallMethod(node.Value, value);
                    node.Value = value;
                    return true;
                }
                node = node.Next;
            }
            return false;
        }

        /// <summary>
        /// 查找元素节点
        /// </summary>
        Node FindNode(ICompare<T, T> compare, T value)
        {
            if (value == null) return null;
            Node node = _head;
            while (node != null)
            {
                if (compare.Compare(node.Value, value))
                {
                    return node;
                }
                node = node.Next;
            }
            return null;
        }

        public void ExchangeValuePosition(ICompare<T, T> compare, T value1, T value2)
        {
            Node a = FindNode(compare, value1);
            Node b = FindNode(compare, value2);

            // 如果节点不存在或相同，直接返回
            if (a == null || b == null || a == b) return;

            if (a.Next == b)
            {
                Node before = a.Previous;
                Node after = b.Next;

                if (before != null) before.Next = b;
                else _head = b;
                b.Previous = before;

                b.Next = a;
                a.Previous = b;

                a.Next = after;
                if (after != null) after.Previous = a;
                else _last = a;
                return;
            }
            if (b.Next == a)
            {
                Node before = b.Previous;
                Node after = a.Next;

                if (before != null) before.Next = a;
                else _head = a;
                a.Previous = before;

                a.Next = b;
                b.Previous = a;

                b.Next = after;
                if (after != null) after.Previous = b;
                else _last = b;
                return;
            }

            // 处理a和b的前后节点关系
            Node aPrev = a.Previous;
            Node aNext = a.Next;
            Node bPrev = b.Previous;
            Node bNext = b.Next;

            // 交换a和b的前后指针
            a.Previous = bPrev;
            a.Next = bNext;
            b.Previous = aPrev;
            b.Next = aNext;

            // 更新前驱节点的Next指针
            UpdateNodePointer(aPrev, b);
            UpdateNodePointer(bPrev, a);

            // 更新后继节点的Previous指针
            UpdateNodePointer(aNext, b, isPrevious: true);
            UpdateNodePointer(bNext, a, isPrevious: true);
        }

        /// <summary>
        /// ：更新节点的指针
        /// </summary>
        private void UpdateNodePointer(Node prevNode, Node newNext, bool isPrevious = false)
        {
            if (prevNode != null)
            {
                if (isPrevious)
                {
                    prevNode.Previous = newNext;
                }
                else
                {
                    prevNode.Next = newNext;
                }
            }
            else
            {
                if (isPrevious)
                {
                    _last = newNext;
                }
                else
                {
                    _head = newNext;
                }
            }
        }



        public T ValuePrevious(ICompare<T, T> compare, T value)
        {
            Node node = FindNode(compare, value);
            if (node != null)
            {
                if (node.Previous != null)
                {
                    return node.Previous.Value;
                }
            }
            return default;
        }

        public T ValueNext(ICompare<T, T> compare, T value)
        {
            Node node = FindNode(compare, value);
            if (node != null)
            {
                if (node.Next != null)
                {
                    return node.Next.Value;
                }
            }
            return default;
        }
    }

}