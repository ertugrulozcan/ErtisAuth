using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ErtisAuth.WebAPI.Helpers;

internal static class SystemDiagnostics
{
	private static long totalMemoryInKb;
	
	/// <summary>
	/// Get the system overall CPU usage percentage.
    /// </summary>
    /// <returns>The percentage value with the '%' sign. e.g. if the usage is 30.1234 %, then it will return 30.12.</returns>
    public static double GetOverallCpuUsage()
    {
	    try
	    {
		    var startTime = DateTime.UtcNow;
		    var startCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
	    
		    System.Threading.Thread.Sleep(500);
	    
		    var endTime = DateTime.UtcNow;
		    var endCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
	    
		    var cpuUsedMs = endCpuUsage - startCpuUsage;
		    var totalMsPassed = (endTime - startTime).TotalMilliseconds;
		    var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
	    
		    return cpuUsageTotal * 100.0;
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
		    return 0;
	    }
    }

    /// <summary>
    /// Get the system overall memory usage percentage.
    /// </summary>
    /// <returns>The percentage value with the '%' sign. e.g. if the usage is 30.1234 %, then it will return 30.12.</returns>
    public static double GetOccupiedMemoryPercentage()
    {
	    try
	    {
		    var totalMemory = GetTotalMemoryInKb() * 1024;
		    var usedMemory = GetUsedMemoryForAllProcesses();
	    
		    return (usedMemory * 100.0) / totalMemory;
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
		    return 0;
	    }
    }
    
    internal static double GetUsedMemoryForAllProcesses()
    {
	    try
	    {
		    var totalAllocatedMemoryInBytes = Process.GetProcesses().Sum(a => a.PrivateMemorySize64);
		    return totalAllocatedMemoryInBytes;
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
		    return 0;
	    }
    }

    internal static long GetTotalMemoryInKb()
    {
	    try
	    {
		    // only parse the file once
		    if (totalMemoryInKb > 0)
		    {
			    return totalMemoryInKb;
		    }
	    
		    // ReSharper disable once StringLiteralTypo
		    const string path = "/proc/meminfo";
		    if (!File.Exists(path))
		    {
			    throw new FileNotFoundException($"File not found: {path}");
		    }
	    
		    using (var reader = new StreamReader(path))
		    {
			    string line;
			    while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
			    {
				    if (line.Contains("MemTotal", StringComparison.OrdinalIgnoreCase))
				    {
					    // e.g. MemTotal: 16370152 kB
					    var parts = line.Split(':');
					    var valuePart = parts[1].Trim();
					    parts = valuePart.Split(' ');
					    var numberString = parts[0].Trim();
				    
					    var result = long.TryParse(numberString, out totalMemoryInKb);
					    return result ? totalMemoryInKb : throw new Exception($"Cannot parse 'MemTotal' value from the file {path}.");
				    }
			    }
		    
			    throw new Exception($"Cannot find the 'MemTotal' property from the file {path}.");
		    }
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
		    return 0;
	    }
    }
}