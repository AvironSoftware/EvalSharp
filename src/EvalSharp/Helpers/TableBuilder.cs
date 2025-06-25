using System.Text;

namespace EvalSharp.Helpers
{
    internal class TableBuilder
    {
        private readonly List<string> _columns = [];
        private readonly List<string[]> _rows = [];
        private int[] _columnWidths = [];
        private const int MaxCellWidth = 78;

        public void AddColumn(string column)
        {
            _columns.Add(column);
        }

        public void AddRow(params string[] columns)
        {
            if (columns.Length != _columns.Count)
                throw new ArgumentException("Row column count must match header count.");

            _rows.Add(columns);
        }

        public string Build()
        {
            CalculateColumnWidths();

            var sb = new StringBuilder();

            // Top border
            sb.Append('┌');
            for (int i = 0; i < _columns.Count; i++)
            {
                sb.Append(new string('─', _columnWidths[i] + 2));
                sb.Append(i == _columns.Count - 1 ? '┐' : '┬');
            }
            sb.AppendLine();

            // Header row
            sb.Append('│');
            for (int i = 0; i < _columns.Count; i++)
            {
                sb.Append($" {_columns[i].PadRight(_columnWidths[i])} │");
            }
            sb.AppendLine();

            // Header separator
            sb.Append('├');
            for (int i = 0; i < _columns.Count; i++)
            {
                sb.Append(new string('─', _columnWidths[i] + 2));
                sb.Append(i == _columns.Count - 1 ? '┤' : '┼');
            }
            sb.AppendLine();

            // Rows with wrapping
            foreach (var row in _rows)
            {
                var wrappedRows = WrapRow(row);

                foreach (var wrappedRow in wrappedRows)
                {
                    sb.Append('│');
                    for (int i = 0; i < wrappedRow.Length; i++)
                    {
                        sb.Append($" {wrappedRow[i].PadRight(_columnWidths[i])} │");
                    }
                    sb.AppendLine();
                }
            }

            // Bottom border
            sb.Append('└');
            for (int i = 0; i < _columns.Count; i++)
            {
                sb.Append(new string('─', _columnWidths[i] + 2));
                sb.Append(i == _columns.Count - 1 ? '┘' : '┴');
            }
            sb.AppendLine();

            return sb.ToString();
        }

        private void CalculateColumnWidths()
        {
            _columnWidths = new int[_columns.Count];

            for (int i = 0; i < _columns.Count; i++)
            {
                _columnWidths[i] = Math.Min(_columns[i].Length, MaxCellWidth);
            }

            foreach (var row in _rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    int rawLength = row[i]?.Length ?? 0;
                    _columnWidths[i] = Math.Max(_columnWidths[i], Math.Min(rawLength, MaxCellWidth));
                }
            }
        }

        private List<string[]> WrapRow(string[] row)
        {
            var wrappedColumns = new List<List<string>>();

            for (int i = 0; i < row.Length; i++)
            {
                var wrapWidth = _columnWidths[i];
                var chunks = row[i]
                    .Replace("\r\n", " ")   // Handle Windows line endings
                    .Replace("\n", " ")     // Handle Unix line endings
                    .WrapAtWordBoundaries(wrapWidth);
                wrappedColumns.Add(chunks);
            }

            int maxLines = wrappedColumns.Max(col => col.Count);
            var wrappedRows = new List<string[]>();

            for (int line = 0; line < maxLines; line++)
            {
                var lineParts = new string[_columns.Count];

                for (int i = 0; i < _columns.Count; i++)
                {
                    var columnLines = wrappedColumns[i];
                    lineParts[i] = line < columnLines.Count ? columnLines[line] : string.Empty;
                }

                wrappedRows.Add(lineParts);
            }

            return wrappedRows;
        }

    }

    internal static class TableBuilderExtensions
    {
        public static List<string> WrapAtWordBoundaries(this string str, int maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrWhiteSpace(str))
            {
                lines.Add(string.Empty);
                return lines;
            }

            var words = str.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0) > maxWidth)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                }

                if (currentLine.Length > 0)
                    currentLine.Append(' ');

                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return lines;
        }
    }

}
