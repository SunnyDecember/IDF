using System;
using System.Collections.Generic;
using System.Threading;

namespace xuexue
{
    /// <summary>
    /// 一个线程同步队列
    /// </summary>
    public class DQueue<T>
    {
        /// <summary>
        /// 构造函数，参数是队列的最大长度。
        /// 如果调用EnqueueMaxLimit()当队列长度要超过的时候会自动丢弃最前端的内容。
        /// </summary>
        /// <param name="maxCapacity">队列的最大长度</param>
        public DQueue(int maxCapacity)
        {
            if (maxCapacity < 1024)
                this._queue = new Queue<T>(maxCapacity);//直接申请最大容量
            else
                this._queue = new Queue<T>(1024);//直接申请最大容量
            maxCount = maxCapacity;
        }

        /// <summary>
        /// 构造函数，参数是队列的最大长度，和初始长度。
        /// 如果调用EnqueueMaxLimit()当队列长度要超过的时候会自动丢弃最前端的内容。
        /// </summary>
        /// <param name="maxCapacity">队列的最大长度</param>
        /// <param name="initSize">初始分配长度</param>
        public DQueue(int maxCapacity, int initSize)
        {
            this._queue = new Queue<T>(initSize);
            maxCount = maxCapacity;
        }

        /// <summary>
        /// 数据队列,暴露出来使用方便扩展
        /// </summary>
        public Queue<T> _queue;

        /// <summary>
        /// 队列的最大长度
        /// </summary>
        private int _maxCount = int.MaxValue;

        /// <summary> 队列的最大数量. </summary>
        public int maxCount
        {
            get { return _maxCount; }
            set { _maxCount = value; }
        }

        /// <summary>
        /// 队列的当前数据个数
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// 队列是否已经满了
        /// </summary>
        public bool IsFull
        {
            get
            {
                if (_queue.Count >= maxCount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 移除并返回位于 Queue 开始处的对象，如果没有那么返回default(T)。
        /// </summary>
        /// <returns>返回的条目</returns>
        public T Dequeue()
        {
            lock (this._queue)
            {
                if (this._queue.Count > 0)
                {
                    return this._queue.Dequeue();
                }
                else
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Peek函数一般在使用的时候含义有所不同，得外面自己加锁.所以这个函数本身没有加锁。
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            if (this._queue.Count > 0)
            {
                return this._queue.Peek();
            }
            else
            {
                return default(T);
            }       
        }

        /// <summary>
        /// 将对象添加到 Queue的结尾处。
        /// </summary>
        /// <param name="item">加入的条目</param>
        public void Enqueue(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items null");
            }
            lock (this._queue)
            {
                this._queue.Enqueue(item);
            }
        }

        /// <summary>
        /// 如果队列达到了限定长度，就自动丢弃最前端的。
        /// 如果正常返回true,丢弃返回false.
        /// </summary>
        /// <param name="item">加入的条目</param>
        /// <returns>如果正常返回true,丢弃返回false</returns>
        public bool EnqueueMaxLimit(T item)
        {
            bool isDiscard = false;
            if (item == null)
            {
                throw new ArgumentNullException("DQueue.EnqueueMaxLimit():输入参数为null"); //注意其实下面的队列支持null
            }

            lock (this._queue)
            {
                if (_queue.Count < maxCount)
                {
                    _queue.Enqueue(item);
                }
                else
                {
                    _queue.Dequeue();
                    _queue.Enqueue(item);
                    isDiscard = true;
                }
            }
            return !isDiscard;
        }

        /// <summary>
        /// 如果队列达到了限定长度，就自动丢弃最前端的。
        /// 如果正常返回true,丢弃返回false.
        /// </summary>
        /// <param name="item">加入的条目</param>
        /// <param name="dequeueItem">最前端的</param>
        /// <returns>如果正常返回true,丢弃返回false</returns>
        public bool EnqueueMaxLimit(T item, out T dequeueItem)
        {
            bool isDiscard = false;
            dequeueItem = default(T);
            if (item == null)
            {
                throw new ArgumentNullException("DQueue.EnqueueMaxLimit():输入参数为null"); //注意其实下面的队列支持null
            }

            lock (this._queue)
            {
                if (_queue.Count < maxCount)
                {
                    _queue.Enqueue(item);
                }
                else
                {
                    dequeueItem = _queue.Dequeue();
                    _queue.Enqueue(item);
                    isDiscard = true;
                }
            }
            return !isDiscard;
        }

        /// <summary>
        /// 返回当前队列的整个数据拷贝,不会清空队列
        /// </summary>
        /// <returns>队列的数组拷贝</returns>
        public T[] ToArray()
        {
            lock (this._queue)
            {
                return _queue.ToArray();
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (this._queue)
            {
                this._queue.Clear();
            }
        }

        /// <summary>
        /// 尝试一次取出整个队列的所有数据,如果为空返回null
        /// </summary>
        /// <returns>队列的数据数组</returns>
        public T[] TryGetData()
        {
            if (_queue.Count == 0)
            {
                return null;
            }

            if (Monitor.TryEnter(this._queue))
            {
                T[] data = _queue.ToArray();
                this._queue.Clear();
                Monitor.Exit(this._queue);

                return data;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试一次取出整个队列的所有数据,尝试把结果写到output数组中去,返回成功取出的条数。
        /// </summary>
        /// <param name="output">缓存地址</param>
        /// <param name="offset">output中的偏移</param>
        /// <returns>成功取出的条数</returns>
        public int TryGetData(T[] output, int offset)
        {
            if (_queue.Count == 0)
            {
                return 0;
            }

            if (Monitor.TryEnter(this._queue))
            {
                int curIndex = 0;
                while (offset + curIndex < output.Length)
                {
                    if (_queue.Count > 0)
                        output[offset + curIndex] = _queue.Dequeue();
                    else
                        break;
                    curIndex++;
                }

                Monitor.Exit(this._queue);

                return curIndex;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 尝试一次取出整个队列的所有数据,尝试把结果写到output数组中去,返回成功取出的条数。
        /// </summary>
        /// <param name="output">缓存地址</param>
        /// <param name="offset">output中的偏移</param>
        /// <returns>成功取出的条数</returns>
        public int GetData(T[] output, int offset)
        {
            lock (this._queue)
            {
                if (_queue.Count == 0)
                {
                    return 0;
                }

                int curIndex = 0;
                while (offset + curIndex < output.Length)
                {
                    if (_queue.Count > 0)
                        output[offset + curIndex] = _queue.Dequeue();
                    else
                        break;
                    curIndex++;
                }
                return curIndex;
            }
        }

        /// <summary>
        /// 一次取出整个队列的所有数据,如果为空返回null
        /// </summary>
        /// <returns>队列的数据数组</returns>
        public T[] GetData()
        {
            lock (this._queue)
            {
                if (_queue.Count == 0)
                {
                    return null;
                }

                T[] data = _queue.ToArray();
                this._queue.Clear();
                return data;
            }
        }

        /// <summary>
        /// 如果元素数小于当前容量的 90%，将容量设置为队列中的实际元素数。
        /// </summary>
        public void TrimExcess()
        {
            lock (this._queue)
            {
                this._queue.TrimExcess();
            }
        }

        /// <summary>
        /// 进入锁
        /// </summary>
        public void LockEnter()
        {
            Monitor.Enter(this._queue);
        }

        /// <summary>
        /// 离开锁
        /// </summary>
        public void LockExit()
        {
            Monitor.Exit(this._queue);
        }
    }
}