﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemProcessorInfo 
{
	public class ThreadInfo : NotifyPropertyChangedThreadSafeAsyncBase
	{
		// ******************************************************************
		private int _index = 0;
		private int _threadId = 0;
		private int _processorGroup = 0;
		private int _currentProcessorNumber = 0;

		// ******************************************************************
		public int Index
		{
			get { return _index; }
			set
			{
				if (_index != value)
				{
					_index = value;
					NotifyPropertyChanged(()=>Index);
				}
			}
		}

		// ******************************************************************
		public int ThreadId
		{
			get { return _threadId; }
			set
			{
				if (_threadId != value)
				{
					_threadId = value;
					NotifyPropertyChanged(()=>ThreadId);
				}
			}
		}

		// ******************************************************************
		public int ProcessorGroup
		{
			get { return _processorGroup; }
			set
			{
				if (_processorGroup != value)
				{
					_processorGroup = value;
					NotifyPropertyChanged(()=>ProcessorGroup);
				}
			}
		}

		// ******************************************************************
		public int CurrentProcessorNumber
		{
			get { return _currentProcessorNumber; }
			set
			{
				if (_currentProcessorNumber != value)
				{
					_currentProcessorNumber = value;
					NotifyPropertyChanged(()=>CurrentProcessorNumber);
				}
			}
		}

		// ******************************************************************

	}
}