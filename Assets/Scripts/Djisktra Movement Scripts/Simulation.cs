using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;
using UnityStandardAssets.Characters.ThirdPerson;

public class Simulation : MonoBehaviour {
    public Grid Grid;
    //public GameObject PedestrianContainer;
    public bool IsSimulating {get; private set;}
    public int CurrentFrame = 0;
    public int RecordedFrames = 0;
    [SerializeField] public NewPlayer[] Players;
    //[HideInInspector] public Pedestrian[] Pedestrians;
    private float[,] dijkstraField;
    private float dijkstraMax = 0f;
    private List<NewPlayer>[,] playerField;

    //[HideInInspector] public ThirdPersonCharacter character;
    //private Animator animator; //animator
    // public NavMeshAgent agent; //agent

    private void Awake() {
        //Pedestrians = PedestrianContainer.GetComponentsInChildren<Pedestrian>();
        //agent = this.GetComponent<NavMeshAgent>(); //gets the navmesh agent
        //character = this.GetComponent<ThirdPersonCharacter>();
        //animator = this.GetComponent<Animator>();
    }

    /**
    *** Creates dijkstra distance field
    *** Also finds the maximum value in dijkstra field, later to be used for dijkstra distance field visualization
    **/
    private void Start() {
        var start = Time.realtimeSinceStartup;
        dijkstraField = Pathfinding.CreateDijkstraField(Grid.Cols, Grid.Rows, Grid.GridContent);
        Debug.Log($"It takes: {Time.realtimeSinceStartup - start} seconds");
        foreach(var i in dijkstraField) {
            if(i > dijkstraMax && i < Mathf.Infinity) {
                dijkstraMax = i;
            }
        }
        SetupPlayerDensityField();
    }

    /**
    *** Creates a 2D array of Pedestrian Lists to store which pedestrians are registered to cells.
    **/
    private void SetupPlayerDensityField() {
        playerField = new List<NewPlayer>[Grid.Cols, Grid.Rows];
        for(var x = 0; x < Grid.Cols; x++) {
            for(var y = 0; y < Grid.Rows; y++) {
                playerField[x, y] = new List<NewPlayer>();
            }
        }
        foreach(var player in Players) {
            var cell = Grid.GetCellCoordinate(player.transform.position);
            playerField[cell.x, cell.y].Add(player);
            player.CurrentCell = cell;
            player.AddToPositionHistory(player.transform.position);
        }
    }
    
    /**
    *** Runs 50 times a second.
    *** Processes every pedestrian.
    *** Gets all the neighbors of a pedestrian, calls the utility function for each one of them.
    *** Decides on one of the neighbors to move towards, creates a direction vector.
    *** Adjusts speed if it's too close to another pedestrian
    *** Saves its new position to its history
    **/
    private void FixedUpdate() {
        if(!IsSimulating) {
            return;
        }
        
        CurrentFrame++;
        RecordedFrames++;

        foreach(var player in Players) {
            var pos = player.transform.position;
            var cell = Grid.GetCellCoordinate(pos);

            if(player.CurrentCell != cell) {
                playerField[cell.x, cell.y].Add(player);
                playerField[player.CurrentCell.x, player.CurrentCell.y].Remove(player);
                player.CurrentCell = cell;
            }

            var neighbors = Pathfinding.GetAllNeighbors(cell, Grid.Cols, Grid.Rows, Grid.GridContent);

            float currentMinCost = Mathf.Infinity;
            Vector2Int currentSelectedCell = Vector2Int.one * -1;
            
            foreach(var neighbor in neighbors) {
                var cost = GetCost(neighbor, player);

                if(cost < currentMinCost) {
                    currentSelectedCell = neighbor;
                    currentMinCost = cost;
                }
            }
            
            float currentNearestPlayerDistance = Mathf.Infinity;
            var directionVector = Vector3.zero;
            if(currentSelectedCell != Vector2Int.one * -1 && currentSelectedCell != cell) {
                player.TargetCell = currentSelectedCell;
                var targetPos = Grid.GetCoordinateFromCell(currentSelectedCell);

                directionVector += new Vector3(targetPos.x - pos.x, 0, targetPos.y - pos.z);

                foreach(var neighbor in neighbors) {
                    foreach(var player2 in playerField[neighbor.x, neighbor.y]) {
                        if(player == player2) {
                            continue;
                        }

                        if(player2.nearestPlayer == player) {
                            continue;
                        }
                        
                        var dist1 = Vector3.Distance(player2.transform.position, player.transform.position);
                        var dist2 = Vector3.Distance(player2.transform.position, player.transform.position + directionVector.normalized * 0.1f);
                       
                        if(dist2 < dist1 && dist1 < currentNearestPlayerDistance) {
                            currentNearestPlayerDistance = dist1;
                            player.nearestPlayer = player2;
                        }
                    }
                }
            }

            player.CurrentSpeed = Mathf.Max(Mathf.Min((currentNearestPlayerDistance - 1f) / 10f, 0.01f), 0);
            player.transform.position = pos + directionVector.normalized * player.CurrentSpeed;
            player.AddToPositionHistory(player.transform.position);

            //ThirdPersonCharacter character = player.getThirdPersonCharacter();
            //character.Move(directionVector.normalized * player.CurrentSpeed, false, false);
        }
    }

    public void StartSimulation() {
        if(RecordedFrames == 0) {
            IsSimulating = true;
        }
        else {
            foreach(var player in Players) {
                player.transform.position = player.previousPositions[0];
                player.previousPositions.Clear();
            }
            SetupPlayerDensityField();
            CurrentFrame = 0;
            RecordedFrames = 0;
            IsSimulating = true;
        }
    }

    public void StopSimulation() {
        IsSimulating = false;
    }

    public void Seek(int frameNumber) {
        CurrentFrame = frameNumber;
        foreach(var player in Players) {
            player.transform.position = player.previousPositions[Mathf.Min(player.previousPositions.Count - 1, CurrentFrame)];
        }
    }

    /**
    *** Utility (cost) function.
    *** Calculate the distance to all other pedestrians if the current pedestrian decides to the "pos"
    *** Sum it with dijkstra distance field value.
    **/
    private float GetCost(Vector2Int pos, NewPlayer player) {
        var neighbors = Pathfinding.GetAllNeighbors(pos, Grid.Cols, Grid.Rows, Grid.GridContent);
        var extraCosts = 0f;

        var targetPos = Grid.GetCoordinateFromCell(pos);
        var playerPos = player.transform.position;
        var velocity = new Vector3(targetPos.x - playerPos.x, 0, targetPos.y - playerPos.z).normalized * 0.1f;
        var newPos = playerPos + velocity;

        foreach(var neighbor in neighbors) {            
            foreach(var player2 in playerField[neighbor.x, neighbor.y]) {
                extraCosts += 5 * Mathf.Exp(-1 * Vector3.Distance(player2.transform.position, newPos));
            }
        }

        return dijkstraField[pos.x, pos.y] + extraCosts;
    }

    /**
    *** Used to visualize dijkstra field on the grid.
    *** This doesn't have to be in a Update function.
    *** But we used it to highlight cells that have a pedestrian on them. 
    **/
    private void Update() {
        if(UIManager.Instance.ShowDijkstraField) {
            var colors = new Color[Grid.MeshFilter.mesh.vertexCount];
            for(var x = 0; x < Grid.Cols; x++) {
                for(var y = 0; y < Grid.Rows; y++) {
                    var color = Color.Lerp(Color.black, Color.white, dijkstraField[x, y]/dijkstraMax);
                    
                    var val = y * Grid.Cols + x;
                    colors[val * 6] = color;
                    colors[val * 6 + 1] = color;
                    colors[val * 6 + 2] = color;
                    colors[val * 6 + 3] = color;
                    colors[val * 6 + 4] = color;
                    colors[val * 6 + 5] = color;
                }
            }
            Grid.MeshFilter.mesh.SetColors(colors);
        }   
    }
}