using System.Text.Json.Nodes;
using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public class TableTransformService : ITableTransformService
{
    /// <inheritdoc />
    public JsonArray BuildGenericTables(IReadOnlyList<DocumentTableDto> tables)
    {
        var output = new JsonArray();

        for (int t = 0; t < tables.Count; t++)
        {
            var table = tables[t];
            var cells = table.Cells;

            // Detect header row: row 0 cells with Kind == "columnHeader"
            bool hasHeaderRow = cells.Any(c => c.RowIndex == 0
                && !string.IsNullOrEmpty(c.Kind)
                && c.Kind == "columnHeader");

            // Heuristic: a 2-column table where every row is label→value
            // is key-value even if row 0 is marked as header
            bool isKeyValue = table.ColumnCount == 2;

            // Build header map (column index → header name) with span dedup
            var headers = new Dictionary<int, string>();
            if (hasHeaderRow && !isKeyValue)
            {
                foreach (var cell in cells.Where(c => c.RowIndex == 0).OrderBy(c => c.ColumnIndex))
                {
                    int span = cell.ColumnSpan ?? 1;
                    for (int cs = 0; cs < span; cs++)
                    {
                        string name = cell.Content;
                        // Disambiguate duplicate names from ColumnSpan
                        if (span > 1 && cs > 0)
                            name = $"{cell.Content}_{cs + 1}";
                        headers[cell.ColumnIndex + cs] = name;
                    }
                }
            }

            var tableObj = new JsonObject
            {
                ["tableIndex"] = t,
                ["rowCount"] = table.RowCount,
                ["columnCount"] = table.ColumnCount,
                ["type"] = isKeyValue ? "key-value" : "columnar",
                ["page"] = table.BoundingRegions.Count > 0 ? table.BoundingRegions[0].PageNumber : 0
            };

            if (isKeyValue)
            {
                BuildKeyValueData(cells, tableObj);
            }
            else
            {
                BuildColumnarData(cells, headers, hasHeaderRow, table, tableObj);
            }

            output.Add(tableObj);
        }

        return output;
    }

    private static void BuildKeyValueData(List<TableCellDto> cells, JsonObject tableObj)
    {
        var kvPairs = new JsonObject();
        foreach (var cell in cells.Where(c => c.ColumnIndex == 0).OrderBy(c => c.RowIndex))
        {
            string key = cell.Content?.Trim()!;
            if (string.IsNullOrEmpty(key)) key = $"row_{cell.RowIndex}";
            string value = cells
                .FirstOrDefault(c => c.RowIndex == cell.RowIndex && c.ColumnIndex == 1)?
                .Content?.Trim() ?? "";
            kvPairs[key] = value;
        }
        tableObj["data"] = kvPairs;
    }

    private static void BuildColumnarData(
        List<TableCellDto> cells,
        Dictionary<int, string> headers,
        bool hasHeaderRow,
        DocumentTableDto table,
        JsonObject tableObj)
    {
        if (headers.Count > 0)
        {
            var headersArray = new JsonArray();
            foreach (var h in headers.OrderBy(kv => kv.Key))
                headersArray.Add(h.Value);
            tableObj["headers"] = headersArray;
        }

        int dataStartRow = hasHeaderRow ? 1 : 0;
        var rows = new JsonArray();

        for (int r = dataStartRow; r < table.RowCount; r++)
        {
            var rowCells = cells.Where(c => c.RowIndex == r).OrderBy(c => c.ColumnIndex).ToList();

            // Is this a spanning section-header row?
            bool isSpanningRow = rowCells.Any(c => (c.ColumnSpan ?? 1) > table.ColumnCount / 2);

            if (isSpanningRow && rowCells.Count <= 2)
            {
                var spanContent = string.Join(" ",
                    rowCells.Select(c => c.Content?.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                if (!string.IsNullOrEmpty(spanContent))
                {
                    rows.Add(new JsonObject
                    {
                        ["_type"] = "section-header",
                        ["content"] = spanContent
                    });
                }
            }
            else
            {
                var rowObj = new JsonObject();
                bool allEmpty = true;

                foreach (var cell in rowCells)
                {
                    string content = cell.Content?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(content)) allEmpty = false;

                    string colName = headers.ContainsKey(cell.ColumnIndex)
                        ? headers[cell.ColumnIndex]
                        : $"col_{cell.ColumnIndex}";
                    rowObj[colName] = content;
                }

                if (!allEmpty)
                    rows.Add(rowObj);
            }
        }
        tableObj["rows"] = rows;
    }
}
