namespace Chip8.Peripherals
{
    public interface IRenderer
    {
        void Draw(bool[,] screen);
    }
}