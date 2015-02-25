using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HQ.Util.General.Collections;

namespace SystemProcessorInfo
{
	public class WindowRunThreadModel : NotifyPropertyChangedThreadSafeAsyncBase
	{
		public ObservableCollection<ThreadInfo> CollThreadInfo { get; set; }

		private Timer _timer = null;

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

		private int _millisecs;
		public int Millisecs
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

		// ******************************************************************
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
		private bool _isRunning = false;
		public bool IsRunning
		{
			get { return _isRunning; }
			set
			{
				if (_isRunning != value)
				{
					_isRunning = value;
					NotifyPropertyChanged(() => IsRunning);
				}
			}
		}

		// ******************************************************************
		public WindowRunThreadModel()
		{
		}

		// ******************************************************************
		public void StartThreads()
		{
			var until = DateTime.Now.AddMilliseconds(Millisecs);

			IsRunning = true;

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

			if (_timer == null)
			{
				_timer = new Timer(StopTimer, null, Millisecs, Timeout.Infinite);
			}
			else
			{
				_timer.Change(Millisecs, Timeout.Infinite);
			}
		}

		// ******************************************************************
		private void StopTimer(object state)
		{
			IsRunning = false;
		}
		
		private int _threadCount = 0;
		
		// ******************************************************************
		private void LooseYourTime(int index, DateTime until)
		{
			var processorNumber = new SystemInfoHelper.PROCESSOR_NUMBER();
			SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);

			Interlocked.Increment(ref _threadCount);

			ThreadInfo ti = CollThreadInfo[index];
			ti.Index = index;
			ti.ThreadId = Thread.CurrentThread.ManagedThreadId;
			ti.CurrentProcessorNumber = processorNumber.Number;
			ti.ProcessorGroup = processorNumber.Group;

			double x, y;
			int i = 1;
			while (IsRunning)
			{
				i = i + 1;

				PrimeTool.IsPrime(i);
				if (i == int.MaxValue)
				{
					i = 0;
					x= Math.Sqrt((double)i);
					x = x + x;
				}

				ti.ThreadId = Thread.CurrentThread.ManagedThreadId;

				SystemInfoHelper.GetCurrentProcessorNumberEx(ref processorNumber);
				ti.CurrentProcessorNumber = processorNumber.Number;
				ti.ProcessorGroup = processorNumber.Group;
			}
		}

		// ******************************************************************




	}
}
