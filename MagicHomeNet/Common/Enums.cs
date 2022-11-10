using System;
using System.Collections.Generic;
using System.Text;

namespace MagicHomeNet.Common
{
    public enum LedProtocol { Unknown = 0, LEDENET = 1, LEDENET_ORIGINAL = 2 }

    public enum TransitionType { Gradual = 0x3a, Strobe = 0x3c, Jump = 0x3b }
    public enum LightMode { Color, WarmWhite, Preset, Custom, Unknown }
    public enum ConnectionAlterResult { Busy = 1, Succeeded = 2, Failed = 3 }
    public enum DeviceInterfaceType { WiFi = 1, BLE = 2, Physical = 3 }

    public enum PresetPattern
    {
        SevenColorsCrossFade = 0x25,
        RedGradualChange = 0x26,
        GreenGradualChange = 0x27,
        BlueGradualChange = 0x28,
        YellowGradualChange = 0x29,
        CyanGradualChange = 0x2a,
        PurpleGradualChange = 0x2b,
        WhiteGradualChange = 0x2c,
        RedGreenCrossFade = 0x2d,
        RedBlueCrossFade = 0x2e,
        GreenBlueCrossFade = 0x2f,
        SevenColorStrobeFlash = 0x30,
        RedStrobeFlash = 0x31,
        GreenStrobeFlash = 0x32,
        BlueStrobeFlash = 0x33,
        YellowStrobeFlash = 0x34,
        CyanStrobeFlash = 0x35,
        PurpleStrobeFlash = 0x36,
        WhiteStrobeFlash = 0x37,
        SevenColorsJumping = 0x38
    }

    public enum DeviceType
    {
        Unknown,
        Lightbulb,
        LedStrip,
        Keyboard,
        Mouse,
        Fan,
        Mousepad,
        Speaker,
        Headset,
        Keypad,
        Memory,
        GPU,
        Motherboard,
        Chair,
        AllDevices,
    }  

    public enum OperationType
    {
        GetColor,
        SetColor,
        SetColorSmoothly,
        GetBrightness,
        SetBrightness,
        TurnOn,
        TurnOff,
    }
}
