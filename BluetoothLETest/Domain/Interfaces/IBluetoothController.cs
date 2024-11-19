using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothLETest.Domain.Interfaces;

public interface IBluetoothController
{
    event EventHandler<byte[]> MessageSent;
    event EventHandler<byte[]> MessageReceived;


    public Guid ReaderServiceUUID { get; }
    Task RequestPermissionsAsync();
    public Task StartScan(ObservableCollection<IDevice> devices);
    public Task StopScan();
    public Task ConnectToDeviceAsync(IDevice device, CancellationToken cancellationToken);
    public Task<List<ICharacteristic>> GetDeviceCharacteristicsAsync(List<IDevice> devices, CancellationToken cancellationToken);
    public Task SubscribeToNotificationsAsync(List<IDevice> devices, CancellationToken cancellationToken);
    Task SendMessageAsync(byte[] message);
}
