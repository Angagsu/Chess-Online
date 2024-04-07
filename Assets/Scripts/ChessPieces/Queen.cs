using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // going down
        for (int i = CurrentY - 1; i >= 0; i--)
        {
            if (board[CurrentX, i] == null)
            {
                r.Add(new Vector2Int(CurrentX, i));
            }

            if (board[CurrentX, i] != null)
            {
                if (board[CurrentX, i].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX, i));
                }

                break;
            }
        }

        // going up
        for (int i = CurrentY + 1; i < tileCountY; i++)
        {
            if (board[CurrentX, i] == null)
            {
                r.Add(new Vector2Int(CurrentX, i));
            }

            if (board[CurrentX, i] != null)
            {
                if (board[CurrentX, i].Team != Team)
                {
                    r.Add(new Vector2Int(CurrentX, i));
                }

                break;
            }
        }

        // going left
        for (int i = CurrentX - 1; i >= 0; i--)
        {
            if (board[i, CurrentY] == null)
            {
                r.Add(new Vector2Int(i, CurrentY));
            }

            if (board[i, CurrentY] != null)
            {
                if (board[i, CurrentY].Team != Team)
                {
                    r.Add(new Vector2Int(i, CurrentY));
                }

                break;
            }
        }

        // going right
        for (int i = CurrentX + 1; i < tileCountX; i++)
        {
            if (board[i, CurrentY] == null)
            {
                r.Add(new Vector2Int(i, CurrentY));
            }

            if (board[i, CurrentY] != null)
            {
                if (board[i, CurrentY].Team != Team)
                {
                    r.Add(new Vector2Int(i, CurrentY));
                }

                break;
            }
        }



        // top right move
        for (int x = CurrentX + 1, y = CurrentY + 1; x < tileCountX && y < tileCountY; x++, y++)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].Team != Team)
                {
                    r.Add(new Vector2Int(x, y));
                }

                break;
            }
        }

        // top left move
        for (int x = CurrentX - 1, y = CurrentY + 1; x >= 0 && y < tileCountY; x--, y++)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].Team != Team)
                {
                    r.Add(new Vector2Int(x, y));
                }

                break;
            }
        }

        // bottom right move
        for (int x = CurrentX + 1, y = CurrentY - 1; x < tileCountX && y >= 0; x++, y--)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].Team != Team)
                {
                    r.Add(new Vector2Int(x, y));
                }

                break;
            }
        }

        // bottom left move
        for (int x = CurrentX - 1, y = CurrentY - 1; x >= 0 && y >= 0; x--, y--)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].Team != Team)
                {
                    r.Add(new Vector2Int(x, y));
                }

                break;
            }
        }



        return r;
    }
}
