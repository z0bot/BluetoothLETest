using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothLETest.Domain.Models;

public partial class Message : ObservableObject
{
    public enum MessageType
    {
        Sent,
        Received
    }

    [ObservableProperty]
    private MessageType type;

    [ObservableProperty]
    private string body;
}
