using CloudOStat.LocalHardware;

using Meadow;
using Meadow.Foundation.Displays.Lcd;


using System.Threading.Tasks;

namespace CloudOStat.DesktopPrototype;

public class MeadowApp : App<Desktop>
{
    VirtualPackage _hardware;

    public override Task Initialize()
    {
        Resolver.Log.Info($"Initializing {this.GetType().Name}");
        Resolver.Log.Info($" Platform OS is a {Device.PlatformOS.GetType().Name}");
        Resolver.Log.Info($" Platform: {Device.Information.Platform}");
        Resolver.Log.Info($" OS: {Device.Information.OSVersion}");
        Resolver.Log.Info($" Model: {Device.Information.Model}");
        Resolver.Log.Info($" Processor: {Device.Information.ProcessorType}");

        //_hardware = new VirtualPackage(Device);

        //Device.Display?.Resize(320, 240, 2);
        var displayController = new DisplayController(Device.Display!);

        return base.Initialize();
    }

    public override Task Run()
    {
        // NOTE: this will not return until the display is closed
        ExecutePlatformDisplayRunner();

        return Task.CompletedTask;
    }

    private void ExecutePlatformDisplayRunner()
    {
        //if (Device.Display is SilkDisplay sd)
        //{
        //    sd.Run();
        //}
        MeadowOS.TerminateRun();
        System.Environment.Exit(0);
    }
}