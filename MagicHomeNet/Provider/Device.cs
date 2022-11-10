using MagicHomeNet;
using MagicHomeNet.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace MagicHomeNet.Provider
{
    public abstract class Device
    {
        public readonly DeviceMetadata DeviceMetadata;

        private readonly SemaphoreSlim deviceConnectionChangesSemaphore = new SemaphoreSlim(1, 1);

        public Device(DeviceMetadata deviceMetadata)
        {
            DeviceMetadata = deviceMetadata;
        }

        public bool IsConnected { get; internal set; }
        public bool IsTurnedOn { get; internal set; }

        public async Task<ConnectionAlterResult> Connect()
        {
            var isFree = await deviceConnectionChangesSemaphore.WaitAsync(0);

            if (!isFree)
            {
                return ConnectionAlterResult.Busy;
            }

            bool didSucceed;

            if (IsConnected)
            {
                didSucceed = true;
            }
            else
            {
                try
                {
                    // TODO - Move to a CancellationToken mechanism and enforce receiving CancellationTokens
                    // in all ConnectInternal methods.
                    var connectTimeoutSpan = TimeSpan.FromSeconds(10);
                    await ConnectInternal().TimeoutAfter(connectTimeoutSpan, $"Failed to connect to device {DeviceMetadata.DeviceName} with guid {DeviceMetadata.RgbMasterDeviceGuid}").ConfigureAwait(false);

                    IsConnected = true;
                    didSucceed = true;
                }
                catch (Exception ex)
                {


                    IsConnected = false;
                    didSucceed = false;
                }
            }

            deviceConnectionChangesSemaphore.Release();

            return didSucceed ? ConnectionAlterResult.Succeeded : ConnectionAlterResult.Failed;
        }

        public async Task<ConnectionAlterResult> Disconnect()
        {
            var isFree = await deviceConnectionChangesSemaphore.WaitAsync(0);

            if (!isFree)
            {
                return ConnectionAlterResult.Busy;
            }

            bool didSucceed;

            if (!IsConnected)
            {
                didSucceed = true;
            }
            else
            {
                try
                {
                    // TODO - Move to a cancellationtoken mechanism and enforce receiving cancellationtokens
                    // in all DisconnectInternal methods.
                    var disconnectTimeoutSpan = TimeSpan.FromSeconds(10);
                    await DisconnectInternal().TimeoutAfter(disconnectTimeoutSpan, $"Failed to disconnect from device {DeviceMetadata.DeviceName} with guid {DeviceMetadata.RgbMasterDeviceGuid}").ConfigureAwait(false);

                    IsConnected = false;
                    didSucceed = true;
                }
                catch (Exception ex)
                {

                    IsConnected = false;
                    didSucceed = false;
                }
            }

            deviceConnectionChangesSemaphore.Release();

            return didSucceed ? ConnectionAlterResult.Succeeded : ConnectionAlterResult.Failed;
        }

        protected abstract Task ConnectInternal();
        protected abstract Task DisconnectInternal();

        public async Task TurnOn()
        {
            await TurnOnInternal();
            IsTurnedOn = true;
        }

        public async Task TurnOff()
        {
            await TurnOffInternal();
            IsTurnedOn = false;
        }

        protected abstract Task TurnOnInternal();
        protected abstract Task TurnOffInternal();

        public async Task SetPowerAsync(bool power)
        {
            if (power)
            {
                await TurnOn();
            }
            else await TurnOff();
        }


        public async Task<Color> GetColor()
        {
            return await GetColorInternal();
        }

        protected abstract Task<Color> GetColorInternal();

        public async Task SetColor(Color color)
        {
            await SetColorInternal(color);
        }

        protected abstract Task SetColorInternal(Color color);

        public async Task SetColorSmoothly(Color color, int relativeSmoothness)
        {
            await SetColorSmoothlyInternal(color, relativeSmoothness);
        }

        protected abstract Task SetColorSmoothlyInternal(Color color, int relativeSmoothness);

        public async Task<byte> GetBrightnessPercentage()
        {
            return await GetBrightnessPercentageInternal();
        }

        protected abstract Task<byte> GetBrightnessPercentageInternal();

        public async Task SetBrightnessPercentage(byte brightness)
        {
            await SetBrightnessPercentageInternal(brightness);
        }

        protected abstract Task SetBrightnessPercentageInternal(byte brightness);



        public async Task SetPresetPattern(PresetPattern pattern, double speed)
        {
            await SetPresetPatternInternal(pattern, speed);
        }

        protected abstract Task SetPresetPatternInternal(PresetPattern pattern, double speed);



        public async Task SetCustomPattern(Color[] colors, TransitionType transition, double speed)
        {
            await SetCustomPatternInternal(colors, transition, speed);
        }

        protected abstract Task SetCustomPatternInternal(Color[] colors, TransitionType transition, double speed);

    }
}
