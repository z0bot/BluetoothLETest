using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace BluetoothLETest.Helpers;

internal class BluetoothPermissions : BasePlatformPermission
{
#if ANDROID
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => 
        new List<(string permission, bool isRuntime)>
        {
           ("android.permission.BLUETOOTH_SCAN", true),
           ("android.permission.BLUETOOTH_CONNECT", true),
           ("android.permission.BLUETOOTH_ADVERTISE", true),
            //(Android.Manifest.Permission.BluetoothScan, true),
            //(Android.Manifest.Permission.BluetoothConnect, true)
        }.ToArray();
#endif
}
