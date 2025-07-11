using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Linea.Interface
{
    public partial class ConsoleMechanics
    {

        private abstract class ConsoleMechanicsGenericList<T> : IList<T>, IList, INotifyCollectionChanged
        {
            protected readonly ConsoleMechanics _mech;

            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            public ConsoleMechanicsGenericList(ConsoleMechanics cm)
            {
                _mech = cm;
                _mech.ConsoleRowCountChanged += (sender, e) =>
                {
                    CollectionChanged?
                    .Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                };
                _mech.ContentChanged += _mech_ContentChanged;
            }

            private void _mech_ContentChanged(object sender, ConsoleContentChangeArgs e)
            {
                IList changedItems = new List<string>();
                for (int i = e.FirstRow; i <= e.LastRow; i++)
                {
                    changedItems.Add(this._mech.GetRowText(i));
                }

                CollectionChanged?
                .Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
                );
            }

            public abstract T this[int index] { get; set; }

            public int Count => _mech.RowCount;
            public bool IsReadOnly => true;

            bool IList.IsFixedSize => false;

            bool IList.IsReadOnly => true;

            int ICollection.Count => this.Count;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => this;

            public void Add(T item) { }
            public void Clear() => _mech.Clear();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex)
            {
                for (int i = 0; i < _mech.RowCount; i++)
                {
                    array[arrayIndex + i] = this[i];
                }
            }

            public abstract IEnumerator<T> GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int IndexOf(T item) => -1;
            public void Insert(int index, T item) { }
            public bool Remove(T item) => false;
            public void RemoveAt(int index) { }

            #region
            object IList.this[int index]
            {
                get => this[index]!;
                set { }
            }
            int IList.Add(object value) => -1;
            void IList.Clear() { }
            bool IList.Contains(object value) => false;
            int IList.IndexOf(object value) => -1;
            void IList.Insert(int index, object value) { }
            void IList.Remove(object value) { }
            void IList.RemoveAt(int index) { }
            #endregion

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                for (int i = 0; i < _mech.RowCount; i++)
                {
                    array.SetValue(this[i], arrayIndex + i);
                }
            }
        }

        private abstract class ConsoleMechanicsGenericEnumerator<T> : IEnumerator<T>
        {
            protected readonly ConsoleMechanics _mech;
            private int _index;
            private T? _current;

            public ConsoleMechanicsGenericEnumerator(ConsoleMechanics mech)
            {
                _mech = mech;
                _index = -1;
                _current = default;
            }

            public T Current => _current ?? throw new InvalidOperationException();

            object IEnumerator.Current => _current ?? throw new InvalidOperationException();

            public bool MoveNext()
            {
                if (++_index >= _mech.RowCount)
                    return false;
                _current = GetCurrent(_index);
                return true;
            }
            protected abstract T GetCurrent(int index);
            public void Reset()
            {
                _index = -1;
                _current = default;
            }

            public void Dispose()
            {
                // Nessuna risorsa da liberare
            }
        }

        private class ConsoleMechanicsStringList : ConsoleMechanicsGenericList<string>
        {
            public ConsoleMechanicsStringList(ConsoleMechanics cm) : base(cm)
            {
            }

            public override string this[int index] { get => _mech.GetRowText(index); set { } }

            public override IEnumerator<string> GetEnumerator()
            {
                return new ConsoleMechanicsStringEnumerator(_mech);
            }
        }
        private class ConsoleMechanicsStringEnumerator : ConsoleMechanicsGenericEnumerator<string>
        {
            public ConsoleMechanicsStringEnumerator(ConsoleMechanics cm) : base(cm) { }

            protected override string GetCurrent(int index) => _mech.GetRowText(index);

        }
    }
}
