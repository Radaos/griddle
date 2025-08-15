using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SheetView
{
    internal static class Griddle
    {
        private const string appTitle = "Griddle: ";
        private const bool LastColOnlyEditable = false;
        private const int headingCol = -1;   // Format this column as a heading or set to -1 to disable formatting.
        private static int lastFoundRow = -1;
        private static int lastFoundCol = -1;
        private static string lastSearchText = "";

        /// <summary>
        /// Shows a modal dialog to edit values in a grid, then returns them as a 2D string array.
        /// </summary>
        /// <param name="elemData">A 2D string array containing the initial data (first row is headers).</param>
        /// <param name="dataTitle">Text to dispay on the window title bar.</param>
        /// <returns>A 2D string array with the grid data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elemData"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="elemData"/> has less than 2 rows or columns.</exception>
        internal static string[,] ShowGrid(string[,] elemData, string dataTitle)
        {
            if (elemData == null)
            {
                throw new ArgumentNullException(nameof(elemData));
            }

            int rows = elemData.GetLength(0);
            int numCols = elemData.GetLength(1);
            if (rows < 2 || numCols < 2)
            {
                throw new ArgumentException("Data must have at least 2 rows (header + data) and 2 columns.", nameof(elemData));
            }

            string[,] result = new string[0, 0];

            using (Form form = new Form())
            using (Panel panel = new Panel())
            using (Button btnExit = new Button())
            using (Button btnSave = new Button())
            using (Button btnLoad = new Button())
            using (Button btnSearch = new Button())
            using (DataGridView grid = new DataGridView())
            using (TextBox txtSearch = new TextBox())
            {
                // Set up form
                form.Text = appTitle + dataTitle;
                form.Icon = SystemIcons.Application;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MaximizeBox = true;
                form.MinimizeBox = false;
                form.ShowInTaskbar = true;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Size = new Size(800, 500);
                form.MinimumSize = new Size(600, 400);

                // Bottom panel for buttons
                panel.Dock = DockStyle.Bottom;
                panel.Height = 52;
                panel.Padding = new Padding(10);
                panel.BackColor = SystemColors.Control;

                // Grid
                grid.Dock = DockStyle.Fill;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.AllowUserToResizeRows = false;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
                grid.MultiSelect = false;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

                // Set up columns
                grid.ColumnCount = numCols;

                FormatCols(elemData, numCols, grid);

                // Populate rows below header
                for (int r = 1; r < rows; r++)
                {
                    object[] rowValues = new object[numCols];
                    for (int c = 0; c < numCols; c++)
                    {
                        rowValues[c] = elemData[r, c];
                    }

                    _ = grid.Rows.Add(rowValues);
                }

                // Define search textbox and button locations
                txtSearch.Size = new Size(180, 30);
                txtSearch.Location = new Point(10, 10);
                txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                txtSearch.Text = "";

                btnSearch.Text = "Search";
                btnSearch.Size = new Size(90, 30);
                btnSearch.Location = new Point(txtSearch.Right + 20, 10);
                btnSearch.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                btnSearch.AutoSize = true;

                btnLoad.Text = "Load";
                btnLoad.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnLoad.Size = new Size(100, 30);
                btnLoad.Location = new Point(panel.Width - 440, 10);
                btnLoad.AutoSize = true;

                btnSave.Text = "Save";
                btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnSave.Size = new Size(100, 30);
                btnSave.Location = new Point(panel.Width - 330, 10);
                btnSave.AutoSize = true;

                btnExit.Text = "Exit";
                btnExit.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                btnExit.Size = new Size(100, 30);
                btnExit.Location = new Point(panel.Width - 220, 10);
                btnExit.AutoSize = true;

                // Keep buttons anchored properly when panel resizes
                panel.Resize += (s, e) =>
                {
                    btnExit.Left = panel.ClientSize.Width - btnExit.Width - 10;
                    btnSave.Left = btnExit.Left - btnSave.Width - 10;
                    btnLoad.Left = btnSave.Left - btnLoad.Width - 10;
                };

                // Search button: search for text in grid, highlight cell if found
                btnSearch.Click += (s, e) =>
                {
                    DataSearch(form, grid, txtSearch, ref lastFoundRow, ref lastFoundCol, ref lastSearchText);
                };

                // Load button: prompt for CSV, load, clear and repopulate grid
                btnLoad.Click += (s, e) =>
                {
                    LoadCsv(appTitle, form, grid);
                };

                // Save button: save data to a CSV file
                btnSave.Click += (s, e) =>
                {
                    string newFileName = SaveCsv(grid.Rows.Count, grid.Columns.Count, form, grid);
                    form.Text = appTitle + newFileName;
                };

                // Exit button: return the data in a 2D string array
                btnExit.Click += (s, e) =>
                {
                    int rowCount = grid.Rows.Count;
                    int colCount = grid.Columns.Count;
                    string[,] allData = new string[rowCount + 1, colCount]; // +1 accounts for header

                    // Copy headers
                    for (int c = 0; c < colCount; c++)
                    {
                        allData[0, c] = grid.Columns[c].HeaderText ?? "";
                    }

                    // Copy all data below headers
                    for (int r = 0; r < rowCount; r++)
                    {
                        for (int c = 0; c < colCount; c++)
                        {
                            allData[r + 1, c] = grid.Rows[r].Cells[c].Value?.ToString() ?? "";
                        }
                    }

                    result = allData;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };

                // Add the controls
                panel.Controls.Add(txtSearch);
                panel.Controls.Add(btnSearch);
                panel.Controls.Add(btnLoad);
                panel.Controls.Add(btnSave);
                panel.Controls.Add(btnExit);
                form.Controls.Add(grid);
                form.Controls.Add(panel);

                // Define keyboard shortcuts
                form.KeyPreview = true;
                form.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        btnExit.PerformClick();
                        e.Handled = true;
                    }
                    else if (e.KeyCode == Keys.Enter && !grid.IsCurrentCellInEditMode)
                    {
                        btnSearch.PerformClick();
                        e.Handled = true;
                    }
                };

                _ = form.ShowDialog();
            }

            return result;
        }

        private static void DataSearch(Form form, DataGridView grid, TextBox txtSearch, ref int lastFoundRow, ref int lastFoundCol, ref string lastSearchText)
        {
            // Capture the state, then update the ref parameters
            int foundRow = lastFoundRow;
            int foundCol = lastFoundCol;
            string searchText = lastSearchText;

            string search = txtSearch.Text;
            if (string.IsNullOrEmpty(search))
            {
                return;
            }

            int startRow = 0, startCol = 0;

            // If search text is unchanged and a previous match exists, start from next cell
            if (search == searchText && foundRow != -1 && foundCol != -1)
            {
                startRow = foundRow;
                startCol = foundCol + 1;
                if (startCol >= grid.ColumnCount)
                {
                    startCol = 0;
                    startRow++;
                }
                if (startRow >= grid.RowCount)
                {
                    startRow = 0;
                    startCol = 0;
                }
            }
            else
            {
                // New search or no previous match: start from beginning
                foundRow = -1;
                foundCol = -1;
                searchText = search;
            }

            bool found = false;
            int rowCount = grid.RowCount;
            int colCount = grid.ColumnCount;

            // Search from startRow/startCol to end
            for (int r = startRow; r < rowCount; r++)
            {
                int cStart = (r == startRow) ? startCol : 0;
                for (int c = cStart; c < colCount; c++)
                {
                    string cellValue = grid.Rows[r].Cells[c].Value?.ToString() ?? "";
                    if (cellValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        grid.ClearSelection();
                        grid.Rows[r].Cells[c].Selected = true;
                        grid.CurrentCell = grid.Rows[r].Cells[c];
                        foundRow = r;
                        foundCol = c;
                        searchText = search;
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }

            // If not found and wraparound occurred, search from 0,0 up to original position
            if (!found && search == searchText && foundRow != -1 && foundCol != -1)
            {
                for (int r = 0; r <= foundRow; r++)
                {
                    int cEnd = (r == foundRow) ? foundCol - 1 : colCount - 1;
                    for (int c = 0; c <= cEnd; c++)
                    {
                        string cellValue = grid.Rows[r].Cells[c].Value?.ToString() ?? "";
                        if (cellValue.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            grid.ClearSelection();
                            grid.Rows[r].Cells[c].Selected = true;
                            grid.CurrentCell = grid.Rows[r].Cells[c];
                            foundRow = r;
                            foundCol = c;
                            searchText = search;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
            }

            if (!found)
            {
                _ = MessageBox.Show(form, $"No match found for \"{search}\".", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                foundRow = -1;
                foundCol = -1;
            }

            // Update the ref parameters
            lastFoundRow = foundRow;
            lastFoundCol = foundCol;
            lastSearchText = searchText;
        }

        /// <summary>
        /// Loads a CSV file and populates DataGridView with the contents.
        /// </summary>
        /// <param name="appTitle"></param>
        /// <param name="form"></param>
        /// <param name="btnLoad"></param>
        /// <param name="grid"></param>
        private static void LoadCsv(string appTitle, Form form, DataGridView grid)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                ofd.Title = "Load CSV File";
                if (ofd.ShowDialog(form) == DialogResult.OK)
                {
                    string fileName = Path.GetFileName(ofd.FileName);
                    try
                    {
                        string[,] loaded = GetDataFromCSV(ofd.FileName);
                        int loadedRows = loaded.GetLength(0);
                        int loadedCols = loaded.GetLength(1);

                        grid.Rows.Clear();
                        grid.Columns.Clear();
                        grid.ColumnCount = loadedCols;
                        for (int c = 0; c < loadedCols; c++)
                        {
                            DataGridViewColumn col = grid.Columns[c];
                            string header = loaded[0, c] ?? "";
                            col.Name = header;
                            col.HeaderText = header;
                        }
                        FormatCols(loaded, loadedCols, grid);

                        for (int r = 1; r < loadedRows; r++)
                        {
                            object[] rowValues = new object[loadedCols];
                            for (int c = 0; c < loadedCols; c++)
                            {
                                rowValues[c] = loaded[r, c];
                            }

                            _ = grid.Rows.Add(rowValues);
                            form.Text = appTitle + fileName;
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = MessageBox.Show(form, $"Failed to load CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the contents of a DataGridView to a CSV file
        /// </summary>
        /// <param name="rows">The number of rows to write.</param>
        /// <param name="cols">The number of columns to write.</param>
        /// <param name="form">The parent form for the dialog.</param>
        /// <param name="grid">The DataGridView containing the data.</param>
        private static string SaveCsv(int rows, int cols, Form form, DataGridView grid)
        {
            string fileName = "";
            try
            {
                using (SaveFileDialog ofd = new SaveFileDialog())
                {
                    ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    ofd.Title = "Save CSV File";
                    if (ofd.ShowDialog(form) == DialogResult.OK)
                    {
                        string filePath = ofd.FileName;
                        fileName = Path.GetFileName(filePath);
                        using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                        {
                            // Write all column headers
                            List<string> headerFields = new List<string>(cols);
                            for (int c = 0; c < cols; c++)
                            {
                                string header = grid.Columns[c].HeaderText ?? "";
                                header = header.Replace("\"", "\"\"");
                                headerFields.Add($"\"{header}\"");
                            }
                            writer.WriteLine(string.Join(",", headerFields));

                            // Write data
                            for (int r = 0; r < rows; r++)
                            {
                                List<string> fields = new List<string>(cols);
                                for (int c = 0; c < cols; c++)
                                {
                                    string val = grid.Rows[r].Cells[c].Value?.ToString() ?? "";
                                    val = val.Replace("\"", "\"\"");
                                    fields.Add($"\"{val}\"");
                                }
                                writer.WriteLine(string.Join(",", fields));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(form, $"Failed to save CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return fileName;
        }

        /// <summary>
        /// Reads a CSV file into a 2D string array and returns it to the caller
        /// Assumes comma as delimiter and no quoted fields.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <returns>A 2D string array containing the CSV data.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        internal static string[,] GetDataFromCSV(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            List<string[]> lines = new List<string[]>();
            int maxColumns = 0;

            foreach (string line in File.ReadLines(filePath))
            {
                string[] fields = line.Split(',');
                lines.Add(fields);
                if (fields.Length > maxColumns)
                {
                    maxColumns = fields.Length;
                }
            }

            string[,] result = new string[lines.Count, maxColumns];
            for (int i = 0; i < lines.Count; i++)
            {
                string[] lineArr = lines[i];
                for (int j = 0; j < lineArr.Length; j++)
                {
                    // Strip quotes from the cell value if present
                    string cell = lineArr[j].Trim('"').Replace("\"\"", "\"");
                    result[i, j] = cell;
                }
            }
            return result;
        }

        private static void FormatCols(string[,] elemData, int numCols, DataGridView grid)
        {
            // Format the columns as required
            for (int c = 0; c < numCols; c++)
            {
                DataGridViewColumn dataCol = grid.Columns[c];
                string header = elemData[0, c] ?? "";
                dataCol.Name = header;
                dataCol.HeaderText = header;

                if (c == headingCol)
                {
                    //Set background colour to light grey for the heading column
                    dataCol.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
                    dataCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dataCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    if (LastColOnlyEditable)
                    {
                        dataCol.ReadOnly = true;
                    }
                }

                if (c > headingCol && c < numCols - 1)
                {
                    dataCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    if (LastColOnlyEditable)
                    {
                        dataCol.ReadOnly = true;
                    }
                }

                if (c == numCols - 1)                  
                {
                    dataCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    if (LastColOnlyEditable)
                    {
                        dataCol.ReadOnly = false;
                    }
                }

            }
        }
    }
}

