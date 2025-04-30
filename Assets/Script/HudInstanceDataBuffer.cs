using System.Collections.Generic;
using UnityEngine;

namespace ST.HUD
{
    public struct HudInstanceData
    {
        public int visible;
        public Vector3 position;
        public float progress;
    }

    public class HudInstanceDataBuffer
    {
        private static HudInstanceDataBuffer instance = default;
        public static HudInstanceDataBuffer Instance => instance;

        private readonly Stack<int> _freeIndices;
        private readonly HudInstanceData[] _buffer;

        private int _count;
        private readonly int _capacity;
        private bool _dirty;
        
        public int Count => _count;

        public HudInstanceDataBuffer(int capacity)
        {
            _capacity = capacity;
            _freeIndices = new Stack<int>(capacity / 10);   // 10%的空间用来存储空闲索引
            _buffer = new HudInstanceData[capacity];
            _count = 0;
            _dirty = false;
            instance = this;
        }
        
        public int Insert(HudInstanceData item)
        {
            if (_freeIndices.TryPop(out var index))
            {
                item.visible = 1;
                _buffer[index] = item;
                _dirty = true;
                return index;
            }
            if (_capacity > _count)
            {
                index = _count;
                _count++;
                item.visible = 1;
                _buffer[index] = item;
                _dirty = true;
                return index;
            }
            return -1;
        }
        
        public void Remove(int index)
        {
            if (index < 0 || index >= _capacity) return;
            _buffer[index].visible = 0;
            _freeIndices.Push(index);
            _dirty = true;
        }
        
        public void TryAppendData(ComputeBuffer buffer)
        {
            if (_dirty)
            {
                buffer.SetData(_buffer, 0, 0, _count);
                _dirty = false;
            }
        }
    }

}