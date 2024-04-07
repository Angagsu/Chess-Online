using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // right
        if (CurrentX + 1 < tileCountX)
        {
            // right
            if (board[CurrentX + 1, CurrentY] == null)
            {
                r.Add(new Vector2Int(CurrentX + 1, CurrentY));
            }
            else if (board[CurrentX + 1, CurrentY].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX + 1, CurrentY));
            }

            // top right
            if (CurrentY + 1 < tileCountY)
            {
                if (board[CurrentX + 1, CurrentY + 1] == null)
                {
                    r.Add(new Vector2Int(CurrentX + 1, CurrentY + 1));
                }
                else if (board[CurrentX + 1, CurrentY + 1].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX + 1, CurrentY + 1));
                }
            }

            // bottom right
            if (CurrentY - 1 >= 0)
            {
                if (board[CurrentX + 1, CurrentY - 1] == null)
                {
                    r.Add(new Vector2Int(CurrentX + 1, CurrentY - 1));
                }
                else if (board[CurrentX + 1, CurrentY - 1].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX + 1, CurrentY - 1));
                }
            }

        }

        // left
        if (CurrentX - 1 >= 0)
        {
            // left
            if (board[CurrentX - 1, CurrentY] == null)
            {
                r.Add(new Vector2Int(CurrentX - 1, CurrentY));
            }
            else if (board[CurrentX - 1, CurrentY].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX - 1, CurrentY));
            }

            // top left
            if (CurrentY + 1 < tileCountY)
            {
                if (board[CurrentX - 1, CurrentY + 1] == null)
                {
                    r.Add(new Vector2Int(CurrentX - 1, CurrentY + 1));
                }
                else if (board[CurrentX - 1, CurrentY + 1].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX - 1, CurrentY + 1));
                }
            }

            // bottom left
            if (CurrentY - 1 >= 0)
            {
                if (board[CurrentX - 1, CurrentY - 1] == null)
                {
                    r.Add(new Vector2Int(CurrentX - 1, CurrentY - 1));
                }
                else if (board[CurrentX - 1, CurrentY - 1].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX - 1, CurrentY - 1));
                }
            }

        }

        // up
        if (CurrentY + 1 < tileCountY)
        {
            if (board[CurrentX, CurrentY + 1] == null || board[CurrentX, CurrentY + 1].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX, CurrentY + 1));
            }
        }

        // down
        if (CurrentY - 1 >= 0)
        {
            if (board[CurrentX, CurrentY - 1] == null || board[CurrentX, CurrentY - 1].Team != Team)
            {
                r.Add(new Vector2Int(CurrentX, CurrentY - 1));
            }
        }


        return r;
    }

    public override SpecialMove GetSpecialMove(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove sm = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((Team == 0) ? 0 : 7));
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((Team == 0) ? 0 : 7));
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((Team == 0) ? 0 : 7));

        if (kingMove == null && CurrentX == 4)
        {
            if (Team == 0)
            {
                // left rook
                if (leftRook == null)
                    if (board[0, 0].ChessPieceType == ChessPieceType.Rook)
                        if (board[0, 0].Team == 0)
                            if (board[3, 0] == null)
                                if (board[2, 0] == null)
                                    if (board[1, 0] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 0));
                                        sm = SpecialMove.Castling;
                                    }

                // right rook
                if (rightRook == null)
                    if (board[7, 0].ChessPieceType == ChessPieceType.Rook)
                        if (board[7, 0].Team == 0)
                            if (board[5, 0] == null)
                                if (board[6, 0] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 0));
                                    sm = SpecialMove.Castling;
                                }
            }
            else
            {
                // left rook
                if (leftRook == null)
                    if (board[0, 7].ChessPieceType == ChessPieceType.Rook)
                        if (board[0, 7].Team == 1)
                            if (board[3, 7] == null)
                                if (board[2, 7] == null)
                                    if (board[1, 7] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 7));
                                        sm = SpecialMove.Castling;
                                    }

                // right rook
                if (rightRook == null)
                    if (board[7, 7].ChessPieceType == ChessPieceType.Rook)
                        if (board[7, 7].Team == 1)
                            if (board[5, 7] == null)
                                if (board[6, 7] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 7));
                                    sm = SpecialMove.Castling;
                                }
            }
        }

        return sm;
    }
}
