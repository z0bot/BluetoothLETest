using BluetoothLETest.Domain.Interfaces;
using BluetoothLETest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

namespace BluetoothLETest.Presentation.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private IBluetoothController bluetoothControllerInterface;
    private IGeolocation geoLocation;
    private CancellationTokenSource? cancellationTokenSource;

    public ObservableCollection<IDevice> Devices { get; set; } = new();
    public ObservableCollection<ICharacteristic> Characteristics { get; set; } = new();
    public ObservableCollection<Message> Messages { get; set; } = new();

    [ObservableProperty]
    private Message currentMessage = new Message { Type = Message.MessageType.Sent, Body = "" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnectNotVisible))]
    private bool isConnectVisible;
    public bool IsConnectNotVisible => !IsConnectVisible;

    private string notificationStatus = "Not Subscribed";
    public string NotificationStatus
    {
        get => notificationStatus;
        set => SetProperty(ref notificationStatus, value);
    }

    public MainViewModel(IBluetoothController bluetoothControllerInterface, IGeolocation geoLocation)
    {
        this.bluetoothControllerInterface = bluetoothControllerInterface;
        this.geoLocation = geoLocation;
        this.bluetoothControllerInterface.MessageReceived += OnMessageReceived;
        this.bluetoothControllerInterface.MessageSent += OnMessageSent;
    }

    [RelayCommand]
    public async Task ScanDevices()
    {
        Devices.Clear();
        try
        {
            IsBusy = true;
            var location = await geoLocation.GetLocationAsync();
            await bluetoothControllerInterface.StartScan(Devices);
        }

        catch (Exception ex)
        {
            IsConnectVisible = false;
            await Shell.Current.DisplayAlert("Error", $"{ex.Message}", "OK");
        }
        finally 
        {
            IsBusy = false;
            if (Devices.Any()) { IsConnectVisible = true; }
        }
    }
    [RelayCommand]
    public async Task SubscribeToNotifications()
    {
        cancellationTokenSource = new CancellationTokenSource();
        try
        {
            NotificationStatus = "Successfully Subsribed to Notifications";
            await Task.Run(async () =>
            {
                await bluetoothControllerInterface.SubscribeToNotificationsAsync(Devices.ToList(), cancellationTokenSource.Token);
            });

            /*
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NotificationStatus = "Successfully Subsribed to Notifications";
            });
            */
        }
        catch(OperationCanceledException)
        {
            await Shell.Current.DisplayAlert("Connection Cancelled", "The operation was cancelled. Please try again", "OK");
            NotificationStatus = "Subsription Cancelled";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to subscribe to notifications: {ex.Message}", "OK");
            NotificationStatus = "Subscription Failed";
        }
        finally
        {
            IsBusy = false;
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    public async Task GetDeviceCharacteristics()
    {
        cancellationTokenSource = new CancellationTokenSource();
        List<ICharacteristic> characteristics = new();

        try
        {
            characteristics = await bluetoothControllerInterface.GetDeviceCharacteristicsAsync(Devices.ToList(), cancellationTokenSource.Token);
            foreach(var characteristic in characteristics)
            {
                Characteristics.Add(characteristic);
            }
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlert("Connection Cancelled", "The operation was cancelled. Please try again", "OK");
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    public async Task SendMessageCommandAsync()
    {
        if (!String.IsNullOrWhiteSpace(CurrentMessage.Body))
        {
            await bluetoothControllerInterface.SendMessageAsync(Encoding.UTF8.GetBytes(CurrentMessage.Body));
        }
        //var command = new byte[] { 0x01, 0x02 };
    }

    private void OnMessageSent(object sender, byte[] message)
    {
        var messageBody = Encoding.UTF8.GetString(message);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new Message
            {
                Type = CurrentMessage.Type,
                Body = messageBody,
            });
        });
    }

    private void OnMessageReceived(object sender, byte[] message)
    {
        var messageBody = Encoding.UTF8.GetString(message);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new Message
            {
                Type = Message.MessageType.Received,
                Body = messageBody,
            });
        });
    }
}
