using BluetoothLETest.Domain.Interfaces;
using BluetoothLETest.Helpers;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using System.Collections.ObjectModel;

namespace BluetoothLETest.Data;

public class BluetoothController : IBluetoothController
{

    public Guid ReaderServiceUUID => Guid.Parse("EAF2890B-F827-476A-93C9-AE8AC8EEB916");

    Guid CCCD = Guid.Parse("00002902-0000-1000-8000-00805f9b34fb");
    Guid ReaderCommandCharacteristic = Guid.Parse("6296DAF0-2004-40D3-B6E6-E0FDA0C4FDBC");
    Guid ReaderResponseCharacteristic = Guid.Parse("773CBFCD-EE10-4F45-BA9A-84D7A41E3707");

    IBluetoothLE bluetoothLE;
    IAdapter adapter;
    IDevice _device;
    ICharacteristic _readerCommandCharacteristic;
    ICharacteristic _readerResponseCharacteristic;

    public event EventHandler<byte[]> MessageSent;
    public event EventHandler<byte[]> MessageReceived;

    public BluetoothController(IBluetoothLE bluetoothLE, IAdapter adapter)
    {
        this.bluetoothLE = bluetoothLE;
        this.adapter = adapter;
    }

    public async Task RequestPermissionsAsync()
    {
        PermissionStatus bleStatus = PermissionStatus.Unknown;
        PermissionStatus locationStatus = PermissionStatus.Unknown;

        if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 12)
        {
            bleStatus = await Permissions.CheckStatusAsync<BluetoothPermissions>();

            if (bleStatus == PermissionStatus.Granted)
                return;

            if (Permissions.ShouldShowRationale<BluetoothPermissions>())
            {
                await Shell.Current.DisplayAlert("Needs Bluetooth Permissions", "Because it does!!", "OK");
            }

            bleStatus = await Permissions.RequestAsync<BluetoothPermissions>();
        }

        locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (locationStatus == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            return;

        if (locationStatus == PermissionStatus.Granted)
            return;

        if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
        {
            await Shell.Current.DisplayAlert("Needs Bluetooth Permissions", "Because it does!!", "OK");
        }

        locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        if (locationStatus != PermissionStatus.Granted)
        {
            await Shell.Current.DisplayAlert("Permission required", "Location permission is required for " +
                "bluetooth scanning." + "Location is never stored or used", "OK");
        }
    }

    public async Task StartScan(ObservableCollection<IDevice> devices)
    {
        await RequestPermissionsAsync();

        var bluetoothStatus = await Permissions.CheckStatusAsync<BluetoothPermissions>();
        var locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (bluetoothLE.State != BluetoothState.On)
        {
            await Shell.Current.DisplayAlert("Bluetooth Off", "Please enable Bluetooth to scan for devices.", "OK");
        }

        if(bluetoothStatus == PermissionStatus.Granted && locationStatus == PermissionStatus.Granted)
        {

            adapter.DeviceDiscovered += (s, a) =>
            {
                Console.WriteLine($"Discovered Device: {a.Device.Name}, ID: {a.Device.Id}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!devices.Any(d => d.Id == a.Device.Id))
                    {
                        devices.Add(a.Device);
                    }
                });
            };

            if (devices.Count != 0) 
                devices.Clear();

            /* Filter scan for specific UUID 
            var scanFilterOptions = new ScanFilterOptions();
            scanFilterOptions.ServiceUuids = new [] {ReaderServiceUUID}; // cross platform filter
            */

            var scanFilterOptions = new ScanFilterOptions
            {
                ServiceUuids = new[] { ReaderServiceUUID }
            };

            await Task.Run(async () =>
            {
                if (adapter.IsScanning)
                {
                    await adapter.StopScanningForDevicesAsync();
                }

                Console.WriteLine("Starting Bluetooth Scan...");
                await adapter.StartScanningForDevicesAsync(scanFilterOptions);
            });

        }
    }

    public async Task ConnectToDeviceAsync(IDevice device, CancellationToken cancellationToken)
    {
        List<ICharacteristic> characteristics = new();
        try
        {
            _device = await adapter.ConnectToKnownDeviceAsync(deviceGuid: device.Id, cancellationToken: cancellationToken);
        }
        catch(DeviceConnectionException ex) 
        {
            await Shell.Current.DisplayAlert("Connection Error", "Something went wrong with connection attempt.", "OK");
        }
    }
    public async Task SubscribeToNotificationsAsync(List<IDevice> devices, CancellationToken cancellationToken)
    {
        byte[] smonchBytes = new byte[] { 0x73, 0x6D, 0x6F, 0x6E, 0x63, 0x68 };

        await ConnectToDeviceAsync(devices[0], cancellationToken);

        if(_device.State == DeviceState.Connected)
        {
            var readerService = await _device.GetServiceAsync(ReaderServiceUUID);
            _readerCommandCharacteristic = await readerService.GetCharacteristicAsync(ReaderCommandCharacteristic); //send message to
            _readerResponseCharacteristic = await readerService.GetCharacteristicAsync(ReaderResponseCharacteristic);

            try
            {
                _readerResponseCharacteristic.ValueUpdated += (sender, args) =>
                {
                    var data = args.Characteristic.Value; // Get the updated value
                    MessageReceived?.Invoke(this, data);
                    Console.WriteLine($"Notification received: {BitConverter.ToString(data)}");
                };

                await _readerResponseCharacteristic.StartUpdatesAsync();
                Console.WriteLine("Subbed to notifs");
            }
            catch (Exception ex) 
            { 
                await Shell.Current.DisplayAlert("Failed to subscribe to notifications", $"{ ex.Message}", "OK" );
                throw;
            }
        }
    }
    public async Task SendMessageAsync(byte[] message)
    {
        try
        {
            await _readerCommandCharacteristic.WriteAsync(message);
            MessageSent?.Invoke(this, message); //Raise message sent event
            Console.WriteLine($"Message sent: {BitConverter.ToString(message)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while sending message: {ex.Message}");
            throw;
        }
    }
    private async Task OnDeviceDisconnected(object sender, IDevice device, ICharacteristic characteristic)
    {
        if (_device.Id == device.Id)
        {
            Console.WriteLine($"Device {_device.Name} disconnected. Stopping notifications.");
            await Shell.Current.DisplayAlert("Device ", $"{_device.Name}", $"{_device.Name} disconnected manually or unexpectedly.", "OK" );
            try
            {
                await characteristic?.StopUpdatesAsync();
                Console.WriteLine("Unsubscribed from notifications due to disconnection.");
                await Shell.Current.DisplayAlert("Unsubscribed from notifications due to disconnection", $"Unsubscribed", "OK" );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping updates: {ex.Message}");
                await Shell.Current.DisplayAlert("Update Error", $"{ex.Message}", "OK" );
            }
        }
    }

    public async Task<List<ICharacteristic>> GetDeviceCharacteristicsAsync(List<IDevice> devices, CancellationToken cancellationToken)
    {
        await ConnectToDeviceAsync(devices[0], cancellationToken);

        List<ICharacteristic> characteristicsList = new();

        if(_device.State == DeviceState.Connected)
        {
            var services = await _device.GetServicesAsync();

            foreach (var service in services) 
            {
                Console.WriteLine($"Service UUID: {service.Id}");
                var characteristics = await service.GetCharacteristicsAsync();

                foreach (var characteristic in characteristics)
                {
                    characteristicsList.Add(characteristic);
                    Console.WriteLine($"Characteristic UUID: {characteristic.Id}");
                    Console.WriteLine($"Properties: {characteristic.Properties}");
                }
            }
        }

        return characteristicsList;
    }
    public Task StopScan()
    {
        throw new NotImplementedException();
    }
}
