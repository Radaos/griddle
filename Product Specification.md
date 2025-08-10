# Product specification — Griddle

## Overview

Griddle is a lightweight Windows Forms utility for viewing and editing 2D string data in a grid, with simple CSV import/export. It exposes a small, static API suitable for embedding in desktop apps that need a quick, modal “sheet editor” where editing can be restricted to only specific columns.

## Goals and non‑goals

### Goals

Provide a modal dialog for reviewing and editing tabular string data.

Enforce column-level edit restrictions (e.g., only last column editable).

Support simple CSV read/write to interoperate with external tools.

Be easy to integrate: single static class, minimal dependencies.

### Non‑goals

Full spreadsheet features (formulas, formatting persistence, sorting, filtering).

Robust CSV dialect handling (quoted fields, escaped delimiters, multiline fields).

Large-scale data virtualization or async streaming.

Target users and scenarios

Developers embedding a quick grid editor in internal tools or utilities.

### Scenarios:

Displaying computed data with a single editable “notes” or “override” column.

Importing an external CSV, making small edits, saving back to CSV.

Environments and dependencies

### Platform: Windows

UI framework: Windows Forms (System.Windows.Forms)

.NET: .NET Framework 4.7.2+ or .NET 6+ (Windows)

Core dependencies: System, System.IO, System.Drawing, System.Windows.Forms

## Functional requirements

**FR‑01**: Modal grid editing

The system shall present a modal dialog to display and edit a 2D string array where the first row is treated as headers.

On closing with confirmation, it shall return a 2D string array containing the grid contents in row-major order, preserving shape.

**FR‑02**: Input validation

If the input 2D array is null, the system shall throw ArgumentNullException.

If the input has fewer than 2 rows or fewer than 2 columns, the system shall throw ArgumentException.

**FR‑03**: Headers

The first row of the input array shall be used as column headers and shall not be editable if column edit restrictions are enabled.

**FR‑04**: Edit restrictions

When edit restriction is enabled, the system shall only allow editing in a single specified column; all others shall be read-only.

A default mode shall restrict editing to the last column when enabled.

**FR‑05**: Column alignment

All columns shall display cell contents right-aligned within the grid.

**FR‑06**: Exit

On Exit, the system shall return the current grid contents to the caller.

**FR‑07**: CSV export

The system shall allow writing the current grid contents to a CSV file via a SaveFileDialog.

CSV export shall write headers in the first row, followed by data rows.

The SaveFileDialog title may be set via the API, but it shall not influence the file name automatically.

**FR‑08**: CSV import

The system shall be able to read a CSV file into a 2D string array using comma as the delimiter and without support for quoted fields or escapes.

If the file does not exist, the system shall throw FileNotFoundException.

**FR‑09**: CSV shape rules

When reading CSV, rows with fewer fields than the maximum observed column count shall be padded with empty strings to ensure a rectangular 2D shape.

Extra fields beyond the first row’s column count shall be preserved by expanding the column count to the maximum encountered.

**FR‑10**: Data integrity

The system shall preserve the number of rows and columns unless the user imports CSV with a different shape.

The system shall trim trailing newline characters but shall not trim or alter internal spaces within cell values.

**FR‑11**: Error reporting

The system shall surface exceptions for invalid inputs or I/O errors rather than silently failing.

**FR‑12**: Accessibility basics

The dialog shall provide keyboard navigation for grid cells and actions (Tab/Shift+Tab, arrow keys, Enter, Esc).

**FR‑13**: Performance expectations

The system shall handle up to ~50k cells (e.g., 1k rows × 50 columns) with acceptable responsiveness on a typical developer machine. Larger datasets are out of scope.

## Design specifications

### Architecture and components

#### Components

Griddle (static class): Hosts the public API for showing the grid, reading CSV, and writing CSV, plus internal helpers.

Key UI element: DataGridView for rendering and editing.

**API surface**

        /// Shows a modal dialog to edit values in a grid, then returns them as a 2D string array.
        /// elemData: 2D string array; first row is headers.
        /// title: dialog window title.
        /// Throws: ArgumentNullException, ArgumentException
        internal static string[,] ShowGrid(string[,] elemData, string title);

        /// Reads a CSV file into a 2D string array; comma delimiter; no quoted fields.
        /// Throws: FileNotFoundException
        internal static string[,] ReadCsv(string filePath);


### Data model:

Input/output: 2D string array where [0, j] are headers; [i, j] for i ≥ 1 are data rows.

### Grid binding: Create DataTable or bind directly to DataGridView by constructing columns, then rows from the array.

### UI layout and behavior

Dialog composition

Title: provided by caller.

Body: DataGridView fills the client area.

Footer: Buttons — Load, Save, Exit

DataGridView settings

Column headers from first row.

ReadOnly per column restriction rule.

DefaultCellStyle.Alignment = MiddleRight for all columns.

Selection mode: CellSelect.

Editing: In-place, commit on cell leave or OK.

Prevent adding/removing rows by the user unless explicitly enabled.

Editing restriction logic

Determine editable column index:

If LastColOnlyEdit == true, editableIndex = last column.

Else, editableIndex may be provided via configuration or defaults to all editable.

### CSV format and rules

Delimiter: comma (,)

No quoting or escaping; commas within values are not supported.

Line endings: CRLF recommended; tolerate LF.

Encoding: System default or UTF‑8 (recommend UTF‑8 without BOM for interoperability).

Headers: first line is headers.

Shape normalization on read: pad short rows with empty strings; expand columns to match widest row.

### Algorithms and flows

**ShowGrid flow**

Validate elemData (null and minimum dimensions).

Create Form and DataGridView; set dialog title.

Create columns from elemData first row; apply alignment and read-only rules.

Populate rows from remaining elemData.

Show dialog modally.

WriteCsv flow

Open SaveFileDialog (set title).

If user selects a path: iterate rows × cols and write comma-separated values; newline per row.

Do not quote or escape values.

ReadCsv flow

Validate file exists.

Read all lines.

Split each line by comma; compute max column count.

Allocate 2D array [rowCount, maxCols]; copy tokens; pad missing cells with empty strings.

Return array.

### Error handling

Input validation exceptions as specified.

I/O errors surfaced to caller (FileNotFoundException, UnauthorizedAccessException, IOException).

Optionally wrap CSV parsing errors with informative messages indicating line number.

### Performance considerations

Avoid DataTable overhead by directly creating DataGridView columns/rows for moderate sizes.

Defer layout while populating (BeginInit/EndInit or SuspendLayout/ResumeLayout).

Use StringBuilder for CSV writing.

### Accessibility and internationalization

Keyboard navigation and standard dialog behaviors (Esc cancels, Enter confirms).

OS text rendering; right alignment maintained.

Note: Using comma as delimiter; locales that use comma as decimal separator are allowed as values but must not contain commas due to no quoting support.

### Security considerations

File dialogs restrict the user to explicit file choices.

No path manipulation beyond what the user selects.

Avoid executing or interpreting CSV contents.

### Acceptance tests (sample)

ID
Requirement
Test description
Expected result

AT‑01
FR‑02
Call ShowGrid(null, "X")
Throws ArgumentNullException

AT‑02
FR‑02
Call ShowGrid([1x2], "X")
Throws ArgumentException

AT‑03
FR‑01/03
Provide 3x3 array; headers in first row
Grid shows 3 columns with header text; data rows present

AT‑04
FR‑04
LastColOnlyEdit=true
Only last column cells are editable; others read-only

AT‑06
FR‑06
Click Exit after edits
Method returns array reflecting in-grid edits

AT‑07
FR‑07
Export to CSV with SaveFileDialog
File contains headers on first line, then data, comma-separated

AT‑08
FR‑08/09
Read CSV with ragged rows
Returned array is rectangular; short rows padded with ""

AT‑09
FR‑11
ReadCsv on missing file
Throws FileNotFoundException

AT‑10
FR‑05
Inspect cell style
All cells displayed right-aligned


