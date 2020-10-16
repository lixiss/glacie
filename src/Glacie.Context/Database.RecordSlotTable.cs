using System;
using Glacie.Infrastructure;

namespace Glacie
{
    // TODO: (Gx) RecordSlotTable, version, enumeration?

    partial class Database
    {
        private struct RecordSlotTable
        {
            private const int DefaultCapacity = 4;

            private RecordSlot[] _items;
            private int _count;
            // private int _version;

            public RecordSlotTable(int capacity)
            {
                if (capacity < 0) throw Error.Argument(nameof(capacity));

                _items = new RecordSlot[capacity];
                _count = 0;
            }

            public int Count => _count;

            public int Capacity
            {
                get => _items.Length;
                set
                {
                    if (value < _count) throw Error.ArgumentOutOfRange(nameof(value));

                    if (value != _items.Length)
                    {
                        if (value > 0)
                        {
                            RecordSlot[] newItems = new RecordSlot[value];
                            if (_count > 0)
                            {
                                Array.Copy(_items, newItems, _count);
                            }
                            _items = newItems;
                        }
                        else
                        {
                            _items = Array.Empty<RecordSlot>();
                        }
                    }
                }
            }

            public ref RecordSlot this[gx_record_id index]
            {
                get
                {
                    if ((uint)index >= (uint)_count)
                    {
                        throw Error.ArgumentOutOfRange(nameof(index));
                    }
                    return ref _items[(int)index];
                }
            }

            public gx_record_id Add(in RecordSlot item)
            {
                // _version++;
                RecordSlot[] array = _items;
                int count = _count;
                if ((uint)count < (uint)array.Length)
                {
                    _count = count + 1;
                    array[count] = item;
                    return (gx_record_id)count;
                }
                else
                {
                    return AddWithResize(item);
                }
            }

            private gx_record_id AddWithResize(in RecordSlot item)
            {
                int count = _count;
                EnsureCapacity(count + 1);
                _count = count + 1;
                _items[count] = item;
                return (gx_record_id)count;
            }

            private void EnsureCapacity(int min)
            {
                if (_items.Length < min)
                {
                    int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
                    if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
                    if (newCapacity < min) newCapacity = min;
                    Capacity = newCapacity;
                }
            }
        }
    }
}
