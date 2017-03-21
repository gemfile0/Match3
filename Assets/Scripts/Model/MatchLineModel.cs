using System.Collections.Generic;

public class MatchLineModel {
    public int cols;
    public int rows;
    public List<WhereCanMatch> wheresCanMatch;

    public MatchLineModel(int cols, int rows) {
        this.cols = cols;
        this.rows = rows;

        wheresCanMatch = new List<WhereCanMatch>();
        
        var startRow = -(rows-1);
        for(var row = 0; row < rows; row++) {
            var startCol = -(cols-1);
            for(var col = 0; col < cols; col++) {
                wheresCanMatch.Add(new WhereCanMatch(startCol, cols, startRow, rows));
                startCol++;
            }
            startRow++;
        }
    }
}

public class WhereCanMatch {
    public List<int[]> matchOffsets;

    public WhereCanMatch(int startCol, int cols, int startRow, int rows) {
        matchOffsets = new List<int[]>();
        for(var row = 0; row < rows; row++) {
            for(var col = 0; col < cols; col++) {
                matchOffsets.Add(new int[2]{ startCol + col, startRow + row });
            }
        }
    }
}