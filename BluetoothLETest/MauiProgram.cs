using BluetoothLETest.Data;
using BluetoothLETest.Domain.Interfaces;
using BluetoothLETest.Presentation.ViewModels;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace BluetoothLETest
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            //VIEWMODELS
            builder.Services.AddSingleton<MainViewModel>();

            //PAGES
            builder.Services.AddSingleton<MainPage>();

            //Platform 
            builder.Services.AddSingleton<IBluetoothLE>(provider => CrossBluetoothLE.Current);
            builder.Services.AddSingleton<IAdapter>(provider => CrossBluetoothLE.Current.Adapter);

            //CONTROLLER
            builder.Services.AddSingleton<IBluetoothController, BluetoothController>();
            builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);


            return builder.Build();
        }
    }
}
