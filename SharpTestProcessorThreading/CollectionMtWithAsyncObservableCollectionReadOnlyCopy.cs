using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Automation.Peers;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace HQ.Util.General.Collections
{
	public delegate void OnMtCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs args);

	/// <summary>
	/// EO: New to replace the "ObservableCollectionMt", should be more stable and easier to use.
	/// Asynchronous MultiThread ObsColl. No 100% compatible with standard ObsColl but very close to it.
	/// Removed functionnalities are clearly stated.
	/// 
	/// There is a major drawback into using that class. You will duplicate every collection entry because every 
	/// item is added to a list which will eventually be duplicate to an observableCollection (beginInvoke).
	/// 
	/// This class seems to me very stable and very efficient. But you should not modify directly the inner ObservableCollection, 
	/// or do it at your own risk if you ever do not want to pass throught that class directly to access the collection.
	/// 
	/// Usage: 
	/// To bind to this collection to be advised you should no process as usual like a real ObservableCollection.
	/// You should bind to the inner ObservableCollection that you can obtain throught the ObsColl property.
	/// 
	/// </summary>
	/// <typeparam name="T">Item type</typeparam>
	public class CollectionMtWithAsyncObservableCollectionReadOnlyCopy<T> : ICollection, ICollection<T>, IReadOnlyList<T> // where T : class
	{
		// ******************************************************************
		private List<T> _recordedNew = new List<T>();
		private List<T> _recordedRemoved = new List<T>();
		private bool _isRecording = false;

		protected object _syncRoot = new object();

		protected List<T> List = new List<T>();

		public bool TestVal { get; set; }

		private readonly ObservableCollection<T> _obsColl = new ObservableCollection<T>();
		private readonly ConcurrentQueue<NotifyCollectionChangedEventArgs> _uiItemQueue = new ConcurrentQueue<NotifyCollectionChangedEventArgs>();
		public event OnMtCollectionChangedHandler OnMtCollectionChanged;
		public Dispatcher Dispatcher { get; set; }

		// ******************************************************************
		/// <summary>
		/// You should never add any item directly in the collection. 
		/// It should only serve as a readonly collection for the UI.
		/// If you ever decide to do so, it would be preferable to use directly the ObsCollection 
		/// without ever using this class (kind of detach)
		/// </summary>
		public ObservableCollection<T> ObsColl
		{
			get { return _obsColl; }
		}

		// ******************************************************************
		public CollectionMtWithAsyncObservableCollectionReadOnlyCopy()
		{
			Dispatcher = Application.Current.Dispatcher;
		}

		// ******************************************************************
		public bool IsRecording
		{
			get { return _isRecording; }
			set { _isRecording = value; }
		}

		// ******************************************************************
		/// <summary>
		/// Return tuple of new and removed items
		/// </summary>
		/// <returns></returns>
		public Tuple<List<T>, List<T>> ResetRecordedItems()
		{
			Tuple<List<T>, List<T>> changes;
			lock (_syncRoot)
			{
				changes = new Tuple<List<T>, List<T>>(_recordedNew, _recordedRemoved);
				_recordedNew = new List<T>();
				_recordedRemoved = new List<T>();
			}

			return changes;
		}

		// ******************************************************************
		public T[] GetCopyOfRecordedItemsNew()
		{
			T[] changes;
			lock (_syncRoot)
			{
				changes = _recordedNew.ToArray();
			}

			return changes;
		}

		// ******************************************************************
		public T[] GetCopyOfRecordedItemsRemoved()
		{
			T[] changes;
			lock (_syncRoot)
			{
				changes = _recordedRemoved.ToArray();
			}

			return changes;
		}

		// ******************************************************************
		private void AddTask(NotifyCollectionChangedEventArgs args)
		{
			_uiItemQueue.Enqueue(args);
			Dispatcher.BeginInvoke(new Action(this.ProcessQueue), DispatcherPriority.ContextIdle);
		}

		// ******************************************************************
		private void ProcessQueue()
		{
			// This Method should always be invoked only by the UI thread only.
			if (!this.Dispatcher.CheckAccess())
			{
				throw new Exception("Can't be called from any thread than the dispatcher one");
			}

			NotifyCollectionChangedEventArgs args;
			while (this._uiItemQueue.TryDequeue(out args))
			{
				switch (args.Action)
				{
					case NotifyCollectionChangedAction.Add:
						int offset = 0;
						foreach (T item in args.NewItems)
						{
							ObsColl.Insert(args.NewStartingIndex + offset, item);
							offset++;
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						if (args.NewStartingIndex >= 0)
						{
							ObsColl.RemoveAt(args.NewStartingIndex);
						}
						else
						{
							foreach (T item in args.OldItems)
							{
								ObsColl.Remove(item);
							}
						}
						break;
					case NotifyCollectionChangedAction.Replace:
						// Replace is used for the [] operator. 'Insert' raise an 'Add' event.

						if (args.NewStartingIndex >= 0 && args.OldStartingIndex < 0)
						{
							throw new ArgumentException(String.Format("Replace action expect NewStartingIndex and OldStartingIndex as: 0 <= {0} <= {1}, {2} <= 0.", args.NewStartingIndex, ObsColl.Count, args.OldStartingIndex));
						}

						IList listOld = args.OldItems as IList;
						IList listNew = args.NewItems as IList;

						if (listOld == null || listNew == null)
						{
							throw new ArgumentException("Both argument Old and New item should be IList in a replace action.");
						}

						ObsColl[args.NewStartingIndex] = (T)listNew[0];
						break;
					case NotifyCollectionChangedAction.Reset:
						ObsColl.Clear();
						break;
					case NotifyCollectionChangedAction.Move:
						ObsColl.Move(args.OldStartingIndex, args.NewStartingIndex);
						break;
					default:
						throw new Exception("Unsupported NotifyCollectionChangedEventArgs.Action");
				}
			}
		}

		// ******************************************************************
		public List<T> GetSnapshot()
		{
			List<T> listCopy = null;

			lock (_syncRoot)
			{
				listCopy = new List<T>(List);
			}

			return listCopy;
		}

		// ******************************************************************
		public void GetSnapshot(IList list)
		{
			lock (_syncRoot)
			{
				foreach (var item in List)
				{
					list.Add(item);
				}
			}
		}

		// ******************************************************************
		public virtual IEnumerator<T> GetEnumerator()
		{
			return GetSnapshot().GetEnumerator();
		}

		// ******************************************************************
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetSnapshot().GetEnumerator();
		}

		//// ******************************************************************
		//private void DoAsynch(Action action)
		//{
		//    Dispatcher.BeginInvoke(action);
		//}

		// ******************************************************************
		public void InsertAsFirst(T item)
		{
			NotifyCollectionChangedEventArgs args;
			lock (_syncRoot)
			{
				List.Insert(0, item);
				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, 0);
				AddTask(args);
			}

			RaiseEventCollectionChanged(args);
		}

		// ******************************************************************
		public void Add(T item)
		{
			NotifyCollectionChangedEventArgs args;
			lock (_syncRoot)
			{
				List.Add(item);

				if (_isRecording)
				{
					_recordedNew.Add(item);
				}

				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, List.Count - 1);
				AddTask(args);
			}

			RaiseEventCollectionChanged(args);
		}

		public void Add(IList<T> items)
		{
			NotifyCollectionChangedEventArgs args;
			lock (_syncRoot)
			{
				int insertIndex = List.Count;
				List.AddRange(items);

				if (_isRecording)
				{
					_recordedNew.AddRange(items);
				}

				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items as IList, insertIndex);
				AddTask(args);
			}

			RaiseEventCollectionChanged(args);
		}

		// ******************************************************************
		public bool Remove(T item)
		{
			bool isRemoved = false;

			NotifyCollectionChangedEventArgs args;
			lock (_syncRoot)
			{
				isRemoved = List.Remove(item);

				if (_isRecording)
				{
					_recordedNew.Add(item);
				}

				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item);
				AddTask(args);
			}

			RaiseEventCollectionChanged(args);

			return isRemoved;
		}

		// ******************************************************************
		public void Replace(T itemOld, T itemNew)
		{
			NotifyCollectionChangedEventArgs args = null;
			lock (_syncRoot)
			{
				int index = List.IndexOf(itemOld);
				if (index < 0 || index >= List.Count)
				{
					throw new ArgumentException("Invalid old value");
				}

				if (_isRecording)
				{
					_recordedNew.Add(itemNew);
					_recordedRemoved.Add(itemOld);
				}

				List[index] = itemNew;

				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, itemNew, itemOld, index);
				AddTask(args);
			}

			RaiseEventCollectionChanged(args);
		}

		// ******************************************************************
		protected virtual void RaiseEventCollectionChanged(NotifyCollectionChangedEventArgs args)
		{
			if (OnMtCollectionChanged != null && args != null)
			{
				OnMtCollectionChanged(this, args);
			}
		}

		// ******************************************************************
		/// <summary>
		/// To use this function and all 'Unsafe' ones in a MT context, 
		/// you should have a lock on the collection prior to call it.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T UnsafeGetAt(int index)
		{
			return List[index];
		}

		// ******************************************************************
		/// <summary>
		/// To use this function and all 'Unsafe' ones in a MT context, 
		/// you should have a lock on the collection prior to call it.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public T UnsafeSetAt(int index, T itemNew)
		{
			T itemOld = List[index];

			if (_isRecording)
			{
				_recordedNew.Add(itemNew);
				_recordedRemoved.Add(itemOld);
			}

			List[index] = itemNew;

			NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, itemNew, itemOld, index);
			AddTask(args);

			RaiseEventCollectionChanged(args);

			return itemOld;
		}

		// ******************************************************************
		public void UnsafeInsertAt(int index, T itemNew)
		{
			if (_isRecording)
			{
				_recordedNew.Add(itemNew);
			}

			List.Insert(index, itemNew);

			NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itemNew, index);
			AddTask(args);

			RaiseEventCollectionChanged(args);
		}

		// ******************************************************************
		/// <summary>
		/// To use this function and all 'Unsafe' ones in a MT context, 
		/// you should have a lock on the collection prior to call it.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T UnsafeRemoveAt(int index)
		{
			T itemOld = List[index];

			if (_isRecording)
			{
				_recordedRemoved.Add(itemOld);
			}

			List.RemoveAt(index);

			NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, itemOld, index);
			AddTask(args);

			RaiseEventCollectionChanged(args);

			return itemOld;
		}

		//// ******************************************************************
		//public virtual void AddRange(IEnumerable<T> items)
		//{
		//    NotifyCollectionChangedEventArgs args;
		//    lock (_syncRoot)
		//    {
		//        List.AddRange(items);
		//        args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, List.Count);
		//        AddTask(args);
		//    }

		//    if (OnMtCollectionChanged != null)
		//    {
		//        OnMtCollectionChanged(args);
		//    }
		//}

		// ******************************************************************
		public virtual void Clear()
		{
			NotifyCollectionChangedEventArgs args = null;
			lock (_syncRoot)
			{
				if (_isRecording)
				{
					_recordedRemoved.AddRange(List);
				}

				List.Clear();
				args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
				AddTask(args);

			}

			RaiseEventCollectionChanged(args);
		}

		// ******************************************************************
		public bool Contains(T item)
		{
			bool result;
			lock (_syncRoot)
			{
				result = List.Contains(item);
			}

			return result;
		}

		// ******************************************************************
		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (_syncRoot)
			{
				List.CopyTo(array, arrayIndex);
			}
		}

		// ******************************************************************
		public int Count
		{
			get
			{
				lock (_syncRoot)
				{
					return List.Count;
				}
			}
		}

		// ******************************************************************
		public void Remove(object item)
		{
			Remove((T)item);
		}

		// ******************************************************************
		public int IndexOf(object value)
		{
			return IndexOf((T)value);
		}

		//// ******************************************************************
		//object IList.this[int index]
		//{
		//    get
		//    {
		//        return this[index];
		//    }
		//    set
		//    {
		//        this[index] = (T)value;
		//    }
		//}

		// ******************************************************************
		//public void CopyTo(Array array, int index)
		//{
		//    if (Application.Current.Dispatcher.CheckAccess())
		//    {
		//        if (array.Rank != 1)
		//            throw new ArgumentException("array should not be multi dimentional, unsupported.");
		//        if (array.GetLowerBound(0) != 0)
		//            throw new ArgumentException("array should be lower bound 0 base.");
		//        if (index < 0)
		//            throw new ArgumentException("index should > 0.");
		//        if (array.Length - index < this.Count)
		//            throw new ArgumentException("index should be less than or equal to collection count");
		//        T[] array1 = array as T[];
		//        if (array1 != null)
		//        {
		//            this.CopyTo(array1, index);
		//        }
		//        else
		//        {
		//            Type elementType = array.GetType().GetElementType();
		//            Type c = typeof(T);
		//            if (!elementType.IsAssignableFrom(c) && !c.IsAssignableFrom(elementType))
		//                throw new ArgumentException("array is of wronf type.");
		//            object[] objArray = array as object[];
		//            if (objArray == null)
		//                throw new ArgumentException("array is of wronf type.");
		//            int count = this.Count;
		//            try
		//            {
		//                for (int index1 = 0; index1 < count; ++index1)
		//                    objArray[index++] = (object)this[index1];
		//            }
		//            catch (ArrayTypeMismatchException ex)
		//            {
		//                throw;
		//            }
		//        }
		//    }
		//    else
		//    {
		//        lock (SyncRoot)
		//        {
		//            if (array.Rank != 1)
		//                throw new ArgumentException("array should not be multi dimentional, unsupported.");
		//            if (array.GetLowerBound(0) != 0)
		//                throw new ArgumentException("array should be lower bound 0 base.");
		//            if (index < 0)
		//                throw new ArgumentException("index should > 0.");
		//            if (array.Length - index < this.Count)
		//                throw new ArgumentException("index should be less than or equal to collection count");
		//            T[] array1 = array as T[];
		//            if (array1 != null)
		//            {
		//                this.CopyTo(array1, index);
		//            }
		//            else
		//            {
		//                Type elementType = array.GetType().GetElementType();
		//                Type c = typeof(T);
		//                if (!elementType.IsAssignableFrom(c) && !c.IsAssignableFrom(elementType))
		//                    throw new ArgumentException("array is of wronf type.");
		//                object[] objArray = array as object[];
		//                if (objArray == null)
		//                    throw new ArgumentException("array is of wronf type.");
		//                int count = this.Count;
		//                try
		//                {
		//                    for (int index1 = 0; index1 < count; ++index1)
		//                        objArray[index++] = (object)this[index1];
		//                }
		//                catch (ArrayTypeMismatchException ex)
		//                {
		//                    throw;
		//                }
		//            }
		//        }
		//    }
		//}

		// ******************************************************************
		public object SyncRoot
		{
			get { return _syncRoot; }
		}

		// ******************************************************************
		public bool IsEqual(IEnumerable<T> iEnumerable)
		{
			if (this.Count != iEnumerable.Count())
			{
				return false;
			}

			lock (_syncRoot)
			{
				var thisEnumerator = this.GetEnumerator();
				thisEnumerator.Reset();
				foreach (var t in iEnumerable)
				{
					thisEnumerator.MoveNext();
					if (thisEnumerator.Current.Equals(t))
					{
						return false;
					}
				}

				IDisposable disposable = thisEnumerator as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}

			return true;
		}

		// ******************************************************************
		private void IsEqualToObsColl()
		{
			if (!IsEqual(this.ObsColl))
			{
				Dump();
			}
		}

		// ******************************************************************
		/// <summary>
		/// This function dumps to the ouput window formated lines of the content of both collections...
		/// The list which is thread safe and the obs coll that is used as a readonly list. 
		/// Its main purpose is to debug to validate that both list contains the same values in the same order.
		/// </summary>
		private void Dump()
		{
			Debug.Print("=============== Start");

			lock (_syncRoot)
			{
				IEnumerator enum1 = List.GetEnumerator();
				IEnumerator enum2 = ObsColl.GetEnumerator();

				enum1.Reset();
				enum2.Reset();

				bool ok1 = enum1.MoveNext();
				bool ok2 = enum2.MoveNext();

				while (ok1 || ok2)
				{
					Debug.Print(String.Format("{0,20} - {0,-20}", ok1 == true ? enum1.Current : "-", ok2 == true ? enum2.Current : "-"));

					if (ok1)
						ok1 = enum1.MoveNext();

					if (ok2)
						ok2 = enum2.MoveNext();
				}

				((IDisposable)enum1).Dispose();
				((IDisposable)enum2).Dispose();
			}

			Debug.Print("=============== End");
		}

		// ******************************************************************
		public static void Test()
		{
			CollectionMtWithAsyncObservableCollectionReadOnlyCopy<string> colTest = new CollectionMtWithAsyncObservableCollectionReadOnlyCopy<string>();
			colTest.Add("2");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Add("3");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.InsertAsFirst("1");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.InsertAsFirst("0");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Remove("0");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Remove("1");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Add("7");
			colTest.Add("8");
			colTest.Add("9");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Add("10");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Dump();
			colTest.UnsafeSetAt(5, "t");
			colTest.Dump();
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Dump();
			colTest.Replace("9", "11");
			colTest.Dump();
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.Replace("7", "6");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));
			colTest.UnsafeSetAt(5, "5");
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(colTest.IsEqualToObsColl));

		}

		// ******************************************************************
		[OnSerializing]
		void OnSerializing(StreamingContext ctx)
		{
			Monitor.Enter(this._syncRoot);
		}

		// ******************************************************************
		[OnSerialized]
		void OnSerialized(StreamingContext ctx)
		{
			Monitor.Exit(this._syncRoot);
		}

		// ******************************************************************
		[OnDeserializing]
		void OnDeserializing(StreamingContext ctx)
		{

		}

		// ******************************************************************
		[OnDeserialized]
		void OnDeserialized(StreamingContext ctx)
		{

		}

		// ******************************************************************
		/// <summary>
		/// ATTENTION : This method is not MT safe
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index]
		{
			get { return this.List[index]; }
		}

		// ******************************************************************
		/// <summary>
		/// Add stack functionnality to use the list as a queue
		/// </summary>
		/// <param name="item"></param>
		public void Push(T item)
		{
			Add(item);
		}

		// ******************************************************************
		/// <summary>
		/// Add stack functionnality to use the list as a queue
		/// </summary>
		/// <returns></returns>
		public T Pop()
		{
			T item = default(T);

			lock (_syncRoot)
			{
				int count = List.Count;
				if (count > 0)
				{
					item = UnsafeRemoveAt(count - 1);
				}
			}

			return item;
		}

		// ******************************************************************
		public bool IsReadOnly
		{
			get { return false; }
		}

		// ******************************************************************
		bool ICollection<T>.Remove(T item)
		{
			return Remove(item);
		}

		// ******************************************************************
		public void CopyTo(Array array, int index)
		{
			lock (_syncRoot)
			{
				foreach (var t in List)
				{
					array.SetValue(t, index++);
				}
			}
		}

		// ******************************************************************
		public bool IsSynchronized
		{
			get { return Dispatcher.CheckAccess(); }
		}

		// ******************************************************************

	}
}
