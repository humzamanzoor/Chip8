public static class Program 
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new Chip8.Screen.Screen());
    }
}