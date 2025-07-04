using System.Collections;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace Linea.WPF
{
    internal class DispatchedList : IList, INotifyCollectionChanged
    {
        private readonly IList _list;
        private readonly Dispatcher _dispatcher;

        public DispatchedList(object list, Dispatcher dispatcher)
        {
            _list = (list as IList) ?? throw new ArgumentNullException(nameof(list));
            INotifyCollectionChanged notify = ((INotifyCollectionChanged)_list);
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            notify.CollectionChanged += Notify_CollectionChanged;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        private void Notify_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_dispatcher.CheckAccess())
            {
                CollectionChanged?.Invoke(this, e);
            }
            else
            {
                _dispatcher.Invoke(() => CollectionChanged?.Invoke(this, e));
            }
        }

        #region IList forwarding
        public int Add(object value) => _list.Add(value);
        public void Clear() => _list.Clear();
        public bool Contains(object value) => _list.Contains(value);
        public int IndexOf(object value) => _list.IndexOf(value);
        public void Insert(int index, object value) => _list.Insert(index, value);
        public bool IsFixedSize => _list.IsFixedSize;
        public bool IsReadOnly => _list.IsReadOnly;
        public void Remove(object value) => _list.Remove(value);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        public object this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
        #endregion

        #region ICollection forwarding
        public void CopyTo(Array array, int index) => _list.CopyTo(array, index);
        public int Count => _list.Count;
        public bool IsSynchronized => _list.IsSynchronized;
        public object SyncRoot => _list.SyncRoot;
        #endregion

        #region IEnumerable forwarding
        public IEnumerator GetEnumerator() => _list.GetEnumerator();
        #endregion
    }
}
