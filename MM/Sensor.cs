using System;
using System.Runtime.InteropServices;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.MM {
  class Sensor : IMmObject {
    private ISensor sensor;

    public string SensorType { get; private set; }
    public string Identifier { get; private set; }
    public string Parent { get; private set; }
    public string Name { get; private set; }
    public float Value { get; private set; }
    public float Min { get; private set; }
    public float Max { get; private set; }
    public int Index { get; private set; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MarshalStruct {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string SensorType;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Identifier;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Parent;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
      public string Name;
      public float Value;
      public float Min;
      public float Max;
      public int Index;
    }

    private MarshalStruct marshalData;

    public Sensor(ISensor sensor) {
      Name = sensor.Name;
      Index = sensor.Index;

      SensorType = sensor.SensorType.ToString();
      Identifier = sensor.Identifier.ToString();
      Parent = sensor.Hardware.Identifier.ToString();

      this.sensor = sensor;

      marshalData.SensorType = SensorType;
      marshalData.Identifier = Identifier;
      marshalData.Parent = Parent;
      marshalData.Name = Name;
      marshalData.Index = Index;
    }
    
    public void Update() {
      Value = (sensor.Value != null) ? (float)sensor.Value : 0;

      if (sensor.Min != null)
        Min = (float)sensor.Min;

      if (sensor.Max != null)
        Max = (float)sensor.Max;

      marshalData.Value = Value;
      marshalData.Min = Min;
      marshalData.Max = Max;
    }

    public int MarshalToBuffer(IntPtr buffer) {
      IntPtr ptr = buffer;
      int structType = 1;
      int dataSize = Marshal.SizeOf(typeof(int));
      Marshal.WriteInt32(buffer, 0, structType);
      ptr += dataSize;
      dataSize += Marshal.SizeOf(typeof(MarshalStruct));
      Marshal.StructureToPtr(marshalData, ptr, false);
      return dataSize;
    }
  }
}
