using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemProcessorInfo
{
	public class MainWindowModel // No INotifyPropertyChange (I'm too lazy)
	{
		public int WmiProcessorCount { get; private set; }
		public int WmiGlobalCoreCount { get; private set; }
		public int WmiLogicalProcessorCount { get; private set; }
		public int CSharpEnvironmentLogicalProcessorCount{ get; private set; }
		public int ProcessorGroupCount { get; private set; }


		public MainWindowModel()
		{
			Refresh();
		}

		public void Refresh()
		{
			WmiProcessorCount = SystemInfoHelper.GetWmiPhysicalProcessorCount();
			WmiGlobalCoreCount = SystemInfoHelper.GetWmiCoreCount();
			WmiLogicalProcessorCount = SystemInfoHelper.GetWmiGlobalLogicalProcessorCount();
			ProcessorGroupCount = SystemInfoHelper.GetProcessorGroupCount();
			CSharpEnvironmentLogicalProcessorCount = Environment.ProcessorCount;
		}
	}
}
