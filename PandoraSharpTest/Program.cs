using Elpis.PandoraSharp;

namespace Elpis.PandoraSharpTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Pandora p = new Pandora();

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