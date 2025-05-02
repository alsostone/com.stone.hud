using System.Collections.Generic;
using UnityEngine;

namespace ST.HUD
{
    public struct HudInstanceData
    {
        public int Visible;
        public Vector3 Position;
        public int NameIndex;
        public float Progress;
    }

    internal class HudInstanceDataBuffer
    {
        public int Count => _count - _freeIndices.Count;
        public HudInstanceData this[int index] => _buffer[index];

        private readonly Stack<int> _freeIndices;
        private readonly HudInstanceData[] _buffer;
        private int _count;
        private readonly int _capacity;
        private bool _dirty;

        internal HudInstanceDataBuffer(int capacity)
        {
            _capacity = capacity;
            _freeIndices = new Stack<int>(capacity / 10);   // 10%的空间用来存储空闲索引
            _buffer = new HudInstanceData[capacity];
            _count = 0;
            _dirty = false;
        }
        
        internal int Insert(HudInstanceData item)
        {
            if (_freeIndices.TryPop(out var index))
            {
                _buffer[index] = item;
                _dirty = true;
                return index;
            }
            if (_capacity > _count)
            {
                index = _count;
                _count++;
                _buffer[index] = item;
                _dirty = true;
                return index;
            }
            return -1;
        }
        
        internal void Remove(int index)
        {
            if (index < 0 || index >= _capacity) return;
            _buffer[index].Visible = 0;
            _freeIndices.Push(index);
            _dirty = true;
        }
        
        internal void TryAppendData(ComputeBuffer buffer)
        {
            if (_dirty)
            {
                buffer.SetData(_buffer, 0, 0, _count);
                _dirty = false;
            }
        }
    }

}