// MIT License
// This implementation based on System.Collections.Generic.List<T>
// https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs
// https://raw.githubusercontent.com/dotnet/runtime/e7204f5d6fcaca5e097ec854b3be6055229fc442/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Features
//#define FEATURE_REFERENCEVALUELIST_HASVERSION

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Glacie.Utilities;

namespace Glacie.Collections
{
    /// <summary>
    /// This <see langword="struct"/> similar to <see cref="System.Collections.Generic.List{T}"/>,
    /// but it is value-type. This intended only to use as private member in some <see langword="class"/>.
    /// This type doesn't implement any interfaces, to prevent unintended boxing.
    /// </summary>
    public struct ReferenceValueList<T>
    {
        private const int DefaultCapacity = 4;

        // See: https://raw.githubusercontent.com/dotnet/runtime/e7204f5d6fcaca5e097ec854b3be6055229fc442/src/libraries/System.Private.CoreLib/src/System/Array.cs
        private const int MaxArrayLength = 0X7FEFFFFF;

        private static readonly T[] s_emptyArray = new T[0];

        private T[] _items;
        private int _count;
#if FEATURE_REFERENCEVALUELIST_HASVERSION
        private int _version;
#endif

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        public ReferenceValueList(int capacity)
        {
            if (capacity < 0) throw Error.ArgumentOutOfRange(nameof(capacity)); // ArgumentOutOfRange_NeedNonNegNum

            if (capacity == 0)
                _items = s_emptyArray;
            else
                _items = ArrayUtilities.AllocateUninitializedArray<T>(capacity);
            _count = 0;
#if FEATURE_REFERENCEVALUELIST_HASVERSION
            _version = 0;
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _items = null!;
            _count = 0;
        }

        public bool Allocated => _items != null;

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal
        // array of the list is reallocated to the given capacity.
        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _count)
                {
                    throw Error.ArgumentOutOfRange(nameof(value)); // ArgumentOutOfRange_SmallCapacity
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = ArrayUtilities.AllocateUninitializedArray<T>(value);
                        if (_count > 0)
                        {
                            Array.Copy(_items, newItems, _count);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        _items = s_emptyArray;
                    }
                }
            }
        }

        public int Count => _count;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_count)
                {
                    ThrowArgumentOutOfRangeIndex();
                }
                return ref _items[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(in T item)
        {
#if FEATURE_REFERENCEVALUELIST_HASVERSION
            _version++;
#endif
            T[] array = _items;
            int count = _count;
            if ((uint)count < (uint)array.Length)
            {
                _count = count + 1;
                array[count] = item;
                return count;
            }
            else
            {
                return AddWithResize(in item);
            }
        }

        // Non-inline from List.Add to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int AddWithResize(in T item)
        {
            int count = _count;
            EnsureCapacity(count + 1);
            _count = count + 1;
            _items[count] = item;
            return count;
        }

        // Clears the contents of List.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
#if FEATURE_REFERENCEVALUELIST_HASVERSION
            _version++;
#endif
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                int count = _count;
                _count = 0;
                if (count > 0)
                {
                    Array.Clear(_items, 0, count); // Clear the elements so that the gc can reclaim the references.
                }
            }
            else
            {
                _count = 0;
            }
        }

        public Memory<T> AsMemory()
        {
            return new Memory<T>(_items, 0, _count);
        }

        public Memory<T> AsMemory(int index, int count)
        {
            if (index < 0) ThrowArgumentOutOfRangeIndex();
            if (count < 0) ThrowArgumentOutOfRangeCount();
            if (_count - index < count) ThrowArgumentInvalidOffsetOrLength();
            return new Memory<T>(_items, index, count);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the current capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        [DoesNotReturn]
        private void ThrowArgumentOutOfRangeIndex()
        {
            // TODO: Check code generation for this[int index]. This method intended to be static,
            // but we want throw ObjectDisposed or not initialized.
            // Generally without null check we just end with NullReferenceException which is also okay.
            if (_items == null)
            {
                throw Error.ObjectDisposed(GetType().ToString(), "Object disposed or not initialized.");
            }
            else throw Error.ArgumentOutOfRange("index");
        }

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRangeCount()
        {
            throw Error.ArgumentOutOfRange("count");
        }

        [DoesNotReturn]
        private static void ThrowArgumentInvalidOffsetOrLength()
        {
            // ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            throw new ArgumentException("Invalid offset or length.");
        }
    }
}
