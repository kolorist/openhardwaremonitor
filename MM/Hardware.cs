using System;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.MM {
  class Hardware : IMmObject {
    public string HardwareType { get; private set; }
    public string Identifier { get; private set; }
    public string Name { get; private set; }
    public string Parent { get; private set; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MarshalStruct {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string HardwareType;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Identifier;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Name;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Parent;
    }

    private MarshalStruct marshalData;

    public Hardware(IHardware hardware) {
      Name = hardware.Name;
      Identifier = hardware.Identifier.ToString();
      HardwareType = hardware.HardwareType.ToString();
      Parent = (hardware.Parent != null)
        ? hardware.Parent.Identifier.ToString()
        : "";

      marshalData.HardwareType = HardwareType;
      marshalData.Identifier = Identifier;
      marshalData.Name = Name;
      marshalData.Parent = Parent;
    }

    public void Update() { }

    public int MarshalToBuffer(IntPtr buffer) {
      IntPtr ptr = buffer;
      int structType = 0;
      int dataSize = Marshal.SizeOf(typeof(int));
      Marshal.WriteInt32(buffer, 0, structType);
      ptr += dataSize;
      dataSize += Marshal.SizeOf(typeof(MarshalStruct));
      Marshal.StructureToPtr(marshalData, ptr, false);
      return dataSize;
    }
  }
}
