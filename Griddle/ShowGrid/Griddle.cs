using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SheetView
{
    internal static class Griddle
    {
        /// <summary>
        /// If true, only the last column is editable in the grid.
        /// </summary>
        private const bool LastColOnlyEdit = true;

        /// <summary>
        /// Shows a modal dialog to edit values in a grid, then returns them as a 2D string array.
        /// </summary>
        /// <param name="elemData">A 2D string array containing the initial data (first row is headers).</param>
        /// <param name="title">The window title for the dialog.</param>
        /// <returns>A 2D string array with the grid data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="elemData"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="elemData"/> has less than 2 rows or columns.</exception>
        internal static string[,] ShowGrid(string[,] elemData, string title)
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
            using (DataGridView grid = new DataGridView())
            {
                // Set up form
                form.Text = title;
                form.Icon = SystemIcons.Application;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowInTaskbar = false;
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

                // Set up columns using first row as headers
                grid.ColumnCount = numCols;
                for (int c = 0; c < numCols; c++)
                {
                    DataGridViewColumn dataCol = grid.Columns[c];
                    string header = elemData[0, c] ?? "";
                    dataCol.Name = header;
                    dataCol.HeaderText = header;
                    if (LastColOnlyEdit)
                    {
                        SetColumnAccess(c, numCols - 1, dataCol);
                    }
                }

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

                // Buttons
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

                // Load button: prompt for CSV, load, clear and repopulate grid
                btnLoad.Click += (s, e) =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                        ofd.Title = "Load CSV File";
                        if (ofd.ShowDialog(form) == DialogResult.OK)
                        {
                            try
                            {
                                string[,] loaded = ReadCsv(ofd.FileName);
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
                                    if (LastColOnlyEdit)
                                    {
                                        SetColumnAccess(c, loadedCols - 1, col);
                                    }
                                }
                                for (int r = 1; r < loadedRows; r++)
                                {
                                    object[] rowValues = new object[loadedCols];
                                    for (int c = 0; c < loadedCols; c++)
                                    {
                                        rowValues[c] = loaded[r, c];
                                    }

                                    _ = grid.Rows.Add(rowValues);
                                }
                            }
                            catch (Exception ex)
                            {
                                _ = MessageBox.Show(form, $"Failed to load CSV:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };

                // Save button: export data to a CSV file
                btnSave.Click += (s, e) =>
                {
                    WriteCsv(title, grid.Rows.Count, grid.Columns.Count, form, grid);
                };

                // Exit button: return a 2D string array
                btnExit.Click += (s, e) =>
                {
                    int rowCount = grid.Rows.Count;
                    int colCount = grid.Columns.Count;
                    string[,] allData = new string[rowCount + 1, colCount]; // +1 for header

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

                // Add controls
                panel.Controls.Add(btnLoad);
                panel.Controls.Add(btnSave);
                panel.Controls.Add(btnExit);
                form.Controls.Add(grid);
                form.Controls.Add(panel);

                // Keyboard shortcuts
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
                        btnExit.PerformClick();
                        e.Handled = true;
                    }
                };

                _ = form.ShowDialog();
            }

            return result;
        }

        /// <summary>
        /// Restricts editing in a DataGridView to only the specified column.
        /// </summary>
        /// <param name="c">The current column index.</param>
        /// <param name="coltoedit">The column index to allow editing.</param>
        /// <param name="col">The DataGridViewColumn to set properties on.</param>
        private static void SetColumnAccess(int c, int coltoedit, DataGridViewColumn col)
        {
            col.ReadOnly = c != coltoedit;
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        /// <summary>
        /// Writes the contents of a DataGridView to a CSV file using a SaveFileDialog.
        /// </summary>
        /// <param name="title">The title for the SaveFileDialog (not used in file naming).</param>
        /// <param name="rows">The number of rows to write.</param>
        /// <param name="cols">The number of columns to write.</param>
        /// <param name="form">The parent form for the dialog.</param>
        /// <param name="grid">The DataGridView containing the data.</param>
        private static void WriteCsv(string title, int rows, int cols, Form form, DataGridView grid)
        {
            try
            {
                using (SaveFileDialog ofd = new SaveFileDialog())
                {
                    ofd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    ofd.Title = "Save CSV File";
                    if (ofd.ShowDialog(form) == DialogResult.OK)
                    {
                        string filePath = ofd.FileName;
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
        }

        /// <summary>
        /// Reads a CSV file into a 2D string array.
        /// Assumes comma as delimiter and no quoted fields.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <returns>A 2D string array containing the CSV data.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        internal static string[,] ReadCsv(string filePath)
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
    }
}
