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
            string[,] returnedData;

            if (args.Length == 0)
            {
                // When No CSV file is specified, initialize with a 2D array of dummy data.
                initialData = new string[,]
                {
            { "Heading1", "Heading2", "Heading 3" },
            { "", "", "" },
            { "", "", "" },
            { "", "", "" },
                };
            }
            else
            {
                // A single argument is expected, which is the path to a CSV file.
                string filePath = args[0];
                initialData = GetDataFromCSV(filePath);
            }

            // View/ edit the data in a grid format.
            returnedData = ShowGrid(initialData, windowTitle);

            // Returned data may be processed here.

        }
    }
}
