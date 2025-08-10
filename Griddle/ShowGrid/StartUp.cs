using System;
using static SheetView.Griddle;

namespace SheetView
{
    internal static class StartUp
    {
        [STAThread]

        private static void Main()
        {
            const string windowTitle = "ExampleValues";
            string[,] initialData;
            string[,] returnedData;

            // ShowGrid is a method that displays a grid with the provided data.
            // It returns the modified data after user interaction.
            // This Main method demonstrates its capability.
            //------------------------------------------------------

            /*
            // Initialize with a 2D array of data.
            initialData = new string[,]
            {
            { "Name", "Value1", "Value2" },
            { "Aaron", "30", "100" },
            { "Betty", "40", "200" },
            { "Clive", "50", "300" },
            };
            returnedData = ShowGrid(initialData, windowTitle);
            */

            //------------------------------------------------------

            // Initialize from a CSV file.
            initialData = ReadCsv("C:\\Temp\\SomeValues.csv");
            returnedData = ShowGrid(initialData, windowTitle);

            //------------------------------------------------------

            // Print values to Console, to show grid data was returned.
            Console.WriteLine("Grid values:");
            for (int i = 0; i < returnedData.GetLength(0); i++)
            {
                for (int j = 0; j < returnedData.GetLength(1); j++)
                {
                    Console.Write(returnedData[i, j] + "\t");
                }
                Console.WriteLine();
            }

        }
    }
}
