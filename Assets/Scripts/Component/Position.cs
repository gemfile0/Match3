public class Position
{
	public int index;
	public int row;
	public int col;
	public static int Cols = 1;
	public static int Rows = 1;

	public Position(int index) 
	{
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