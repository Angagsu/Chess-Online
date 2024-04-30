using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialMove
{
    None,
    InPassing,
    Castling,
    Promotion
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;

    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlackes = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private Vector3 tileCenter;

    private bool isWhiteTurn;

    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    private void Start()
    {
        isWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        tileCenter = new Vector3(tileSize / 2, 0, tileSize / 2);
        SpawnAllPieces();
        PositionAllPieces();

        ResgisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = 
                    ContainsValidMove(ref availableMoves, currentHover) ?
                    LayerMask.NameToLayer("Highlight") :
                    LayerMask.NameToLayer("Tile");

                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // Is it our turn?
                    if (chessPieces[hitPosition.x, hitPosition.y].Team == 0 && isWhiteTurn && currentTeam == 0 ||
                        chessPieces[hitPosition.x, hitPosition.y].Team == 1 && !isWhiteTurn && currentTeam == 1)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        specialMove = currentlyDragging.GetSpecialMove(ref chessPieces, ref moveList, ref availableMoves);
                        PreventCheck();
                        HighlightTiles();
                    }
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.CurrentX, currentlyDragging.CurrentY);
                
                if (ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y)))
                {
                    MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    // Net implementation
                    NetMakeMove mm = new NetMakeMove();
                    mm.OriginalX = previousPosition.x;
                    mm.OriginalY = previousPosition.y;
                    mm.DestinationX = hitPosition.x;
                    mm.DestinationY = hitPosition.y;
                    mm.TeamId = currentTeam;
                    Client.Instance.SendToServer(mm);
                }
                else
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = 
                    ContainsValidMove(ref availableMoves, currentHover) ? 
                    LayerMask.NameToLayer("Highlight") : 
                    LayerMask.NameToLayer("Tile");

                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.CurrentX, currentlyDragging.CurrentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;

            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }

    // Highlight Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    // Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spawning of the pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        // Spawning white team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);


        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }


        // Spawning black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }

    }
    private ChessPiece SpawnSinglePiece(ChessPieceType chessPieceType, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)chessPieceType - 1], transform).GetComponent<ChessPiece>();

        cp.ChessPieceType = chessPieceType;
        cp.Team = team;
        cp.GetComponentInChildren<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    // Positioning all pieces
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x,y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].CurrentX = x;
        chessPieces[x, y].CurrentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + tileCenter;
    }

    // Checkmate
    private void Checkmate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnRematchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.TeamId = 0;
            wrm.WantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.TeamId = 1;
            brm.WantRematch = 1;
            Client.Instance.SendToServer(brm);
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.TeamId = currentTeam;
            rm.WantRematch = 1;
            Client.Instance.SendToServer(rm);
        }
    }

    public void GameReset()
    {
        rematchButton.interactable = true;

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // clean up board
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }
        }


        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);

        for (int i = 0; i < deadBlackes.Count; i++)
            Destroy(deadBlackes[i].gameObject);

        deadWhites.Clear();
        deadBlackes.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.TeamId = currentTeam;
        rm.WantRematch = 0;
        Client.Instance.SendToServer(rm);

        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();

        Invoke("ShutdownRelay", 1);

        playerCount = -1;
        currentTeam = -1;
    }

    // SpecialMoves
    private void ProcessSpecialMove()
    {
        if(specialMove == SpecialMove.InPassing)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.CurrentX == enemyPawn.CurrentX)
            {
                if (myPawn.CurrentY == enemyPawn.CurrentY - 1 || myPawn.CurrentY == enemyPawn.CurrentY + 1)
                {
                    if (enemyPawn.Team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8.3f * tileSize, yOffset, -1 * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlackes.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1.3f * tileSize, yOffset, 8f * tileSize)
                            - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadBlackes.Count);
                    }

                    chessPieces[enemyPawn.CurrentX, enemyPawn.CurrentY] = null;
                }
            }
        }

        if (specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.ChessPieceType == ChessPieceType.Pawn)
            {
                if (targetPawn.Team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQuenn = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQuenn.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQuenn;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                if (targetPawn.Team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQuenn = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQuenn.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQuenn;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }

        if (specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            // left rook
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // white side
                {
                    ChessPiece whiteRook = chessPieces[0, 0];
                    chessPieces[3, 0] = whiteRook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) // black side
                {
                    ChessPiece blackRook = chessPieces[0, 7];
                    chessPieces[3, 7] = blackRook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            // right rook
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // white side
                {
                    ChessPiece whiteRook = chessPieces[7, 0];
                    chessPieces[5, 0] = whiteRook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // black side
                {
                    ChessPiece blackRook = chessPieces[7, 7];
                    chessPieces[5, 7] = blackRook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventCheck() 
    {
        ChessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x, y] != null)
                    if (chessPieces[x, y].ChessPieceType == ChessPieceType.King)
                        if (chessPieces[x, y].Team == currentlyDragging.Team)
                            targetKing = chessPieces[x, y];

        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> availbleMoves, ChessPiece targetKing)
    {
        // Save the current values to reset after the function call
        int actualX = cp.CurrentX;
        int actualY = cp.CurrentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Going through all the moves, simulate them and check if we're in check 
        for (int i = 0; i < availbleMoves.Count; i++)
        {
            int simulationX = availbleMoves[i].x;
            int simulationY = availbleMoves[i].y;

            Vector2Int kingPositionThisSimulation = new Vector2Int(targetKing.CurrentX, targetKing.CurrentY);

            // Did we simulate the king's move
            if (cp.ChessPieceType == ChessPieceType.King)
                kingPositionThisSimulation = new Vector2Int(simulationX, simulationY);


            // Copy the [,] and mot a reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simulationAttackingPieces = new List<ChessPiece>();

            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].Team != cp.Team)
                            simulationAttackingPieces.Add(simulation[x, y]);
                    }
                }
            }

            // Simulate that move
            simulation[actualX, actualY] = null;
            cp.CurrentX = simulationX;
            cp.CurrentY = simulationY;
            simulation[simulationX, simulationY] = cp;

            // Did one of the piece got taken down during our simulation
            var deadPiece = simulationAttackingPieces.Find(c => c.CurrentX == simulationX && c.CurrentY == simulationY);
            if (deadPiece != null)
                simulationAttackingPieces.Remove(deadPiece);

            // Get all the simulated attacking pieces moves
            List<Vector2Int> simulationMoves = new List<Vector2Int>();

            for (int a = 0; a < simulationAttackingPieces.Count; a++)
            {
                var pieceMoves = simulationAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simulationMoves.Add(pieceMoves[b]);
                }
            }

            // Is the king in trouble? if so, remove the move
            if (ContainsValidMove(ref simulationMoves, kingPositionThisSimulation))
            {
                movesToRemove.Add(availbleMoves[i]);
            }

            // Restore the actual CP data
            cp.CurrentX = actualX;
            cp.CurrentY = actualY;
        }

        // Remove from the current availble move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            availbleMoves.Remove(movesToRemove[i]);
        }
    }
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = chessPieces[lastMove[1].x, lastMove[1].y].Team == 0 ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].Team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].ChessPieceType == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }
                }

        // Is the king attacked right now
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();

        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }
        // Are we in check right now?
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.CurrentX, targetKing.CurrentY)))
        {
            // king is under attack, can we move something to help him?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }

        return false;
    }
    private void MoveTo(int originalX, int originalY, int x, int y)
    {
        ChessPiece cp = chessPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        if(chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.Team == ocp.Team)
            {
                return;
            }

            if (ocp.Team == 0)
            {
                if (ocp.ChessPieceType == ChessPieceType.King)
                {
                    Checkmate(1);
                }

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8.3f * tileSize, yOffset, -1 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.ChessPieceType == ChessPieceType.King)
                {
                    Checkmate(0);
                }

                deadBlackes.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1.3f * tileSize, yOffset, 8f * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlackes.Count);
            }

        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        if (localGame)
        {
            currentTeam = (currentTeam == 0) ? 1 : 0;
        }

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y)});

        ProcessSpecialMove();

        if(currentlyDragging)
            currentlyDragging = null;

        RemoveHighlightTiles();

        if (CheckForCheckmate())
            Checkmate(cp.Team);

        return;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return -Vector2Int.one;
    }

    #region Events_Lgc

    private void ResgisterEvents()
    {
        NetUtility.SERVER_WELCOME += OnWelcomeServer;
        NetUtility.SERVER_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.SERVER_REMATCH += OnRematchServer;

        NetUtility.CLIENT_WELCOME += OnWelcomeClient;
        NetUtility.CLIENT_START_GAME += OnStartGameClient;
        NetUtility.CLIENT_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.CLIENT_REMATCH += OnRematchClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
    }

    private void UnregisterEvents()
    {
        NetUtility.SERVER_WELCOME -= OnWelcomeServer;
        NetUtility.SERVER_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.SERVER_REMATCH -= OnRematchServer;


        NetUtility.CLIENT_WELCOME -= OnWelcomeClient;
        NetUtility.CLIENT_START_GAME -= OnStartGameClient;
        NetUtility.CLIENT_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.CLIENT_REMATCH -= OnRematchClient;


        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
    }

    // Server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        // Client has connected, assign a team and return the message back to him
        NetWelcome nw = msg as NetWelcome;

        // Assign the team
        nw.AssignedTeam = ++playerCount;

        // Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        // If full, start the game
        if (playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }

    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        // Recive the message, broadcast it back
        NetMakeMove mm = msg as NetMakeMove;

        // Broadcat it back
        Server.Instance.Broadcast(mm);
    }

    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    // Client
    private void OnWelcomeClient(NetMessage msg)
    {
        // Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        // Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My assigned team is {nw.AssignedTeam}");

        if (localGame && currentTeam == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }

    private void OnStartGameClient(NetMessage msg)
    {
        // We need to change the camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.WhiteTeam : CameraAngle.BlackTeam);
    }

    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        Debug.Log($"MM : {mm.TeamId} : {mm.OriginalX} {mm.OriginalY} --> {mm.DestinationX} {mm.DestinationY}");

        if (mm.TeamId != currentTeam)
        {
            ChessPiece target = chessPieces[mm.OriginalX, mm.OriginalY];
            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.GetSpecialMove(ref chessPieces, ref moveList, ref availableMoves);

            MoveTo(mm.OriginalX, mm.OriginalY, mm.DestinationX, mm.DestinationY);
        }
    }

    private void OnRematchClient(NetMessage msg)
    {
        // Receive the connection message
        NetRematch rm = msg as NetRematch;

        // Set the boolean for rematch
        playerRematch[rm.TeamId] = rm.WantRematch == 1;

        // Activate the piece of UI
        if (rm.TeamId != currentTeam)
        {
            rematchIndicator.transform.GetChild((rm.WantRematch == 1) ? 0 : 1).gameObject.SetActive(true);
            if (rm.WantRematch != 1)
                rematchButton.interactable = false;
        }

        // If both wants to rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }

    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

    private void OnSetLocalGame(bool isSetLocal)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = isSetLocal;
    }

    #endregion
}
