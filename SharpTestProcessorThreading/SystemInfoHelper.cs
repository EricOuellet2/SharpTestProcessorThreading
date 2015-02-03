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
			uint numaHighestNodeNumber = 0;
			try
			{
				GetNumaHighestNodeNumber(out numaHighestNodeNumber);
			}
			catch (Exception)
			{
				return -1;
			}

			return (int)numaHighestNodeNumber; // Node number start ar 0
		}

		[DllImport("kernel32.dll")]
		public static extern UInt16 GetActiveProcessorGroupCount();

		[DllImport("kernel32.dll")]
		public static extern UInt16 GetMaximumProcessorGroupCount();


		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetProcessAffinityMask(IntPtr hProcess,
		   out UInt64 lpProcessAffinityMask, out UInt64 lpSystemAffinityMask);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetNumaNodeProcessorMask(byte node, out UInt64 processorMask);

		/// <summary>
		/// By thread
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		public static extern UInt32 GetCurrentProcessorNumber();

		public struct PROCESSOR_NUMBER
		{
			public UInt16 Group;
			public byte Number;
			public byte Reserved;
		}

		[DllImport("kernel32.dll")]
		public static extern void GetCurrentProcessorNumberEx(ref PROCESSOR_NUMBER processorNumber);
		

		/// <summary>
		/// Start: From David Hefffernan on StackOverflow: Code to get GetLogicalProcessorInformation
		/// 
		/// EO: I Think it is buggy: I receive 27 processor groups on a machine with 16
		/// </summary>


		public enum PROCESSOR_CACHE_TYPE
		{
			Unified = 0,
			Instruction = 1,
			Data = 2,
			Trace = 3,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CACHE_DESCRIPTOR
		{
			public byte Level;
			public byte Associativity;
			public ushort LineSize;
			public uint Size;
			public PROCESSOR_CACHE_TYPE Type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESSORCORE
		{
			public byte Flags;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct NUMANODE
		{
			public uint NodeNumber;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION
		{
			[FieldOffset(0)]
			public PROCESSORCORE ProcessorCore;
			[FieldOffset(0)]
			public NUMANODE NumaNode;
			[FieldOffset(0)]
			public CACHE_DESCRIPTOR Cache;
			[FieldOffset(0)]
			private UInt64 Reserved1;
			[FieldOffset(8)]
			private UInt64 Reserved2;
		}

		public enum LOGICAL_PROCESSOR_RELATIONSHIP
		{
			RelationProcessorCore,
			RelationNumaNode,
			RelationCache,
			RelationProcessorPackage,
			RelationGroup,
			RelationAll = 0xffff
		}

		public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
		{
			public UIntPtr ProcessorMask;
			public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
			public SYSTEM_LOGICAL_PROCESSOR_INFORMATION_UNION ProcessorInformation;
		}

		[DllImport(@"kernel32.dll", SetLastError = true)]
		private static extern bool GetLogicalProcessorInformation(
			IntPtr Buffer,
			ref uint ReturnLength
		);

		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		public static SYSTEM_LOGICAL_PROCESSOR_INFORMATION[] GetLogicalProcessorInformation()
		{
			uint ReturnLength = 0;
			GetLogicalProcessorInformation(IntPtr.Zero, ref ReturnLength);
			if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
			{
				IntPtr Ptr = Marshal.AllocHGlobal((int)ReturnLength);
				try
				{
					if (GetLogicalProcessorInformation(Ptr, ref ReturnLength))
					{
						int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
						int len = (int)ReturnLength / size;
						var buffer = new SYSTEM_LOGICAL_PROCESSOR_INFORMATION[len];
						IntPtr Item = Ptr;
						for (int i = 0; i < len; i++)
						{
							buffer[i] = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION)Marshal.PtrToStructure(Item, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
							Item += size;
						}
						return buffer;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(Ptr);
				}
			}
			return null;
		}

		// End: From David Hefffernan on StackOverflow: Code to get GetLogicalProcessorInformation


	}
}
