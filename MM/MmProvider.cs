using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.WMI;

namespace OpenHardwareMonitor.MM {
  class MmProvider {
    private MemoryMappedFile memoryMappedFile;
    private MemoryMappedViewAccessor memoryAccessor;
    private readonly int mmBufferSize = 1 * 1024 * 1024;
    private IntPtr mmBuffer;
    private IntPtr dataPtr;

    private List<IMmObject> activeInstances;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct TestData {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string Name;
    }

    public MmProvider(IComputer computer) {
      memoryMappedFile = MemoryMappedFile.CreateOrOpen("Global\\OpenHardwareMonitor.mm", mmBufferSize);
      memoryAccessor = memoryMappedFile.CreateViewAccessor();
      mmBuffer = Marshal.AllocHGlobal(mmBufferSize);

      dataPtr = mmBuffer + Marshal.SizeOf(typeof(int));

      activeInstances = new List<IMmObject>();

      foreach (IHardware hardware in computer.Hardware)
        ComputerHardwareAdded(hardware);

      computer.HardwareAdded += ComputerHardwareAdded;
      computer.HardwareRemoved += ComputerHardwareRemoved;
    }

    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    private unsafe void WriteBytes(int totalBytes) {
      byte* ptr = null;
      memoryAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
      IntPtr intPtr = new IntPtr(ptr);
      CopyMemory(intPtr, mmBuffer, (uint)totalBytes);
      memoryAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
      memoryAccessor.Flush();
    }

    public void Update() {
      foreach (IMmObject instance in activeInstances)
        instance.Update();

      IntPtr ptr = dataPtr;
      int totalBytes = 0;
      foreach (IMmObject instance in activeInstances) {
        int writtenBytes = instance.MarshalToBuffer(ptr);
        ptr += writtenBytes;
        totalBytes += writtenBytes;
        Debug.Assert(totalBytes < mmBufferSize);
      }
      Marshal.WriteInt32(mmBuffer, totalBytes);
      WriteBytes(totalBytes);
    }

    private void ComputerHardwareAdded(IHardware hardware) {
      if (!Exists(hardware.Identifier.ToString())) {
        foreach (ISensor sensor in hardware.Sensors)
          HardwareSensorAdded(sensor);

        hardware.SensorAdded += HardwareSensorAdded;
        hardware.SensorRemoved += HardwareSensorRemoved;

        Hardware hw = new Hardware(hardware);
        activeInstances.Add(hw);
      }

      foreach (IHardware subHardware in hardware.SubHardware)
        ComputerHardwareAdded(subHardware);
    }

    private void HardwareSensorAdded(ISensor data) {
      Sensor sensor = new Sensor(data);
      activeInstances.Add(sensor);
    }

    private void ComputerHardwareRemoved(IHardware hardware) {
      hardware.SensorAdded -= HardwareSensorAdded;
      hardware.SensorRemoved -= HardwareSensorRemoved;
      
      foreach (ISensor sensor in hardware.Sensors) 
        HardwareSensorRemoved(sensor);
      
      foreach (IHardware subHardware in hardware.SubHardware)
        ComputerHardwareRemoved(subHardware);

      RevokeInstance(hardware.Identifier.ToString());
    }

    private void HardwareSensorRemoved(ISensor sensor) {
      RevokeInstance(sensor.Identifier.ToString());
    }

    private bool Exists(string identifier) {
      return activeInstances.Exists(h => h.Identifier == identifier);
    }

    private void RevokeInstance(string identifier) {
      int instanceIndex = activeInstances.FindIndex(
        item => item.Identifier == identifier.ToString()
      );

      if (instanceIndex == -1)
        return;

      activeInstances.RemoveAt(instanceIndex);
    }

    public void Dispose() {
      memoryAccessor.Write(0, 0);
      memoryAccessor.Flush();

      memoryAccessor.Dispose();
      memoryMappedFile.Dispose();
    }
  }
}
