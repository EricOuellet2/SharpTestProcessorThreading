using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SystemProcessorInfo
{
	public class MainWindowModel // No INotifyPropertyChange (I'm too lazy)
	{
		public int WmiProcessorCount { get; private set; }
		public int WmiGlobalCoreCount { get; private set; }
		public int WmiLogicalProcessorCount { get; private set; }
		public int CSharpEnvironmentLogicalProcessorCount{ get; private set; }
		public int NumaHighestNodeNumber { get; private set; }
		public int ProcessorGroupCount { get; private set; }
		public int ThreadPoolMaxThreadsCountWorkerThreads { get; private set; }
		public int ThreadPoolMaxThreadsCountCompletionPortThreads { get; private set; }

		public UInt64 ProcessAffinityMask { get; private set; }
		public UInt64 SystemAffinityMask { get; private set; }
		
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
			ProcessorGroupCount = SystemInfoHelper.GetActiveProcessorGroupCount();
			CSharpEnvironmentLogicalProcessorCount = Environment.ProcessorCount;

			UInt64 processAffinityMask;
			UInt64 systemAffinityMask;

			SystemInfoHelper.GetProcessAffinityMask(
				System.Diagnostics.Process.GetCurrentProcess().Handle, 
				out processAffinityMask,
				out systemAffinityMask);

			ProcessAffinityMask = processAffinityMask;
			SystemAffinityMask = systemAffinityMask;
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
