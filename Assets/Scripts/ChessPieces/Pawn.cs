using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (Team == 0) ? 1 : -1;

        // one in front
        if (board[CurrentX, CurrentY + direction] == null)
        {
            r.Add(new Vector2Int(CurrentX, CurrentY + direction));
        }

        // two in front
        if (board[CurrentX, CurrentY + direction] == null)
        {
            if (Team == 0 && CurrentY == 1 && board[CurrentX, CurrentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(CurrentX, CurrentY + (direction * 2)));
            }
            if (Team == 1 && CurrentY == 6 && board[CurrentX, CurrentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(CurrentX, CurrentY + (direction * 2)));
            }
        }

        // kill move
        if (CurrentX != tileCountX - 1)
        {
            if (board[CurrentX + 1, CurrentY + direction] != null && board[CurrentX + 1, CurrentY + direction].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX + 1, CurrentY + direction));
            }
        }
        if (CurrentX != 0)
        {
            if (board[CurrentX - 1, CurrentY + direction] != null && board[CurrentX - 1, CurrentY + direction].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX - 1, CurrentY + direction));
            }
        }

        return r;
    }

    public override SpecialMove GetSpecialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (Team == 0) ? 1 : -1;

        if ((Team == 0 && CurrentY == 6) || (Team == 1 && CurrentY == 1))
        {
            return SpecialMove.Promotion;
        }

        // in passing
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            if (board[lastMove[1].x, lastMove[1].y].ChessPieceType == ChessPieceType.Pawn)
            {
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2)
                {
                    if (board[lastMove[1].x, lastMove[1].y].Team != Team)
                    {
                        if (lastMove[1].y == CurrentY)
                        {
                            if (lastMove[1].x == CurrentX - 1)
                            {
                                availableMoves.Add(new Vector2Int(CurrentX - 1, CurrentY + direction));
                                return SpecialMove.InPassing;
                            }
                            if (lastMove[1].x == CurrentX + 1)
                            {
                                availableMoves.Add(new Vector2Int(CurrentX + 1, CurrentY + direction));
                                return SpecialMove.InPassing;
                            }
                        }
                    }
                }
            }
        }


        return SpecialMove.None;
    }
}
