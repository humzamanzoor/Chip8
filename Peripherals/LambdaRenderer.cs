namespace Chip8.Peripherals
{
    public class LambdaRenderer : IRenderer
    {
        private readonly Action<bool[,]> _draw;
        public LambdaRenderer(Action<bool[,]> draw)
        {
            _draw = draw;
        }
        public void Draw(bool[,] screen)
         => _draw?.Invoke(screen);
    }
}