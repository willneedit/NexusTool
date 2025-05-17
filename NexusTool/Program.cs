
using HidApi;
using Mono.Options;

namespace NexusTool;

public class Program
{
    public static bool Quiet { get; private set; } = false;
    private static bool _showHelp;
    private static int? _brightness = null;
    private static bool _info;
    private static bool _loop;
    private static int? _animation = null;
    private static bool _stopanim;
    private static bool _blankscreen;
    private static int? _touch = null;


    private static List<string> Extra = [];

    public static int Main(string[] argv)
    {
        ParseCommandlineOptions(argv);

        if (_showHelp) return 1;

        try
        {
            foreach (DeviceInfo deviceInfo in Hid.Enumerate())
            {
                if (deviceInfo.VendorId != 0x1b1c || deviceInfo.ProductId != 0x1b8e || deviceInfo.UsagePage != 12) continue;

                using Device device = deviceInfo.ConnectToDevice();

                Dispatch(device);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 100;
        }
        finally
        {
            Hid.Exit(); //Call at the end of your program            
        }

        return 0;
    }

    private static void Dispatch(Device device)
    {
        Nexus nexus = new(device);

        if (_info) nexus.Info();

        if (_brightness != null) nexus.SetBrightness(_brightness.Value);

        if (_animation != null) nexus.PlayAnimation(_animation.Value, _loop);

        if (_stopanim) nexus.StopAnimation();

        if (_blankscreen) nexus.BlankScreen();

        if (_touch != null) nexus.WaitForTouch(_touch.Value);
    }

    private static void ParseCommandlineOptions(string[] args)
    {
        OptionSet options = new()
        {
            { "q|quiet", "No output, except payload data", q => Quiet = q != null },
            { "i|info", "Show device information", i => _info = i != null },
            { "b|brightness=", "Set brightness (0-100)", (int b) => _brightness = b},
            { "l|loop", "Play animation as a loop", l => _loop = l != null },
            { "p|playanim=", "Play embedded animation (1-3)", (int a) => _animation = a },
            { "s|stopanim", "Stop embedded animation", s => _stopanim = s != null },
            { "c|clearscreen", "Clears the screen", c => _blankscreen = c != null },
            { "t|touch=", "Wait for seconds for a touch/swipe", (int t) => _touch = t },
            { "h|help", "This text :)", h => _showHelp = h != null }
        };

        try
        {
            Extra = options.Parse(args);

        }
        catch (OptionException)
        {
            _showHelp = true;
        }

        if (_showHelp) options.WriteOptionDescriptions(Console.Out);
    }
}