using MagicHomeNet;
using MagicHomeNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace MagicHomeNet.Provider
{
    public abstract class BaseProvider
    {
        private List<Device> lastDiscoveredDevices;

        public List<Device> LastDiscoveredDevices => lastDiscoveredDevices;

        public readonly ProviderMetadata ProviderMetadata;
        public bool IsRegistered { get; private set; }

        public async Task<(bool didSuccess, List<Device> devices)> Discover()
        {
            var discoverTimeoutSpan = TimeSpan.FromSeconds(5);

            var cancellationToken = new CancellationTokenSource(discoverTimeoutSpan).Token;

            List<Device> devices = null;

            try
            {
                devices = await InternalDiscover(cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to discover devices for provider {ProviderMetadata.ProviderName} with GUID {ProviderMetadata.ProviderGuid}.");
               
                return (false, devices);
            }

            return (true, devices);
        }

        protected abstract Task<List<Device>> InternalDiscover(CancellationToken cancellationToken = default);

        public async Task<bool> Register()
        {
            try
            {
                if (!IsRegistered)
                {
                    var registerTimeoutSpan = TimeSpan.FromSeconds(5);

                    var cancellationToken = new CancellationTokenSource(registerTimeoutSpan).Token;

                    await InternalRegister(cancellationToken).TimeoutAfter(registerTimeoutSpan, $"Failed to register to provider {ProviderMetadata.ProviderName} with guid {ProviderMetadata.ProviderGuid}").ConfigureAwait(false);
                    IsRegistered = true;
                }

                return IsRegistered;
            }
            catch (Exception ex)
            {
               
                Debug.WriteLine($"Failed registering provider with GUID {ProviderMetadata.ProviderGuid}.");
                return false;
            }
        }

        public async Task<bool> Unregister()
        {
            try
            {
                if (IsRegistered)
                {
                    var unregisterTimeoutSpan = TimeSpan.FromSeconds(5);

                    var cancellationToken = new CancellationTokenSource(unregisterTimeoutSpan).Token;

                    await InternalUnregister(cancellationToken).TimeoutAfter(unregisterTimeoutSpan, $"Failed to unregister from provider {ProviderMetadata.ProviderName} with guid {ProviderMetadata.ProviderGuid}").ConfigureAwait(false);
                    IsRegistered = false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to unregister provider with GUID {ProviderMetadata.ProviderGuid}.");
                return false;
            }
        }

        public BaseProvider(ProviderMetadata providerMetadata)
        {
            ProviderMetadata = providerMetadata;
        }

        protected abstract Task InternalRegister(CancellationToken cancellationToken = default);
        protected abstract Task InternalUnregister(CancellationToken cancellationToken = default);
    }
}
