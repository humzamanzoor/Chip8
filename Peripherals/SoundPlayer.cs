namespace Chip8.Peripherals
{
    public class SoundPlayer : ISoundPlayer
    {
        public void Beep(int milliseconds)
        {
            if(OperatingSystem.IsWindows())
                Console.Beep(1000 ,milliseconds);

            else
                return;
        }
    }
}