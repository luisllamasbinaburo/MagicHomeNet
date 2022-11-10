using MagicHomeNet.Common;
using MagicHomeNet.Provider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Utils;

namespace MagicHomeNet
{
    public class MagicHomeDevice : Device
    {
        private const int defaultMagicHomePort = 5577;

        private readonly string LightIp;

        private Socket InternalLightSocket;
        private LedProtocol MagicHomeProtocol;
        private bool shouldUseCsum;

        public ConnectionStatus ConnectionStatus { get; private set; }

        public byte WarmWhite { get; private set; }
        public Color Color { get; private set; }
        public LightMode Mode { get; private set; }
        public DateTime Time { get; private set; }
        public byte Brightness { get; private set; }

        public MagicHomeDevice(string lightIp, MagicHomeDeviceMetadata magicHomeDeviceMetadata) : base(magicHomeDeviceMetadata)
        {
            LightIp = lightIp;
            shouldUseCsum = true;
        }

        protected override async Task ConnectInternal()
        {
            ConnectionStatus = ConnectionStatus.Connecting;
            InternalLightSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = 1000,
                SendTimeout = 1000
            };

            await InternalLightSocket.ConnectAsync(IPAddress.Parse(LightIp), defaultMagicHomePort).ConfigureAwait(false);

            MagicHomeProtocol = await GetMagicHomeProtocol().ConfigureAwait(false);

            shouldUseCsum = MagicHomeProtocol == LedProtocol.LEDENET;
            ConnectionStatus = ConnectionStatus.Connected;
        }

        protected override Task DisconnectInternal()
        {
            ConnectionStatus = ConnectionStatus.Disconnecting;
            InternalLightSocket.Close();
            InternalLightSocket.Dispose();
            ConnectionStatus = ConnectionStatus.Disconnected;

            return Task.CompletedTask;
        }

        protected override async Task TurnOnInternal()
        {
            if (MagicHomeProtocol == LedProtocol.LEDENET)
            {
                await TrySendDataToDevice(0x71, 0x23, 0x0f).ConfigureAwait(false);
            }
            else
            {
                await TrySendDataToDevice(0xcc, 0x23, 0x33).ConfigureAwait(false);
            }
        }

        protected override async Task TurnOffInternal()
        {
            if (MagicHomeProtocol == LedProtocol.LEDENET)
            {
                await TrySendDataToDevice(0x71, 0x24, 0x0f).ConfigureAwait(false);
            }
            else
            {
                await TrySendDataToDevice(0xcc, 0x24, 0x33).ConfigureAwait(false);
            }
        }


        protected override async Task SetColorInternal(System.Drawing.Color color)
        {
            if (MagicHomeProtocol == LedProtocol.LEDENET)
            {
                await TrySendDataToDevice(0x41, color.R, color.G, color.B, 0x00, 0x00, 0x0f).ConfigureAwait(false);
            }
            else
            {
                await TrySendDataToDevice(0x56, color.R, color.G, color.B, 0xaa).ConfigureAwait(false);
            }
        }

        protected override async Task<Color> GetColorInternal()
        {
            await GetStatus();
            return Color;
        }

        protected override async Task<byte> GetBrightnessPercentageInternal()
        {
            await GetStatus();
            return Brightness;
        }

        protected override async Task SetBrightnessPercentageInternal(byte brightness)
        {
            await SetWhiteColor(brightness);
        }

        public async Task SetBrightnessAsync(byte brightness)
        {
            if (brightness > 100) brightness = 100;

            if (Mode == LightMode.Color)
                await SetColorInternal(Color.FromArgb(
                    Convert.ToByte(Color.R * (brightness / Brightness)),
                    Convert.ToByte(Color.G * (brightness / Brightness)),
                    Convert.ToByte(Color.B * (brightness / Brightness))
                    ));
            else if (Mode == LightMode.WarmWhite)
                await SetWhiteColor(Convert.ToByte(WarmWhite * brightness / Brightness));
            UpdateBrightness();
        }

        /// <summary> Sets warm white. If the light does not support warm white, will set cold white instead. </summary>
        public async Task SetWhiteColor(byte white)
        {
            if (MagicHomeProtocol == LedProtocol.LEDENET)
                await TrySendDataToDevice(0x31, 0, 0, 0, white, 0x0f, 0x0f);
            else
                await SetColdWhite(white);

            //Populate field
            WarmWhite = white;
            Color = Color.Black;
            UpdateBrightness();
        }

        /// <summary> Sets all the red, green and blue color to the same value to create cold white light. </summary>
        protected async Task SetColdWhite(byte white) => await SetColor(Color.FromArgb(white, white, white));


        protected override async Task SetColorSmoothlyInternal(System.Drawing.Color color, int relativeSmoothness)
        {
            List<byte> data = new List<byte>() { 0x51, color.R, color.G, color.B };

            for (int i = 0; i < 16 - 1; i++)
            {
                data.AddRange(new byte[] { 0, 1, 2, 3 });
            }

            data.AddRange(new byte[] { 0x00, SpeedToSmoothDelay(relativeSmoothness), Convert.ToByte(0x3a), 0xff, 0x0f });

            byte[] dataReady = data.ToArray();
            await TrySendDataToDevice(dataReady);
        }

        private byte SpeedToSmoothDelay(double speed)
        {
            // Speed 31, is approximately 2 second, the fastest option possible. We consider that 100%.
            // Speed 1, is approximately 60 seconds, the slowest option possible. We consider that 1%.
            // These are our boundries.
            // We base our calculation according to these assumptions.
            // We consider every speed point ~2 seconds (just a bit longer maybe).

            double boundSpeed;

            if (speed < 2000)
            {
                boundSpeed = 2000;
            }
            else if (speed > 62000)
            {
                boundSpeed = 62000;
            }
            else
            {
                boundSpeed = speed;
            }

            var estimatedSpeedPoints = 31 - (int)Math.Round(boundSpeed / 2000.0, MidpointRounding.AwayFromZero);
            var estimatedSpeedPercentage = (estimatedSpeedPoints / 31.0) * 100;

            var invertedSpeedPercentage = 100 - estimatedSpeedPercentage;

            byte delay = Convert.ToByte((invertedSpeedPercentage * (0x1f - 1)) / 100);
            delay += 1;

            return delay;
        }

        protected override async Task SetPresetPatternInternal(PresetPattern pattern, double speed)
        {
            byte byte_speed = SpeedToByte(speed);
            await TrySendDataToDevice(0x61, Convert.ToByte(pattern), byte_speed, 0x0f).ConfigureAwait(false);
            Mode = LightMode.Preset;
        }

        //0x01 is the fastest, 0xFF is the slowest.
        private static byte SpeedToByte(double speed)
        {
            if (speed > 1.0) speed = 1.0f;
            if (speed < 0.0) speed = 1.0f;

            byte byte_speed = Convert.ToByte(0xFF - speed * 254);
            return byte_speed;
        }
      
        /// <summary> 
        /// Sets the light a custom pattern.
        /// Use an array of Color objects to assign a list of colors the light will cycle through.
        /// </summary>
        /// <param name="colors"> The array of colors that the light will cycle through. </param>
        /// <param name="transition"> The transition type (Gradual, Strobe, Jump). </param>
        /// <param name="speed"> How quick the light will cycle through the pattern, from 0 to 100. </param>
        protected override async Task SetCustomPatternInternal(Color[] colors, TransitionType transition, double speed)
        {
            List<byte> data = new List<byte>();
            bool firstbyte = true;

            for (int i = 0; i < colors.Length; i++)
            {
                if (firstbyte == true)
                {
                    data.Add(0x51);
                    firstbyte = false;
                }
                else
                    data.Add(0);

                data.AddRange(new byte[] { colors[i].R, colors[i].G, colors[i].B });
            }

            for (int i = 0; i < 16 - colors.Length; i++)
                data.AddRange(new byte[] { 0, 1, 2, 3 });

            data.AddRange(new byte[] { 0x00, SpeedToSmoothDelay(speed), Convert.ToByte(transition), 0xff, 0x0f });

            byte[] dataReady = data.ToArray();
            await TrySendDataToDevice(dataReady);

            //Populate field.
            Mode = LightMode.Custom;
        }


        private async Task<LedProtocol> GetMagicHomeProtocol()
        {
            var sentSuccessfully = await TrySendDataToDevice(0x81, 0x8a, 0x8b);

            if (!sentSuccessfully)
            {
                throw new TimeoutException("The request for magic home protocol of LEDENET received a timeout");
            }

            var lednetReceiveAttempt = await TryReceiveData(TimeSpan.FromSeconds(1));

            if (lednetReceiveAttempt.Item1)
            {
                return LedProtocol.LEDENET;
            }

            sentSuccessfully = await TrySendDataToDevice(0xef, 0x01, 0x77);

            if (!sentSuccessfully)
            {
                throw new TimeoutException("The request for magic home protocol of LEDENET_ORIGINAL received a timeout");
            }

            var lednetOriginalReceiveAttempt = await TryReceiveData(TimeSpan.FromSeconds(1));

            if (lednetOriginalReceiveAttempt.Item1)
            {
                return LedProtocol.LEDENET_ORIGINAL;
            }

            return LedProtocol.Unknown;
        }


        /// <summary> Sends a request to get the light's status.
        /// Updates this instance with current bulbs's mode, time, status, protocol, color, brightness.
        /// 
        /// This operation usually takes between 80 and 500 milliseconds.
        /// </summary>
        public async Task GetStatus()
        {
            //Send request for status.
            if (MagicHomeProtocol == LedProtocol.LEDENET)
                await TrySendDataToDevice(0x81, 0x8a, 0x8b);
            else if (MagicHomeProtocol == LedProtocol.LEDENET_ORIGINAL)
                await TrySendDataToDevice(0xef, 0x01, 0x77);

            //Read and process the response.
            var read = await TryReadDataFromDevice();

            if (read.success == false) return;

            byte[] dataRaw = read.buffer;
            string[] dataHex = new string[14];
            for (int i = 0; i < dataHex.Length; i++)
                dataHex[i] = dataRaw[i].ToString("X");

            //Check if it uses checksum.
            if (MagicHomeProtocol == LedProtocol.LEDENET_ORIGINAL)
                if (dataHex[1] == "01")
                    shouldUseCsum = false;

            //Check power state.
            if (dataHex[2] == "23")
                IsTurnedOn = true;
            else if (dataHex[2] == "24")
                IsTurnedOn = false;

            //Check light mode.
            Mode = DetermineMode(dataHex[3], dataHex[9]);

            //Handle color property.
            switch (Mode)
            {
                case LightMode.Color:
                    Color = System.Drawing.Color.FromArgb(dataRaw[6], dataRaw[7], dataRaw[8]);
                    WarmWhite = 0;
                    break;
                case LightMode.WarmWhite:
                    Color = Color.Black;
                    WarmWhite = dataRaw[9];
                    break;
                case LightMode.Preset:
                case LightMode.Unknown:
                case LightMode.Custom:
                    Color = Color.Black;
                    WarmWhite = 0;
                    break;
            }

            UpdateBrightness();

            //Send request to get the time of the light.
            Time = await GetTimeAsync();
        }


        /// <summary> Determines the mode of the light according to a code given by the light. </summary>
        /// <returns> Mode of the light. </returns>
        internal static LightMode DetermineMode(string patternCode, string whiteCode)
        {
            switch (patternCode)
            {
                case "60":
                    return LightMode.Custom;
                case "41":
                case "61":
                case "62":
                    if (whiteCode == "0")
                        return LightMode.Color;
                    else return LightMode.WarmWhite;
                case "2a":
                case "2b":
                case "2c":
                case "2d":
                case "2e":
                case "2f":
                    return LightMode.Preset;
            }

            if (int.TryParse(patternCode, out int result))
            {
                if (result >= 25 && result <= 38)
                {
                    return LightMode.Preset;
                }
            }

            return LightMode.Unknown;
        }



        /// <summary> Sets the date and time of the light. Leave null for the current system date (DateTime.Now). </summary>
        public async Task SetTimeAsync(DateTime? dateTime = null)
        {
            if (dateTime == null)
                dateTime = DateTime.Now;

            await TrySendDataToDevice(0x10, 0x14,
                Convert.ToByte(dateTime.Value.Year - 2000),
                Convert.ToByte(dateTime.Value.Month),
                Convert.ToByte(dateTime.Value.Day),
                Convert.ToByte(dateTime.Value.Hour),
                Convert.ToByte(dateTime.Value.Minute),
                Convert.ToByte(dateTime.Value.Second),
                Convert.ToByte(dateTime.Value.DayOfWeek),
                0x00, 0x0f);
        }

        /// <summary> Gets the time of the light. </summary>
        public async Task<DateTime> GetTimeAsync()
        {
            await TrySendDataToDevice(0x11, 0x1a, 0x1b, 0x0f);
            var rst = await TryReadDataFromDevice();
            if (rst.success)
            {
                var data = rst.buffer;
                Time = new DateTime(
                    Convert.ToInt32(data[3]) + 2000,
                    Convert.ToInt32(data[4]),
                    Convert.ToInt32(data[5]),
                    Convert.ToInt32(data[6]),
                    Convert.ToInt32(data[7]),
                    Convert.ToInt32(data[8])
               );
            }

            return Time;
        }

        /// <summary> Updates the brightness property of this instance based on it's colors or warm white value. </summary>
        private void UpdateBrightness()
        {
            if (Mode == LightMode.Color)
                Brightness = DetermineBrightness(Color.R, Color.G, Color.B);
            else if (Mode == LightMode.WarmWhite)
                Brightness = DetermineBrightness(WarmWhite, WarmWhite, WarmWhite);
        }

        /// <summary> Determines brightness by red, green and blue color values. </summary>
        /// <returns> Brightness level from 0 to 100. </returns>
        internal static byte DetermineBrightness(byte Red, byte Green, byte Blue)
        {
            int maxx = 0;

            if (Red > maxx) maxx = Red;
            if (Green > maxx) maxx = Green;
            if (Blue > maxx) maxx = Blue;

            maxx = maxx * 100 / 255;

            var brightness = Convert.ToByte(maxx);

            return brightness;
        }


        private async Task<Tuple<bool, byte[]>> TryReceiveData(TimeSpan? timeout = null, int attemptsCount = 1)
        {
            bool didSucceed = false;
            byte[] buffer = new byte[14];

            while (attemptsCount > 0)
            {
                try
                {
                    await InternalLightSocket.ReceiveAsync(
                        buffer,
                        SocketFlags.None,
                        timeout != null ? new CancellationTokenSource(timeout.Value).Token : default
                        );

                    didSucceed = true;
                    break;
                }
                catch (TaskCanceledException)
                {
                    attemptsCount -= 1;
                }
            }

            if (!didSucceed)
            {
                buffer = null;
            }

            return new Tuple<bool, byte[]>(didSucceed, buffer);
        }

        private async Task<bool> TrySendDataToDevice(params byte[] dataToSend)
        {
            return await TrySendDataToDevice(null, 1, dataToSend.ToArray());
        }

        private async Task<bool> TrySendDataToDevice(TimeSpan timeout, byte[] dataToSend)
        {
            return await TrySendDataToDevice(timeout, 1, dataToSend.ToArray());
        }

        private async Task<bool> TrySendDataToDevice(int attemptsCount, byte[] dataToSend)
        {
            return await TrySendDataToDevice(null, attemptsCount, dataToSend.ToArray());
        }

        private async Task<bool> TrySendDataToDevice(TimeSpan? timeout, int attemptsCount, byte[] dataToSend)
        {
            List<byte> finalSentData = new List<byte>(dataToSend);

            if (shouldUseCsum)
            {
                var csum = dataToSend.Aggregate((byte)0, (total, nextByte) => (byte)(total + nextByte));
                csum = (byte)(csum & 0xFF);

                finalSentData.Add(csum);
            }

            bool didSucceed = false;

            while (attemptsCount > 0)
            {
                try
                {
                    await InternalLightSocket.SendAsync(
                        finalSentData.ToArray(),
                        SocketFlags.None,
                        timeout != null ? new CancellationTokenSource(timeout.Value).Token : default
                        );
                    didSucceed = true;
                    break;
                }
                catch (TaskCanceledException)
                {
                    attemptsCount -= 1;
                }
            }

            return didSucceed;
        }

        private async Task<(bool success, byte[] buffer)> TryReadDataFromDevice(params byte[] dataToSend)
        {
            return await TryReadDataFromDevice(null, 1);
        }

        private async Task<(bool success, byte[] buffer)> TryReadDataFromDevice(TimeSpan? timeout, int attemptsCount)
        {
            byte[] buffer = new byte[14];
            var finalSentData = new ArraySegment<byte>(buffer);

            bool didSucceed = false;
            while (attemptsCount > 0)
            {
                try
                {
                    await InternalLightSocket.ReceiveAsync(
                        finalSentData,
                        SocketFlags.None,
                        timeout != null ? new CancellationTokenSource(timeout.Value).Token : default
                        );
                    didSucceed = true;
                    break;
                }
                catch (TaskCanceledException)
                {
                    attemptsCount -= 1;
                }
            }

            return (didSucceed, buffer);
        }
    }
}
