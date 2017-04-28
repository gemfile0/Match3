public class PositionVector
{
	public int colOffset;
	public int rowOffset;
}

[System.Serializable]
public class Position
{
	public int index;
	public int row;
	public int col;
	public static LevelModel levelModel;
	public Position(int col, int row)
	{
		UnityEngine.Assertions.Assert.IsNotNull(levelModel);
		
		this.col = col;
		this.row = row;
		this.index = row * Position.levelModel.cols + col;
	}

	public Position(int pivotIndex, int colOffset, int rowOffset)
	{
		UnityEngine.Assertions.Assert.IsNotNull(levelModel);
		
		row = (pivotIndex / Position.levelModel.cols) + rowOffset;
		col = (pivotIndex % Position.levelModel.cols) + colOffset;
		index = row * Position.levelModel.cols + col;
	}

	public bool IsAcceptableIndex()
	{
		return col >= 0
			&& col <  Position.levelModel.cols
			&& row >= 0
			&& row <  Position.levelModel.rows;
	}

	public override string ToString()
	{
		return string.Format("({0}, {1})", col, row);
	}
}