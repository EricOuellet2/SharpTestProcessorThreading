using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HQ.Util.General.Collections;

namespace SystemProcessorInfo
{
	public class WindowRunThreadModel : NotifyPropertyChangedThreadSafeAsyncBase
	{
		public CollectionMtWithAsyncObservableCollectionReadOnlyCopy<ThreadInfo> CollThreadInfo { get; set; }


		private int _numberOfThread;
		public int NumberOfThread
		{
			get { return _numberOfThread; }
			set
			{
				if (_numberOfThread != value)
				{
					_numberOfThread = value;
					NotifyPropertyChanged(()=>NumberOfThread);
				}
			}
		}

		private double _millisecs;
		public double Millisecs
		{
			get { return _millisecs; }
			set
			{
				if (_millisecs != value)
				{
					_millisecs = value;
					NotifyPropertyChanged(()=>Millisecs);
				}
			}
		}

		private bool _useThreadPool = false;

		public bool UseThreadPool
		{
			get { return _useThreadPool; }
			set
			{
				if (_useThreadPool != value)
				{
					_useThreadPool = value;
					NotifyPropertyChanged(()=>UseThreadPool);
				}
			}
		}

		// ******************************************************************
		public WindowRunThreadModel()
		{
			CollThreadInfo  = new CollectionMtWithAsyncObservableCollectionReadOnlyCopy<ThreadInfo>();
		}

		// ******************************************************************
		public void StartThreads()
		{
			var until = DateTime.Now.AddMilliseconds(Millisecs);

			if (UseThreadPool)
			{
				Parallel.For(0, NumberOfThread, (n) => LooseYourTime(n, until));
			}
			else
			{
				for (int n = 0; n < NumberOfThread; n++)
				{
					int localN = n;
					var thread = new Thread(() => LooseYourTime(localN, until));
					thread.Priority = ThreadPriority.Lowest;
					thread.Start();
				}
			}
		}
		
		private int _threadCount = 0;

		// ******************************************************************
		private void LooseYourTime(int index, DateTime until)
		{
			var processorNumber = new SystemInfoHelper.PROCESSOR_NUMBER();
			SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);

			Interlocked.Increment(ref _threadCount);

			var ti = new ThreadInfo
			{
				Index = index,
				ThreadId = Thread.CurrentThread.ManagedThreadId,
				CurrentProcessorNumber = processorNumber.Number,
				ProcessorGroup = processorNumber.Group
			};
			CollThreadInfo.Add(ti);

			int i = 1;
			while (DateTime.Now < until)
			{
				i = i + 1;
				if (i > 100000)
				{
					i = 0;

					ti.ThreadId = Thread.CurrentThread.ManagedThreadId;
					SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);
					ti.CurrentProcessorNumber = processorNumber.Number;
					ti.ProcessorGroup = processorNumber.Group;
				}
			}
		}

		// ******************************************************************




	}
}
