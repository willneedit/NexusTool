using System;
using System.Text;
using HidApi;

namespace NexusTool;

public class Nexus
{
    private Device _device;

    public Nexus(Device device)
    {
        _device = device;
    }

    public void Info()
    {
        Console.WriteLine($"Menufacturer   : '{_device.GetManufacturer()}'");
        Console.WriteLine($"Product        : '{_device.GetProduct()}'");
        Console.WriteLine($"Serial No.     : '{_device.GetSerialNumber()}'");

        // Get Feature report ID 5: ??? + Firmware info
        ReadOnlySpan<byte> bytes = _device.GetFeatureReport(5, 64);
        int pos = 0;
        for (pos = 6; pos < bytes.Length; pos++) if (bytes[pos] == 0) break;

        string str = Encoding.ASCII.GetString(bytes[6..pos]);
        Console.WriteLine($"Firmware verion: '{str}'");
    }

    public void SetBrightness(int brightness)
    {
        if (brightness < 0 || brightness > 100)
            throw new ArgumentOutOfRangeException(nameof(brightness));

        // Set Feature report 3, Command 1, 1 byte argument
        byte[] setbrightness = [3, 1, (byte)brightness];
        _device.SendFeatureReport(setbrightness);

        if (!Program.Quiet) Console.WriteLine($"Set brightness to {brightness}");
    }

    public void PlayAnimation(int animation, bool loop)
    {
        if (animation < 1 || animation > 3)
            throw new ArgumentOutOfRangeException(nameof(animation));

        // Set Feature report 3, Command 13, 2 bytes argument
        byte[] setAnimation = [3, 13, (byte)animation, (byte)(loop ? 1 : 0)];
        _device.SendFeatureReport(setAnimation);

        if (!Program.Quiet) Console.WriteLine($"{(loop ? "Looping" : "One-shot")} animation {animation} set to play");
    }

    public void StopAnimation()
    {
        // Set Feature report 3, Command 15, no arguments
        byte[] stopAnimation = [3, 15];
        _device.SendFeatureReport(stopAnimation);

        if (!Program.Quiet) Console.WriteLine("Stopping animation");
    }

    public void BlankScreen()
    {
        // Set Feature report 3, Command 4, no arguments
        byte[] blankScreen = [3, 4];
        _device.SendFeatureReport(blankScreen);

        if (!Program.Quiet) Console.WriteLine("Blanking screen");
    }

    public void WaitForTouch(int timeoutsecs)
    {
        if (timeoutsecs < 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutsecs));

        int first = -1;
        int last = -1;
        int lastX = -1;
        bool? touched = null;

        DateTime expiry = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutsecs);
        while (DateTime.UtcNow < expiry || touched == true) 
        {
            int X = -1;

            ReadOnlySpan<byte> bytes = _device.ReadTimeout(64, 2000);
            if (bytes.Length > 9)
            {
                if (bytes[0] != 0x01 || bytes[1] != 0x02 || bytes[2] != 0x21)
                {
                    Console.WriteLine("!! Bogus data");
                    break;
                }

                bool touch = bytes[5] != 0;
                X = bytes[6] + (bytes[7] << 8);

                if (touch != touched)
                {
                    if (touch)
                    {
                        // Console.WriteLine($"Touched @ {X}");
                        first = X;
                    }
                    else
                    {
                        // Console.WriteLine($"Relesed @ {lastX}");
                        last = lastX;
                    }
                    touched = touch;
                }
            }

            // false means touched and released, null means never seen it all.
            if (touched == false) break;

            lastX = X;
        }

        if (first < 0)
            Console.WriteLine("--"); // Never touched
        else
        {
            if (last < 0) last = first;

            int diff = last - first;
            if (diff > 200)
                Console.WriteLine("->"); // Swipe right
            else if (diff < -200)
                Console.WriteLine("<-"); // Swipe left
            else if (diff < -50 || diff > 50)
                Console.WriteLine($"+- {first}"); // Jittery touch (too short to swipe, but too far to touch a specific icon)
            else
                Console.WriteLine($"++ {first}"); // Stationary touch
        }
    }

    public void ShowImage(string name)
    {
        List<byte[]> images = RawImage.LoadRawImages(name, 1);

        byte[] image = images.First();

        UploadImage(image);
    }

    private void UploadImage(byte[] image)
    {
        byte[] loadBuffer = new byte[1024];

        loadBuffer[0] = 0x02; // Endpoint 2
        loadBuffer[1] = 0x05; // Command
        loadBuffer[2] = 0x40;
        loadBuffer[3] = 0x00; // 1 - last block
        loadBuffer[4] = 0x00; // Block Number Lo
        loadBuffer[5] = 0x00; // Block Number Hi (? Alwas 0)
        loadBuffer[6] = 0x00; // Payload length Lo (in bytes)
        loadBuffer[7] = 0x00; // Payload length Hi

        int remaining = image.Length;
        int offset = 0;
        int blockno = 0;

        while (remaining > 0)
        {
            int packlen = (remaining > 1016) ? 1016 : remaining; // 1024 - 8 bytes header len

            Buffer.BlockCopy(image, offset, loadBuffer, 8, packlen);

            loadBuffer[4] = (byte)blockno;
            loadBuffer[6] = (byte)(packlen & 0xff);
            loadBuffer[7] = (byte)(packlen >> 8);

            remaining -= packlen;
            offset += packlen;
            blockno++;

            loadBuffer[3] = (byte)((remaining == 0) ? 1 : 0);

            _device.Write(loadBuffer);
        }
    }

}
