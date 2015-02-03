using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HQ.Util.General.Collections;

namespace SystemProcessorInfo
{
	public class MainWindowModel // No INotifyPropertyChange (I'm too lazy)
	{
		public int WmiProcessorCount { get; private set; }
		public int WmiGlobalCoreCount { get; private set; }
		public int WmiLogicalProcessorCount { get; private set; }
		public int CSharpEnvironmentLogicalProcessorCount{ get; private set; }
		public int NumaHighestNodeNumber { get; private set; }
		public int ActiveProcessorGroupCount { get; private set; }
		public int MaximumProcessorGroupCount { get; private set; }
		public int ThreadPoolMaxThreadsCountWorkerThreads { get; private set; }
		public int ThreadPoolMaxThreadsCountCompletionPortThreads { get; private set; }

		public UInt64 ProcessAffinityMask { get; private set; }
		public string ProcessAffinityMaskString { get; private set; }
		
		public UInt64 SystemAffinityMask { get; private set; }
		public string SystemAffinityMaskString { get; private set; }

		public string NumaNodeAndTheirAffinityMask { get; private set; }

		public MainWindowModel()
		{
			Refresh();
			RefreshThreadPoolInfo();
		}

		public void Refresh()
		{
			WmiProcessorCount = SystemInfoHelper.GetWmiPhysicalProcessorCount();
			WmiGlobalCoreCount = SystemInfoHelper.GetWmiCoreCount();
			WmiLogicalProcessorCount = SystemInfoHelper.GetWmiGlobalLogicalProcessorCount();
			NumaHighestNodeNumber = SystemInfoHelper.GetNumaHighestNodeNumber();
			ActiveProcessorGroupCount = SystemInfoHelper.GetActiveProcessorGroupCount();
			MaximumProcessorGroupCount = SystemInfoHelper.GetMaximumProcessorGroupCount();
			CSharpEnvironmentLogicalProcessorCount = Environment.ProcessorCount;

			UInt64 processAffinityMask;
			UInt64 systemAffinityMask;

			SystemInfoHelper.GetProcessAffinityMask(
				System.Diagnostics.Process.GetCurrentProcess().Handle, 
				out processAffinityMask,
				out systemAffinityMask);

			ProcessAffinityMask = processAffinityMask;
			ProcessAffinityMaskString = String.Format("{0} (bit count: {1})\r\n{2}", processAffinityMask, GetBitCount(processAffinityMask), GetBitString(processAffinityMask));
			SystemAffinityMask = systemAffinityMask;
			SystemAffinityMaskString = String.Format("{0} (bit count: {1})\r\n{2}", systemAffinityMask, GetBitCount(processAffinityMask), GetBitString(systemAffinityMask)); 

			var sb = new StringBuilder();
			for (int nodeIndex = 0; nodeIndex <= NumaHighestNodeNumber; nodeIndex++)
			{
				UInt64 numaNodeProcessorMask;
				SystemInfoHelper.GetNumaNodeProcessorMask((byte)nodeIndex, out numaNodeProcessorMask);
				sb.Append(String.Format("Node: {0} Processor Mask: {1} (bit count: {2})", nodeIndex, numaNodeProcessorMask, GetBitCount(numaNodeProcessorMask)));
				sb.Append(Environment.NewLine);
				sb.Append(GetBitString(numaNodeProcessorMask));
				sb.Append(Environment.NewLine);
			}

			NumaNodeAndTheirAffinityMask = sb.ToString();

			var structLogProcInfo = SystemInfoHelper.GetLogicalProcessorInformation();
			
		}

		public int GetBitCount(UInt64 number)
		{
			int count = 0;
			while (number != 0)
			{
				if ((number & 1) == 1)
				{
					count++;
				}
				number = number >> 1;
			}
			return count;
		}

		private const UInt64 Int64Lastbit = 9223372036854775808; 

		public string GetBitString(UInt64 number)
		{
			var sb = new StringBuilder();
			UInt64 bit = Int64Lastbit;

			for(int index = 0; index < 64; index++)
			{
				if ((number & bit) > 0)
				{
					sb.Append('1');
				}
				else
				{
					sb.Append('-');
				}
				bit = bit >> 1;
			}

			return sb.ToString();
		}
		
		public void RefreshThreadPoolInfo()
		{
			int workerThreads;
			int completionPortThreads;
			ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
			ThreadPoolMaxThreadsCountWorkerThreads = workerThreads;
			ThreadPoolMaxThreadsCountCompletionPortThreads = completionPortThreads;
		}
	}
}
