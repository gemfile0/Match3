public class Position
{
	public int index;
	public int row;
	public int col;
	public static int Cols = 0;
	public static int Rows = 0;

	public Position(int index) 
	{
        UnityEngine.Assertions.Assert.AreNotEqual(0, Cols);
		UnityEngine.Assertions.Assert.AreNotEqual(0, Rows);
		
		this.index = index;
		row = index / Position.Cols;
		col = index % Position.Cols;
	}

	public Position(int pivotIndex, int colOffset, int rowOffset)
	{
		row = (pivotIndex / Position.Cols) + rowOffset;
		col = (pivotIndex % Position.Cols) + colOffset;
		index = row * Position.Cols + col;
	}

	public bool IsAcceptableIndex() 
	{
		return col >= 0 && col < Position.Cols && row >= 0 && row < Position.Rows;
	}
}