using System;

public class PositionVector
{
	public int colOffset;
	public int rowOffset;

	public override string ToString()
	{
		return string.Format("({0}, {1})", colOffset, rowOffset);
	}
}

[System.Serializable]
public class Position
{
	static Position[,] cache;
	public int index;
	public int row;
	public int col;
	static LevelModel levelModel;

	Position(int col, int row)
	{
		UnityEngine.Assertions.Assert.IsNotNull(levelModel);
		
		this.col = col;
		this.row = row;
		this.index = row * Position.levelModel.cols + col;
	}

	public static void Setup(LevelModel levelModel)
	{
		Position.levelModel = levelModel;
		cache = new Position[levelModel.rows, levelModel.cols];
	}

	public static Position Get(int col, int row)
	{
		if (col < 0
			|| col >= Position.levelModel.cols
			|| row < 0
			|| row >= Position.levelModel.rows
		) {
			throw new ArgumentException("Bad arguments: (" + col + ", " + row + ")");
		}

		var position = cache[row, col];
		if (position == null) 
		{
			position = new Position(col, row);
			cache[row, col] = position;
		}
		return position;
	}

	public static Position Get(Position sourcePosition, int colOffset, int rowOffset)
	{
		return Get(sourcePosition.col + colOffset, sourcePosition.row + rowOffset);
	}

	public override string ToString()
	{
		return string.Format("({0}, {1})", col, row);
	}
}