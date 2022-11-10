using MagicHomeNet;
using MagicHomeNet.Common;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace MagicHomeNet
{
    public class MagicHomeDeviceMetadata : DeviceMetadata
    {
        public MagicHomeDeviceMetadata(Guid discoveringProvider, string deviceName) : base(discoveringProvider, DeviceType.Lightbulb, deviceName, new HashSet<OperationType>() { OperationType.SetColor, OperationType.TurnOn, OperationType.TurnOff, OperationType.SetColorSmoothly /*, OperationType.SetBrightness*/ })
        {

        }
    }
}