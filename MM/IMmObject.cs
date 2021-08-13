using System;

namespace OpenHardwareMonitor.MM {
  interface IMmObject {
    string Name { get; }
    string Identifier { get; }

    // Not exposed.
    void Update();

    int MarshalToBuffer(IntPtr buffer);
  }
}
