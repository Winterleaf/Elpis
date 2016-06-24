namespace PandoraSharpTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            PandoraSharp.Pandora p = new PandoraSharp.Pandora();

            try
            {
                //p.Sync();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex);
            }

            System.Console.WriteLine("testing");
        }
    }
}