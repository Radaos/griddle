using System;
using static SheetView.Griddle;

namespace SheetView
{
    internal static class StartUp
    {
        private const string windowTitle = "";
        [STAThread]

        private static void Main(string[] args)
        {
            string[,] initialData;
            string[,] returnedData = null;

            if (args.Length == 0)
            {
                // No CSV specified so initialize with a 2D array of dummy data.

                initialData = new string[,]
                {
            { "Heading1", "Heading2", "Heading 3" },
            { "", "", "" },
            { "", "", "" },
            { "", "", "" },
                };
                // View/ edit the data in a grid.
                returnedData = ShowGrid(initialData, windowTitle);
            }
            else
            {
                Console.WriteLine("Command-line arguments:");

                if (args.Length > 0)
                {
                    string filePath = args[0];
                    string fileName = System.IO.Path.GetFileName(filePath);
                    // Initialize from a CSV file.
                    initialData = ReadCsv(filePath);
                    // View/ edit the data in a grid.
                    returnedData = ShowGrid(initialData, fileName);
                }
            }

            // Process returned data as needed.



            /////////////////////////////////////////////////////////////////
            // Demo: Print values to Console, to show grid data was returned.
            if (returnedData == null)
            {
                Console.WriteLine("No data returned from ShowGrid.");
                return;
            }
            else
            {
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
            /////////////////////////////////////////////////////////////////

        }
    }
}
