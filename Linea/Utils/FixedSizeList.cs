using System;
using System.Collections;
using System.Collections.Generic;

namespace Linea.Utils
{
    public class FixedSizeList<T> : IList<T>
    {
        private T?[] _array;
        private uint _head; // Points to the oldest element
        private uint _count;
        private uint _capacity;

        public event Action<int, T>? ItemDiscarding;
        public event Action<T>? ItemDiscarded;

        public FixedSizeList(uint capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            _array = new T[capacity];
            _capacity = capacity;
            _head = 0;
            _count = 0;
        }

        public int Count => (int)_count;

        public uint Capacity
        {
            get => _capacity;
            set
            {
                if (value == _capacity) return;
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than zero.");
                uint OldCapacity = _capacity;
                uint NewCapacity = value;

                if (NewCapacity < OldCapacity)
                {
                    //shrinking, we keep the array as is

                    //do we need to clear elements?
                    if (_count > NewCapacity)
                    {
                        uint slotsToClear = _count - NewCapacity;
                        _count = NewCapacity;
                        //Design choise: we clear the oldest elements
                        DiscardFrom(0, (uint)slotsToClear);
                        _head = (uint)ToValidIndex_Array(_head + slotsToClear); //move the head forward
                    }
                }
                else
                {
                    //Growing Capacity
                    if (NewCapacity > _array.Length)
                    {
                        T[] newArray = new T[NewCapacity * 2]; //let's give ourselves a little extra space
                        for (int i = 0; i < _count; i++)
                        {
                            newArray[i] = this[i];
                        }
                        _array = newArray;
                        _head = 0;
                    }
                }

                _capacity = value;
            }
        }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            Insert(_count, item);
        }
        public T GetFirst() => this[0];
        public T GetLast() => this[_count - 1];
        public T RemoveFromHead()
        {
            if (_count == 0)
                throw new InvalidOperationException("Collection is empty.");

            T item = this[0];
            _array[_head] = default(T); // Clear the slot
            if (_count > 1)
            {
                _head = ToValidUIndex_Array(_head + 1);
            }
            _count--;
            return item;
        }

        public T RemoveFromTail()
        {
            if (_count == 0)
                throw new InvalidOperationException("Collection is empty.");
            T item = this[_count - 1];
            _count--;
            ClearAfterCount(1);
            return item;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => RemoveAt(NN(index));
        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(uint index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            ShiftLeft(index + 1, 1);
        }

        public void Clear() => Clear(false);
        private void Clear(bool invokDiscard)
        {
            if (_count == 0) return; // Nothing to clear
            if (invokDiscard)
            {
                for (uint i = 0; i < _array.Length; i++)
                {
                    Discard(i);
                }
            }
            else
            {
                Array.Clear(_array, 0, _array.Length);
            }
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert. The value can be null for reference types.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is less than 0. -or- index is greater than <see cref="Count"/>.</exception>
        public void Insert(int index, T item) => Insert(NN(index), item);


        private static uint NN(int Index) => Index >= 0 ? (uint)Index : throw new ArgumentException("Index must be positive");


        public void Insert(uint index, T item)
        {
            if (index < 0 || index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == _count)
            {
                if (_count == _capacity)
                {
                    T? toDiscard = _array[_head];
                    if (toDiscard != null)
                    {
                        ItemDiscarding?.Invoke(0, toDiscard); // Invoke the discarding event      
                    }

                    _array[_head] = item;

                    if (toDiscard != null)
                    {
                        ItemDiscarded?.Invoke(toDiscard); // Invoke the discard event      
                    }

                    uint newHeadLogicalSlot = ToValidUIndex(1, Capacity);
                    _head = ToValidUIndex(newHeadLogicalSlot, _array.Length);
                }
                else
                {
                    int newItemSlot = ToValidIndex_Array(_head + _count);
                    _array[newItemSlot] = item;
                    _count++;
                }
            }
            else
            {
                uint newIndex = ShiftRight(index, 1);
                _array[ToValidIndex_Array(_head + newIndex)] = item;
            }
        }

        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(this[i], item))
                    return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < _count)
                throw new ArgumentException("The destination array has insufficient space.");

            for (int i = 0; i < _count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///  Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set</param>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is less than 0. -or- index is equal to or greater than <see cref="Count"/>  </exception>
        public T this[int index]
        {
            get => this[index >= 0 ? (uint)index : throw new ArgumentException("Index must be positive")];
            set => this[index >= 0 ? (uint)index : throw new ArgumentException("Index must be positive")] = value;
        }

        /// <summary>
        ///  Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set</param>
        /// <returns>The element at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is less than 0. -or- index is equal to or greater than <see cref="Count"/>  </exception>
        public T this[uint index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _array[ToValidIndex_Array(_head + index)]!;
            }
            set
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _array[ToValidIndex_Array(_head + index)] = value;
            }
        }

        private int ToValidIndex_Array(uint i) => Ext.ToValidIndex((int)i, _array.Length);
        private int ToValidIndex_Array(int i) => Ext.ToValidIndex(i, _array.Length);
        private uint ToValidUIndex_Array(uint i) => ToValidUIndex(i, _array.Length);
        private uint ToValidUIndex_Array(int i) => (uint)Ext.ToValidIndex(i, _array.Length);
        internal static uint ToValidUIndex(uint i, uint len) => ((i + len) % len);
        internal static uint ToValidUIndex(uint i, int len)
        {
            return ToValidUIndex(i, NN(len));
        }


        // ICollection<T> Remove removes the first occurrence (oldest) if it matches
        public bool Remove(T item)
        {
            if (_count == 0)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (uint logicalIndex = 0; logicalIndex < _count; logicalIndex++)
            {
                T elem = this[logicalIndex];
                if (comparer.Equals(elem, item))
                {
                    ShiftLeft(logicalIndex + 1, 1);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shifts elements in the list to the left by the specified amount, starting at the given logical index.
        /// The element at <c>from_logicalIndex - 1</c> will be overwritten by the element at <c>from_logicalIndex</c>, and so on.
        /// This is typically used to remove an element at logical index <c>i</c> by calling <c>ShiftLeft(i + 1, 1)</c>.
        /// </summary>
        /// <param name="from_logicalIndex">
        /// The logical index of the first element to be moved left. 
        /// All elements from this index up to the end of the list will be shifted left by <paramref name="requestedShiftAmount"/>.
        /// The element to be removed is at <c>from_logicalIndex - 1</c>.
        /// </param>
        /// <param name="requestedShiftAmount">
        /// The number of positions to shift elements left.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="from_logicalIndex"/> or <paramref name="requestedShiftAmount"/> are out of range.
        /// </exception>
        private void ShiftLeft(uint from_logicalIndex, uint requestedShiftAmount)
        {
            if (requestedShiftAmount == 0) return;
            if (from_logicalIndex < 0 || from_logicalIndex > _count)
                throw new ArgumentOutOfRangeException(nameof(from_logicalIndex));

            requestedShiftAmount = Math.Min((uint)_count, requestedShiftAmount);
            if (from_logicalIndex == _count)
            {
                //special case, we are asked to shift a few empty slots from the end 
                _count -= requestedShiftAmount;
                ClearAfterCount(requestedShiftAmount);
                return;
            }

            if (requestedShiftAmount == _count)
            {
                Clear();
            }
            else if (requestedShiftAmount > from_logicalIndex)
            {
                ClearFrom(0, requestedShiftAmount);
                _head = ToValidUIndex_Array((_head + requestedShiftAmount));
                _count -= requestedShiftAmount;
            }
            else
            {
                uint spanSize = _count - from_logicalIndex;
                for (uint i = 0; i < spanSize; i++)
                {
                    uint from_arrayIndex = ToValidUIndex(_head + from_logicalIndex + i, _array.Length);
                    uint to_arrayIndex = ToValidUIndex(from_arrayIndex - (requestedShiftAmount), _array.Length);

                    _array[to_arrayIndex] = _array[from_arrayIndex];
                }
                _count -= requestedShiftAmount;
                ClearAfterCount(requestedShiftAmount);
            }
        }
        private int ArrayIndexToLogicalIndex(uint arrayIndex)
        {
            return ToValidIndex_Array(arrayIndex - _head);
        }

        private void Discard(uint arrayIndex)
        {
            if (_array[arrayIndex] is not null)
            {
                var oldItem = _array[arrayIndex]!;

                ItemDiscarding?.Invoke(ArrayIndexToLogicalIndex(arrayIndex), oldItem);

                _array[arrayIndex] = default(T); // Clear the slot

                ItemDiscarded?.Invoke(oldItem);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from_logicalIndex">The logical index of the first element to be shifted right</param>
        /// <param name="signedCount">the shift amount</param>
        /// <returns>returns the new "from" logical position if the head moved during the shift</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private uint ShiftRight(uint from_logicalIndex, uint requestedShiftAmount)
        {
            if (requestedShiftAmount == 0) return from_logicalIndex;
            if (from_logicalIndex >= _count)
                throw new ArgumentOutOfRangeException(nameof(from_logicalIndex));


            uint freeSpace = Capacity - _count;
            uint freeableSpace = from_logicalIndex;
            uint span_size = _count - from_logicalIndex;

            if (requestedShiftAmount >= Capacity)
            {
                Clear(true);
                return 0;
            }
            else if (requestedShiftAmount > freeSpace + freeableSpace)
            {
                uint elementsToSacrifice = (requestedShiftAmount) - freeSpace - freeableSpace;
                uint CurrentFrom_ArrayLocation = ToValidUIndex(_head + from_logicalIndex + elementsToSacrifice, Capacity);
                _count = span_size - elementsToSacrifice;
                DiscardAfterCount();
                return _count;
            }
            else if (requestedShiftAmount > freeSpace)
            {
                uint advanceHead = (requestedShiftAmount) - freeSpace;

                for (uint i = 0; i < advanceHead; i++)
                {
                    Discard(ToValidUIndex(_head + i, _array.Length));
                }

                _head += advanceHead;
                from_logicalIndex -= advanceHead;
                _count -= advanceHead;
            }

            for (int i = (int)(span_size - 1); i >= 0; i--)
            {

                uint from_arrayIndex = ToValidUIndex(_head + from_logicalIndex + (uint)i, _array.Length);
                uint to_arrayIndex = ToValidUIndex(from_arrayIndex + requestedShiftAmount, _array.Length);

                _array[to_arrayIndex] = _array[from_arrayIndex];
                _array[from_arrayIndex] = default(T); // Clear the slot we just moved from
            }
            _count += requestedShiftAmount;

            return from_logicalIndex;
        }


        /// <summary>
        /// Does not update the count
        /// <para />
        /// Does not call the discard events
        /// </summary>
        /// <param name="amount"></param>
        private void ClearAfterCount(uint? amount = null) => ClearFrom(_count, amount ?? (uint)(_capacity - _count));

        /// <summary>
        /// Does not update the count
        /// <para />
        /// Does not call the discard events
        /// </summary>
        /// <param name="FirstLogicalSlot"></param>
        /// <param name="amount"></param>
        private void ClearFrom(uint FirstLogicalSlot, uint amount)
        {
            uint FirstArraySlot = ToValidUIndex_Array(_head + FirstLogicalSlot);
            for (uint j = 0; j < amount; j++)
            {
                uint indexToClear = ToValidUIndex_Array(FirstArraySlot + j);
                _array[indexToClear] = default(T); // Clear the slot
            }
        }


        /// <summary>
        /// Does not update the count
        /// <para />
        /// Calls the discard events
        /// </summary>
        /// <param name="amount"></param>
        private void DiscardAfterCount(uint? amount = null) => DiscardFrom(_count, amount ?? (uint)(_capacity - _count));

        /// <summary>
        /// does not update the count
        /// <para />
        /// Calls the discard events
        /// </summary>
        /// <param name="FirstLogicalSlot"></param>
        /// <param name="amount"></param>
        private void DiscardFrom(uint FirstLogicalSlot, uint amount)
        {
            uint FirstArraySlot = ToValidUIndex_Array(_head + FirstLogicalSlot);
            for (uint j = 0; j < amount; j++)
            {
                uint indexToClear = ToValidUIndex_Array(FirstArraySlot + j);
                Discard(indexToClear);
            }
        }

        public int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                var elem = this[i];
                if (comparer.Equals(elem, item))
                {
                    return i;
                }
            }
            return -1;
        }

    }
}
