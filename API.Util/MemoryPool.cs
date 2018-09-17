﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace API.Util
{
    public sealed class MemoryPool<T> : IDisposable where T : IDisposable, new()
    {
        private int _count;
        private Queue<T> _pool;
        private readonly object _append, _read;
        private Func<T> _createT;
        private bool _disposed;
        public MemoryPool(int count = 100, Func<T> func = null, bool autoCreate = true)
        {
            _append = new object();
            _read = new object();
            _pool = new Queue<T>(count);
            _count = count;
            if (func == null) _createT = () => new T();
            else _createT = func;
            if (autoCreate)
            {
                for (int i = 0; i < _count; i++)
                {
                    _pool.Enqueue(_createT());
                }
            }
        }
        public void Init(int count, Func<T> func)
        {
            _count = count;
            _createT = func;
            if (func == null) _createT = () => new T();
            else _createT = func;
            for (int i = 0; i < _count; i++)
            {
                _pool.Enqueue(_createT());
            }
        }
        public T Pop()
        {
            try
            {
                Monitor.Enter(_read);
                if (_pool.Count < 1)
                {
                    return _createT();
                }
                return _pool.Dequeue();
            }
            finally
            {
                Monitor.Exit(_read);
            }
        }
        public void Push(T data)
        {
            try
            {
                Monitor.Enter(_append);
                if (_pool.Count < _count)
                {
                    _pool.Enqueue(data);
                }
                else
                {
                    data.Dispose();
                    data = default(T);
                }
            }
            finally
            {
                Monitor.Exit(_append);
            }
        }
        private void Dispose(bool isDispose)
        {
            if(Monitor.TryEnter(this))
            {
                try
                {
                    _disposed = isDispose;
                    for (int i = 0; i < _pool.Count; i++)
                    {
                        var data = _pool.Dequeue();
                        data.Dispose();
                        data = default(T);
                    }
                    _pool.Clear();
                    _pool = null;
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
        }
        public void Dispose()
        {
            if (_disposed)
                return;
            Dispose(true);
        }
        public int CurrentCount { get => _pool.Count; }
        public int Count { get => _count; }
    }
}
