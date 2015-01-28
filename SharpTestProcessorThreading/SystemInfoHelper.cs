using System;
using System.Management; // Require: System.Management
using System.Runtime.InteropServices;

namespace SystemProcessorInfo
{
	// http://multiproc.codeplex.com/

	// Ref: http://blogs.msdn.com/b/virtual_pc_guy/archive/2010/08/13/using-powershell-to-find-the-virtual-processor-to-logical-processor-ratio-of-hyper-v.aspx
	// Ref excerpt:
	// Power Shell command:
	// write-host (@(gwmi -ns root\virtualization MSVM_Processor).count / (@(gwmi Win32_Processor) | measure -p NumberOfLogicalProcessors -sum).Sum) "virtual processor(s) per logical processor" -f yellow

	/* 
		But what does this line of code do – and how does it work?  Well – there are a couple of things to know:

		For each virtual processor for each currently running virtual machine there is an instance of the MSVM_Processor WMI object on the system.  So to figure out how many virtual processors are currently running – you just have to count how many instances of this WMI object are currently present on the system.
		Summing up the NumberOfLogicalProcessors property of the Win32_Processor object will given you the other side of the calculation – namely how many logical processors are present in the system.
		In order to keep the one line of code as short as possible – I have used every PowerShell abbreviation that I know of (gwmi for Get-WMIObject, –ns for –namespace, measure for measure-object, –p for –property, –f for –foregroundColor)
	 */

	public class SystemInfoHelper
	{
		public static int GetWmiPhysicalProcessorCount()
		{
			var searcherCpuCount = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
			int processorCount = -1;
			try
			{
				foreach (var res in searcherCpuCount.Get())
				{
					processorCount = Convert.ToInt32(res["NumberOfProcessors"]);
				}
			}
			catch (Exception ex)
			{
			}

			return processorCount;
		}

		public static int GetWmiCoreCount() // It one show 1 core when running on virutal machine
		{
			var searcherCoreCount = new ManagementObjectSearcher("Select * from Win32_Processor");
			int coreCount = -1;
			try
			{
				foreach (var res in searcherCoreCount.Get())
				{
					coreCount = Convert.ToInt32(res["NumberOfCores"]);
				}
			}
			catch (Exception)
			{
			}

			return coreCount;
		}

		//public static int GetWmiLogicalProcessorCount2()
		//{
		//	var searcherCoreCount = new ManagementObjectSearcher("Select * from Win32_Processor");
		//	int coreCount = -1;

		//	try
		//	{
		//		foreach (var res in searcherCoreCount.Get())
		//		{
		//			coreCount = Convert.ToInt32(res["NumberOfLogicalProcessors"]);
		//		}
		//	}
		//	catch (Exception)
		//	{
		//	}

		//	return coreCount;
		//}

		public static int GetWmiGlobalLogicalProcessorCount()
		{
			var searcherCpuCount = new ManagementObjectSearcher("Select * from Win32_ComputerSystem");
			int processorCount = -1;

			try
			{
				foreach (var res in searcherCpuCount.Get())
				{
					processorCount = Convert.ToInt32(res["NumberOfLogicalProcessors"]);
				}
			}
			catch (Exception)
			{
			}
			
			return processorCount;
		}

		public static int GetLogicalProcessorCountFromCsharpEnvironment()
		{
			return Environment.ProcessorCount;
		}

		/// <summary>
		/// Althought not garanty to be exact as per documentation, that's the simplest way to 
		/// know the number of ProcessorGroup (Eric Ouellet)
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetNumaHighestNodeNumber(out uint count);

		public static int GetNumaHighestNodeNumber()
		{
			uint processorGroup = 0;
			try
			{
				GetNumaHighestNodeNumber(out processorGroup);
			}
			catch (Exception)
			{
				return -1;
			}
			
			return (int) processorGroup + 1; // Node number start ar 0
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern ushort GetActiveProcessorGroupCount();

	}
}
