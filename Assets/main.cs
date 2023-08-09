//attach to camera

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

using Utils;

using System.Threading;

public class main : MonoBehaviour {
    //terrain gen
    public static int worldWidth = 200;
    public static int worldHeight = 200;
    public static float perlinStrideX = .04f;
    public static float perlinStrideY = .02f;
    public static float perlin2StrideX = .2f;
    public static float perlin2StrideY = .1f;
    public static float forestPerlinStrideX = .02f;
    public static float forestPerlinStrideY = .02f;
    public static float waterThreshold = 0.38f;
    public static float mountainThreshold = 0.72f;
    public static float forestThreshold = 0.4f;
    public static float mineralPerlinStrideX = .03f;
    public static float mineralPerlinStrideY = .015f;
    public static float ironThreshold = 0.15f;
    public static float coalThreshold = 0.8f;
    
    public static int dirtCount = 0;
    public static int waterCount = 0;
    public static int mountainCount = 0;
    public static int forestCount = 0;
    public static int stoneCount = 0;
    public static int ironCount = 0;
    public static int coalCount = 0;
    
    public static byte logsInForest = 3;
    public static bool generatingWorld = true;
    
    public static int view = 0; //0 normal, 1 mineral
    public static bool mineralViewAvailable = true;
    
    //set in unity editor
    public Grid _grid;
    public static Grid grid;
    public Tilemap tilemap;
    public Tilemap mineralTilemap;
    public Tilemap foregroundTilemap;
    public Tilemap _arrowsTilemap;
    public static Tilemap arrowsTilemap;
    public Tile dirt;
    public Tile water;
    public Tile mountain;
    public Tile forest;
    public Tile stone;
    public Tile coal;
    public Tile iron;
    public Tile red;
    public Tile clear;
    public Tile keep;
    public Tile sawmill;
    public Tile coalRefinery;
    public Tile wall;
    public Tile mine;
    public Tile crossbowBase;
    public Tile machinegunBase;
    public Tile sniperBase;
    public TextMeshProUGUI middleText;
    public TextMeshProUGUI bottomLeftText;
    public TextMeshProUGUI topLeftText;
    public TextMeshProUGUI topRightText;
    
    public Sprite sawmillDroneSprite;
    public Sprite _crossbowSprite;
    public static Sprite crossbowSprite;
    public Sprite _gruntSprite;
    public static Sprite gruntSprite;
    public Sprite _grunt2Sprite;
    public static Sprite grunt2Sprite;
    public Sprite _grunt3Sprite;
    public static Sprite grunt3Sprite;
    public Sprite _arrowSprite;
    public static Sprite arrowSprite;
    
    public Sprite _bullet1Sprite;
    public static Sprite bullet1Sprite;
    public Sprite _bullet2Sprite;
    public static Sprite bullet2Sprite;
    
    public Sprite _sniperGunSprite;
    public static Sprite sniperGunSprite;
    public Sprite _machineGunSprite;
    public static Sprite machineGunSprite;
    
    //camera
    //public Camera camera;
    public Vector3 camPos;
    public float camSpeed;
    
    public Tile _arrowE;
    public static Tile arrowE;
    public Tile _arrowNE;
    public static Tile arrowNE;
    public Tile _arrowN;
    public static Tile arrowN;
    public Tile _arrowNW;
    public static Tile arrowNW;
    public Tile _arrowW;
    public static Tile arrowW;
    public Tile _arrowSW;
    public static Tile arrowSW;
    public Tile _arrowS;
    public static Tile arrowS;
    public Tile _arrowSE;
    public static Tile arrowSE;
    
    public static Vector3Int lastMouseCell = new Vector3Int(0,0,0);
    public static Tile selectedTile;
    
    //maps
    static float[,] heightMap;
    static byte[,] forestMap;
    static byte[,] mineralMap; //1 is iron, 2 is coal
    static byte[,] buildingMap;
    static PathingNode[,] pathingMap;
    
    //key presses
    static bool wpressed = false;
    static bool apressed = false;
    static bool spressed = false;
    static bool dpressed = false;
    
    //true if 2 tiles are diagonal of eachother
    public static bool isDiagonal(Vector3Int a, Vector3Int b) {
        Vector3Int c = a - b;
        return (c.x != 0 && c.y != 0);
    }
    
    public static LinkedList<Vector3Int> neighborTiles(Vector3Int pos) {
        LinkedList<Vector3Int> list = new LinkedList<Vector3Int>();
        if (pos.x > 0) {
            list.AddLast(pos + Vector3Int.left);
            if (pos.y > 0) {list.AddLast(pos + new Vector3Int(-1,-1,0));}
            if (pos.y < worldHeight - 1) {list.AddLast(pos + new Vector3Int(-1,1,0));}
        }
        if (pos.x < worldWidth - 1) {
            list.AddLast(pos + Vector3Int.right);
            if (pos.y > 0) {list.AddLast(pos + new Vector3Int(1,-1,0));}
            if (pos.y < worldHeight - 1) {list.AddLast(pos + new Vector3Int(1,1,0));}
        }
        if (pos.y > 0) {list.AddLast(pos + Vector3Int.down);}
        if (pos.y < worldHeight - 1) {list.AddLast(pos + Vector3Int.up);}
        
        return list;
    }
    
    //each node has path
    public class PathingNode {
        public float pathCost;
        public float nodeCost;
        public PathingNode nextNode;
        public int x;
        public int y;
        public uint unum;
        
        public PathingNode(float a, int b, int c) {
            nodeCost = a;
            pathCost = 100000000.0f;
            unum = 0;
            x=b;
            y=c;
        }
    }
    
    public static float sqrt2 = Mathf.Sqrt(2.0f);
    
    public static bool pathMapEdited = false;
    public static float pathEditInterval = 2.0f;
    public static float pathEditProg = 0.0f;
    public static Thread pathThread;
    
    //List<Vector3Int> updateVec3List;
    //List<Tile> updateTileList;
    
    public static void updateNode(Vector3Int node) {
        if (node.x == keepCell.x && node.y == keepCell.y) {return;}
        
        float least = 9999999999.0f;
        PathingNode pnode = pathingMap[node.x, node.y];
        
        float originalCost = pnode.pathCost;
        
        LinkedList<Vector3Int> neighbors = neighborTiles(node);
        
        //find neighbor node that is cheapest.
        foreach (Vector3Int n in neighbors) {
            PathingNode pn = pathingMap[n.x, n.y];
            float cost = pnode.nodeCost;
            
            if (isDiagonal(node, n)) {
                cost *= sqrt2;
            }
            float test = pn.pathCost + cost;
            if (test < least) {
                least = test;
                pnode.nextNode = pn;
                
                //Vector3Int diff = n - node;
                
                //Tile tile;
                //if (diff.x == 1 && diff.y == 0) {tile = arrowE;}
                //else if (diff.x == 1 && diff.y == 1) {tile = arrowNE;}
                //else if (diff.x == 0 && diff.y == 1) {tile = arrowN;}
                //else if (diff.x == -1 && diff.y == 1) {tile = arrowNW;}
                //else if (diff.x == -1 && diff.y == 0) {tile = arrowW;}
                //else if (diff.x == -1 && diff.y == -1) {tile = arrowSW;}
                //else if (diff.x == 0 && diff.y == -1) {tile = arrowS;}
                //else if (diff.x == 1 && diff.y == -1) {tile = arrowSE;}
            }
        }
        
        pnode.pathCost = least;
        //arrowsTilemap.SetColor(node, new Color(least / 256.0f, least / 256.0f, least / 256.0f, 1.0f));
        //arrowsTilemap.SetTile(node, dirt);
    }
    
    public static uint updateNum = 0;
    
    public static void regenPathmap() {
        if (!pathMapEdited) {
            return;
        }
        
        pathMapEdited = false;
        
        updateNum+=1;
        
        
        for (int i = 0; i < worldWidth; i++) {
            for (int j = 0; j < worldHeight; j++) {
                pathingMap[i, j].pathCost = 10000000000000000000.0f;
            }
        }
        
        pathingMap[keepCell.x, keepCell.y].pathCost = 0;
        pathingMap[keepCell.x, keepCell.y].nodeCost = 0;
        pathingMap[keepCell.x, keepCell.y].nextNode = pathingMap[keepCell.x, keepCell.y];
        
        var pq = new PriorityQueue<Vector3Int, float>();
                    
        pq.Enqueue(keepCell, 0);
        
        
        while (pq.Count > 0) {
            Vector3Int lowest = pq.Dequeue();
            foreach (Vector3Int n in neighborTiles(lowest)) {
                PathingNode pn = pathingMap[n.x,n.y];
                if (pn.unum != updateNum) {
                    updateNode(n);
                    pq.Enqueue(n, pn.pathCost);
                    pn.unum = updateNum;
                }
            }
        }
    }
    
    
    void genMap() {
        float perlinRandomX = Random.value * 1000.0f;
        float perlinRandomY = Random.value * 1000.0f;
        float perlin2RandomX = Random.value * 1000.0f;
        float perlin2RandomY = Random.value * 1000.0f;
        float forestPerlinX = Random.value * 1000.0f;
        float forestPerlinY = Random.value * 1000.0f;
        float mineralPerlinX = Random.value * 1000.0f;
        float mineralPerlinY = Random.value * 1000.0f;
        
        dirtCount = 0;
        waterCount = 0;
        mountainCount = 0;
        forestCount = 0;
        stoneCount = 0;
        ironCount = 0;
        coalCount = 0;
        
        
        heightMap = new float[worldWidth,worldHeight];
        forestMap = new byte[worldWidth,worldHeight];
        mineralMap = new byte[worldWidth,worldHeight];
        buildingMap = new byte[worldWidth,worldHeight];
        
        float x = perlinRandomX;
        float x2 = perlin2RandomX;
        float fx = forestPerlinX;
        float mx = mineralPerlinX;
        
        for (int i = 0; i < worldWidth; i++) {
            float y = perlinRandomY;
            float y2 = perlin2RandomY;
            float fy = forestPerlinY;
            float my = mineralPerlinY;
            for (int j = 0; j < worldHeight; j++) {
                float val = Mathf.PerlinNoise(x, y) * 0.75f+ Mathf.PerlinNoise(x2,y2) * 0.25f;
                heightMap[i,j] = val;
                forestMap[i,j] = 0;
                
                Tile tile;
                if (val < waterThreshold) {tile = water;waterCount+=1;}
                else if (val > mountainThreshold) {tile = mountain;mountainCount+=1;}
                else if (Mathf.PerlinNoise(fx, fy) < forestThreshold) {
                    tile = forest;
                    forestCount+=1;
                    forestMap[i,j] = logsInForest;
                } else {tile = dirt;dirtCount+=1;}
                
                tilemap.SetTile(new Vector3Int(i,j,0), tile);
                
                float val2 = Mathf.PerlinNoise(mx, my) * 0.95f + Random.value * 0.05f;
                
                Tile tile2;
                if (val2 < ironThreshold) {tile2 = iron; mineralMap[i,j] = 1; ironCount += 1;}
                else if (val2 > coalThreshold) {tile2 = coal; mineralMap[i,j] = 2; coalCount += 1;}
                else {tile2 = stone; mineralMap[i,j] = 0; stoneCount++;}
                
                mineralTilemap.SetTile(new Vector3Int(i,j,0), tile2);
                
                y += perlinStrideY;
                y2 += perlin2StrideY;
                fy += forestPerlinStrideY;
                my += mineralPerlinStrideY;
            }
            x += perlinStrideX;
            x2 += perlin2StrideX;
            fx += forestPerlinStrideX;
            mx += mineralPerlinStrideX;
        }    
    }
    
    //gen map with logs and constraints so map should be playable
    static int minIron = 100;
    static int minCoal = 100;
    
    void metaGenMap() {
        bool a = true;
        while (a) {
            a = false;
            genMap();
            if (ironCount < minIron) {a = true;continue;}
            if (coalCount < minCoal) {a = true;continue;}
        }
        
        Debug.Log("Generating world");
        Debug.Log("dirt count:     " + dirtCount.ToString());
        Debug.Log("water count:    " + waterCount.ToString());
        Debug.Log("mountain count: " + mountainCount.ToString());
        Debug.Log("forest count:   " + forestCount.ToString());
        Debug.Log("stone count:    " + stoneCount.ToString());
        Debug.Log("iron count:     " + ironCount.ToString());
        Debug.Log("coal count:     " + coalCount.ToString());
    }
    
    //buildings stuff
    public static Vector3Int keepCell;
    
    //economy
    //public static bool sawmillUnlocked = true;
    public static bool mine1Unlocked = false;
    
    //military
    public static bool wallUnlocked = false;
    public static bool crossbowUnlocked = false;
    
    
    
    public static int numSawmills = 0;
    public static int numCoalRefineries = 0;
    public static int numMine1s = 0;
    public static int numWalls = 0;
    public static int numCrossbows = 0;
    public static int numMachineguns = 0;
    public static int numSnipers = 0;
    
    public static int numIronMines = 0;
    public static int numCoalMines = 0;
    
    public static float coalRefineryProgress = 0.0f;
    public static float coalRefineryProgressStride = 0.0f;
    public static float ironMineProgress = 0.0f;
    public static float ironMineProgressStride = 0.0f;
    public static float coalMineProgress = 0.0f;
    public static float coalMineProgressStride = 0.0f;
    
    
    public static List<Sawmill> sawmills;
    public static LinkedList<Turret> turrets;
    public static LinkedList<Enemy> enemies;
    public static LinkedList<Projectile> projectiles;
    
    public static int globalWood = 50;
    public static int globalCoal = 0;
    public static int globalIron = 0;
    
    //public static int woodStorage = 50;
    //public static int coalStorage = 50;
    //public static int ironStorage = 50;
    
    public static int sawmillCost = 50; //wood
    public static int coalRefineryCost = 80; //wood
    public static int mine1Cost = 100; //wood or iron
    public static int wallCost = 10; //wood or iron
    public static int crossbowCost = 80; //wood
    public static int machinegunCost = 100; // iron
    public static int sniperCost = 100; // iron
    
    public static Vector3 halfTile;
    
    
    //enemy spawning
    public static int enemyFrames = 0;
    public static int enemyFramesNeeded = 60 * 80; //30fps assumed, 80 seconds
    public static int spawnLocation; //0 is right, 1 is top, etc
    
    public static float waveStrength = 5.0f; //1 is 1 grunt
    public static float waveStrengthIncrement = 5.0f;
    public static int waveNumber = 1;
    public static float waveStrengthMultiplier = 1.3f;
    
    public static bool pathingView = false;
    public static bool debugMode = false;
    
    string[] spawnLocations;
    
    // Start is called before the first frame update
    void Start()
    {
        metaGenMap();
        selectedTile = keep;
        
        sawmills = new List<Sawmill>();
        turrets = new LinkedList<Turret>();
        enemies = new LinkedList<Enemy>();
        projectiles = new LinkedList<Projectile>();
        pathingMap = new PathingNode[worldWidth, worldHeight];
        spawnLocations = new string[4];
        spawnLocations[0] = "E";
        spawnLocations[1] = "N";
        spawnLocations[2] = "W";
        spawnLocations[3] = "S";
        Sawmill.sprite = sawmillDroneSprite;
        
        //serielizable cant be static
        grid = _grid;
        crossbowSprite = _crossbowSprite;
        gruntSprite = _gruntSprite;
        grunt2Sprite = _grunt2Sprite;
        grunt3Sprite = _grunt3Sprite;
        
        arrowE = _arrowE;
        arrowNE = _arrowNE;
        arrowN = _arrowN;
        arrowNW = _arrowNW;
        arrowW = _arrowW;
        arrowSW = _arrowSW;
        arrowS = _arrowS;
        arrowSE = _arrowSE;
        
        arrowsTilemap = _arrowsTilemap;
        
        arrowSprite = _arrowSprite;
        bullet1Sprite = _bullet1Sprite;
        bullet2Sprite = _bullet2Sprite;
        sniperGunSprite = _sniperGunSprite;
        machineGunSprite = _machineGunSprite;
        
        
        halfTile = (grid.CellToWorld(new Vector3Int(1,1,0)) - grid.CellToWorld(Vector3Int.zero)) * 0.5f;
        
        spawnLocation = (int) Mathf.Floor(Random.value * 4);
        
        
        
        arrowsTilemap.GetComponent<TilemapRenderer>().enabled = false;
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        
    }
    
    
    // Update is called once per frame
    void Update() { //seems to be 60 fps
        //camera inputs
        if (Input.GetKeyDown(KeyCode.W)) {
            wpressed = true;
        }
        if (Input.GetKeyDown(KeyCode.A)) {
            apressed = true;
        }
        if (Input.GetKeyDown(KeyCode.S)) {
            spressed = true;
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            dpressed = true;
        }
        
        if (Input.GetKeyUp(KeyCode.W)) {
            wpressed = false;
        }
        if (Input.GetKeyUp(KeyCode.A)) {
            apressed = false;
        }
        if (Input.GetKeyUp(KeyCode.S)) {
            spressed = false;
        }
        if (Input.GetKeyUp(KeyCode.D)) {
            dpressed = false;
        }
        
        if (wpressed) {
            camPos += new Vector3(0,1,0) * camSpeed * GetComponent<Camera>().orthographicSize;
        }
        if (apressed) {
            camPos += new Vector3(-1,0,0) * camSpeed * GetComponent<Camera>().orthographicSize;
        }
        if (spressed) {
            camPos += new Vector3(0,-1,0) * camSpeed * GetComponent<Camera>().orthographicSize;
        }
        if (dpressed) {
            camPos += new Vector3(1,0,0) * camSpeed * GetComponent<Camera>().orthographicSize;
        }
        
        if (Input.GetKeyDown(KeyCode.Q)) {
            GetComponent<Camera>().orthographicSize *= 1.25f;
        }
        if (Input.GetKeyDown(KeyCode.E)) {
            GetComponent<Camera>().orthographicSize *= 0.8f;
        }
        
        //mouse cell
        Vector3 mousePos = GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mouseCell = grid.WorldToCell(mousePos);
        mouseCell.z = 0;
        
        
        
        //normal and mineral view
        if (Input.GetKeyDown(KeyCode.N) && view != 0) {
            view = 0;
            tilemap.GetComponent<TilemapRenderer>().sortingOrder = 1;
            mineralTilemap.GetComponent<TilemapRenderer>().sortingOrder = 0;
        }
        
        if (Input.GetKeyDown(KeyCode.M) && view != 1 && mineralViewAvailable) {
            view = 1;
            tilemap.GetComponent<TilemapRenderer>().sortingOrder = 0;
            mineralTilemap.GetComponent<TilemapRenderer>().sortingOrder = 1;
        }
        
        //world generation
        if (generatingWorld) {
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                waterThreshold += 0.01f;
                setMiddleText(waterThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                waterThreshold -= 0.01f;
                setMiddleText(waterThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                mountainThreshold -= 0.01f;
                setMiddleText((1.0f - mountainThreshold).ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                mountainThreshold += 0.01f;
                setMiddleText((1.0f - mountainThreshold).ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5)) {
                forestThreshold += 0.01f;
                setMiddleText(forestThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6)) {
                forestThreshold -= 0.01f;
                setMiddleText(forestThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7)) {
                ironThreshold += 0.01f;
                setMiddleText(ironThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8)) {
                ironThreshold -= 0.01f;
                setMiddleText(ironThreshold.ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9)) {
                coalThreshold -= 0.01f;
                setMiddleText((1.0f - coalThreshold).ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0)) {
                coalThreshold += 0.01f;
                setMiddleText((1.0f - coalThreshold).ToString(), 2.0f);
            }
            if (Input.GetKeyDown(KeyCode.R)) {
                metaGenMap();
            }
            
            //place keep
            if (Input.GetMouseButton(0)) {
                if (buildable(mouseCell)) {
                    keepCell = new Vector3Int(mouseCell.x, mouseCell.y, 0);
                    tilemap.SetTile(keepCell, keep);
                    buildingMap[keepCell.x, keepCell.y] = 11;
                    selectedTile = clear;
                    generatingWorld = false;
                    
                    bottomLeftText.text = "Building hotkeys:\n0. Clear\n1. Sawmill (50W)";
                    
                    Debug.Log("Keep Placed");
                    Debug.Log(keepCell);
                    
                    //setup map
                    for (int i = 0; i < worldWidth; i++) {
                        for (int j = 0; j < worldHeight; j++) {
                            float val = heightMap[i,j];
                            float val2 = 0;
                            
                            
                            if (val < waterThreshold) {val2 = 100000;}
                            else if (val > mountainThreshold) {val2 = 100000;}
                            else if (forestMap[i,j] > 0) {val2 = 5;}
                            else {val2 = 10;}
                            
                            pathingMap[i,j] = new PathingNode(val2, i, j);
                        }
                    }
                    
                    pathingMap[keepCell.x, keepCell.y].pathCost = 0;
                    pathingMap[keepCell.x, keepCell.y].nodeCost = 0;
                    pathingMap[keepCell.x, keepCell.y].nextNode = pathingMap[keepCell.x, keepCell.y];
                    
                    //use priority queue to setup initially
                    
                    var pq = new PriorityQueue<Vector3Int, float>();
                    
                    pq.Enqueue(keepCell, 0);
                    
                    while (pq.Count > 0) {
                        Vector3Int lowest = pq.Dequeue();
                        foreach (Vector3Int n in neighborTiles(lowest)) {
                            PathingNode pn = pathingMap[n.x,n.y];
                            if (pn.pathCost == 100000000.0f) {
                                updateNode(n);
                                pq.Enqueue(n, pn.pathCost);
                            }
                        }
                    }

                    

                } else {
                    setMiddleText("Build on Dirt", 3.0f);
                }
            }
        } else {
            //actual game here
            
            //pathing thread
            pathEditProg += Time.deltaTime;
            if (pathEditProg >= pathEditInterval) {
                pathEditProg = 0.0f;
                pathThread = new Thread(regenPathmap);
                pathThread.Start();
            }
            
            //pathing view
            if (Input.GetKeyDown(KeyCode.P)) {
                pathingView = !pathingView;
                arrowsTilemap.GetComponent<TilemapRenderer>().enabled = pathingView;
                for (int i = 0; i < worldWidth; i++) {
                    for (int j = 0; j < worldHeight; j++) {
                        
                        Vector3Int diff = Vector3Int.zero;
                        diff.x = pathingMap[i,j].nextNode.x - i;
                        diff.y = pathingMap[i,j].nextNode.y - j;
                
                        Tile tile = keep;
                        if (diff.x == 1 && diff.y == 0) {tile = arrowE;}
                        else if (diff.x == 1 && diff.y == 1) {tile = arrowNE;}
                        else if (diff.x == 0 && diff.y == 1) {tile = arrowN;}
                        else if (diff.x == -1 && diff.y == 1) {tile = arrowNW;}
                        else if (diff.x == -1 && diff.y == 0) {tile = arrowW;}
                        else if (diff.x == -1 && diff.y == -1) {tile = arrowSW;}
                        else if (diff.x == 0 && diff.y == -1) {tile = arrowS;}
                        else if (diff.x == 1 && diff.y == -1) {tile = arrowSE;}
                        
                        arrowsTilemap.SetTile(new Vector3Int(i,j,0), tile);
                    }
                }

            }
            
            if (pathingView && debugMode && selectedTile == clear) {
                if (Input.GetMouseButton(0)) {
                    updateNode(mouseCell);
                }
                if (Input.GetMouseButton(1)) {
                    Debug.Log((pathingMap[mouseCell.x, mouseCell.y].nodeCost, pathingMap[mouseCell.x, mouseCell.y].pathCost));
                    //updateNode2(mouseCell);
                }
            }
            
            //cheat mode
            if (Input.GetKeyDown(KeyCode.P) && Input.GetKeyDown(KeyCode.O) && Input.GetKeyDown(KeyCode.G)) {
                debugMode = true;
                globalWood += 1000;
                globalCoal += 1000;
                globalIron += 1000;
            }
            
            if (Input.GetKeyDown(KeyCode.L)) {
                enemyFrames = enemyFramesNeeded - 10;
            }
            
            //building hotkeys
            if (Input.GetKeyDown(KeyCode.Alpha0)) {
                selectedTile = clear;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1)) {
                selectedTile = sawmill;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2)) {
                selectedTile = coalRefinery;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3)) {
                selectedTile = mine;
            }
            
            else if (Input.GetKeyDown(KeyCode.Z)) {
                selectedTile = wall;
            }
            else if (Input.GetKeyDown(KeyCode.X)) {
                selectedTile = crossbowBase;
            }
            else if (Input.GetKeyDown(KeyCode.C)) {
                selectedTile = machinegunBase;
            }
            else if (Input.GetKeyDown(KeyCode.V)) {
                selectedTile = sniperBase;
            }
            
            //build click
            if (Input.GetMouseButton(0)) {
                if (buildable(mouseCell)) {
                    //build sawmill
                    if (selectedTile == sawmill && globalWood >= sawmillCost) {
                        tilemap.SetTile(mouseCell, sawmill);
                        sawmills.Add(new Sawmill(mouseCell + Vector3Int.zero));
                        numSawmills+=1;
                        buildingMap[mouseCell.x, mouseCell.y] = 1;
                        globalWood -= sawmillCost;
                        
                        if (numSawmills == 1) {
                            bottomLeftText.text = "Building hotkeys:\n0. Clear\n1. Sawmill (50W)\n2. Charcoal Refinery (80W)\n3. Mine (100WI)\nZ. Wall (10WI)\nX. Crossbow (80W)\nC. Machine Gun (100I)\nV. Sniper (100I)";
                        }
                    }
                    
                    //build coal refinery
                    else if (selectedTile == coalRefinery && globalWood >= coalRefineryCost) {
                        globalWood -= coalRefineryCost;
                        tilemap.SetTile(mouseCell, coalRefinery);
                        numCoalRefineries += 1;
                        coalRefineryProgressStride += 0.008f;
                        buildingMap[mouseCell.x, mouseCell.y] = 1;
                    }
                    
                    //build mine
                    else if (selectedTile == mine && globalWood + globalIron >= mine1Cost) {
                        bool place = true;
                        if (mineralMap[mouseCell.x, mouseCell.y] == 1) {numIronMines+=1;ironMineProgressStride+=0.004f;}
                        else if (mineralMap[mouseCell.x, mouseCell.y] == 2) {numCoalMines+=1;coalMineProgressStride+=0.004f;}
                        else {
                            place = false;
                        }
                        
                        if (place) {
                            globalWood -= mine1Cost;
                            if (globalWood < 0) {
                                globalIron += globalWood;
                                globalWood = 0;
                            }
                            
                            
                            tilemap.SetTile(mouseCell, mine);
                            numMine1s += 1;
                            buildingMap[mouseCell.x, mouseCell.y] = 1;
                        }
                    }
                    
                    //build wall
                    else if (selectedTile == wall && globalWood + globalIron >= wallCost) {
                        globalWood -= wallCost;
                        if (globalWood < 0) {
                            globalIron += globalWood;
                            globalWood = 0;
                        }
                        pathingMap[mouseCell.x, mouseCell.y].nodeCost = 50;
                        //updateNode2(mouseCell);
                        pathMapEdited = true;
                        
                        tilemap.SetTile(mouseCell, wall);
                        buildingMap[mouseCell.x, mouseCell.y] = 21;
                    }
                    
                    //build crossbow
                    else if (selectedTile == crossbowBase && globalWood >= crossbowCost) {
                        globalWood -= crossbowCost;
                        
                        tilemap.SetTile(mouseCell, crossbowBase);
                        buildingMap[mouseCell.x, mouseCell.y] = 6;
                        
                        numCrossbows+=1;
                        turrets.AddLast(new Turret(0, mouseCell + Vector3Int.zero));
                    }
                    
                    //build machinegun
                    else if (selectedTile == machinegunBase && globalIron >= machinegunCost) {
                        globalIron -= machinegunCost;
                        
                        tilemap.SetTile(mouseCell, machinegunBase);
                        buildingMap[mouseCell.x, mouseCell.y] = 6;
                        
                        numMachineguns+=1;
                        turrets.AddLast(new Turret(1, mouseCell + Vector3Int.zero));
                    }
                    
                    //build sniper
                    else if (selectedTile == sniperBase && globalIron >= machinegunCost) {
                        globalIron -= sniperCost;
                        
                        tilemap.SetTile(mouseCell, sniperBase);
                        buildingMap[mouseCell.x, mouseCell.y] = 6;
                        
                        numSnipers+=1;
                        turrets.AddLast(new Turret(2, mouseCell + Vector3Int.zero));
                    }
                    
                    
                    
                    
                    
                } else {
                    //setMiddleText("Build on Dirt", 3.0f);
                }
            }
            
            
            //sawmill logic
            foreach (Sawmill s in sawmills) {
                if (s.droneGoingHome == false) {
                    s.dronePos += s.droneVector * Sawmill.droneSpeed;
                    if ((s.dronePos - s.targetPos).sqrMagnitude < 0.3f) {
                        if (forestMap[s.targetCell.x, s.targetCell.y] > 0) {
                            forestMap[s.targetCell.x, s.targetCell.y] -= 1;
                            s.droneHasWood = true;
                        }
                        
                        s.droneGoingHome = true;
                        s.droneObject.transform.eulerAngles = new Vector3(0,0,Mathf.Atan2(-s.droneVector.y, -s.droneVector.x) / Mathf.PI * 180.0f - 90.0f);
                        
                        if (forestMap[s.targetCell.x, s.targetCell.y] == 0) {
                            tilemap.SetTile(s.targetCell, dirt);
                            pathingMap[s.targetCell.x, s.targetCell.y].nodeCost = 10;
                            //updateNode2(new Vector3Int(s.targetCell.x, s.targetCell.y, 0));
                            pathMapEdited = true;
                        }
                    }
                } else {
                    
                    s.dronePos -= s.droneVector * Sawmill.droneSpeed;
                    if ((s.dronePos - s.worldPos).sqrMagnitude < 0.3f) {
                        s.droneGoingHome = false;
                        s.droneObject.transform.eulerAngles = new Vector3(0,0,Mathf.Atan2(s.droneVector.y, s.droneVector.x) / Mathf.PI * 180.0f - 90.0f);
                        
                        if (s.droneHasWood) {
                            s.woodStockpile += 1;
                            globalWood += 1;
                            s.droneHasWood = false;
                        }
                        
                        if (forestMap[s.targetCell.x, s.targetCell.y] == 0) {
                            bool a = true;
                            int min = 1000000000;
                            Vector3Int ntarg = Vector3Int.zero; //new target
                            
                            s.dronePos = s.worldPos + Vector3.zero;
                            
                            int sr = 2; //search radius (square)
                            
                            for (int i = Mathf.Max(0,s.targetCell.x - sr); i < Mathf.Min(worldWidth-1, s.targetCell.x + sr)+1; i++) {
                                for (int j = Mathf.Max(0,s.targetCell.y - sr); j < Mathf.Min(worldHeight-1, s.targetCell.y + sr)+1; j++) {
                                    if (sqrInt(s.pos.x-i) + sqrInt(s.pos.y - j) < min && forestMap[i,j] > 0) {
                                        min = sqrInt(s.pos.x-i) + sqrInt(s.pos.y - j);
                                        a = false;
                                        ntarg = new Vector3Int(i,j,0);
                                    }
                                    
                                    
                                }
                            }
                            
                            if (a) {s.findTree();} else {
                                s.targetCell = ntarg;
                                s.targetPos = grid.CellToWorld(s.targetCell);
                                s.droneVector = (s.targetPos - s.worldPos).normalized;
                                
                            }
                        }
                    }
                }
                s.droneObject.transform.localPosition = s.dronePos;
                
            }
            
            //mines and refineries
            if (globalCoal > 8) {ironMineProgress += ironMineProgressStride;}
            if (globalCoal > 2) {coalMineProgress += coalMineProgressStride;}
            if (globalWood > 3) {coalRefineryProgress += coalRefineryProgressStride; };
            
            while (ironMineProgress > 1.0f) {ironMineProgress-=1.0f; globalIron+=1; globalCoal -= 2;}
            while (coalMineProgress > 1.0f) {coalMineProgress-=1.0f; globalCoal+=2;}
            while (coalRefineryProgress > 1.0f) {coalRefineryProgress-=1.0f; globalCoal+=1; globalWood -= 3;}
            
            //enemy logic
            enemyFrames+=2;
            if (enemyFrames == enemyFramesNeeded) {
                enemyFrames = 0;
                //spawn wave
                //float sizeFactor = Random.value * 0.8f + 0.1f; //0 is all smallest, 1 is all largest.
                
                float thisWaveStrength = waveStrength;
                
                float spawnVal5 = 0;
                float spawnVal6 = 0;
                
                if (spawnLocation == 0) {
                    spawnVal5 = (halfTile.x*2.0f)*((float)(worldWidth-2));
                    spawnVal6 = (halfTile.y*2.0f)*((float)(worldHeight-2) * Random.value + 1.0f);
                } else if (spawnLocation == 1) {
                    spawnVal5 = (halfTile.x*2.0f)*((float)(worldWidth-2) * Random.value + 1.0f);
                    spawnVal6 = (halfTile.y*2.0f)*((float)(worldHeight-2));
                } else if (spawnLocation == 2) {
                    spawnVal5 = (halfTile.x*2.0f)*(1.0f);
                    spawnVal6 = (halfTile.y*2.0f)*((float)(worldHeight-2) * Random.value + 1.0f);
                } else {
                    spawnVal5 = (halfTile.x*2.0f)*((float)(worldWidth-2) * Random.value + 1.0f);
                    spawnVal6 = (halfTile.y*2.0f)*(1.0f);
                }
                
                //Debug.Log(spawnVal5);
                //Debug.Log(spawnVal6);
                
                float sizeVal1 = Random.value * 0.8f + 0.2f;
                float sizeVal2 = Random.value * 0.8f + 0.2f;
                if (sizeVal2 > sizeVal1) {
                    float temp = sizeVal1;
                    sizeVal1 = sizeVal2;
                    sizeVal2 = temp;
                }
                
                while (thisWaveStrength > 1.0f) {
                    float spawnVal1 = 0;
                    float spawnVal2 = 0;
                    float spawnVal3 = 0;
                    float spawnVal4 = 0;
                    
                    
                    
                    
                    if (spawnLocation == 0) {
                        spawnVal1 = 4;
                        spawnVal2 = 8;
                        spawnVal3 = -1;
                        spawnVal4 = -0.5f;
                    } else if (spawnLocation == 1) {
                        spawnVal1 = 8;
                        spawnVal2 = 4;
                        spawnVal3 = -0.5f;
                        spawnVal4 = -1;
                    } else if (spawnLocation == 2) {
                        spawnVal1 = 4;
                        spawnVal2 = 8;
                        spawnVal3 = 1;
                        spawnVal4 = -0.5f;
                    } else {
                        spawnVal1 = 8;
                        spawnVal2 = 4;
                        spawnVal3 = -0.5f;
                        spawnVal4 = 1;
                    }
                    
                    Vector3 spawnPos = new Vector3(
                        spawnVal5 + (Random.value + spawnVal3)*spawnVal1,
                        spawnVal6 + (Random.value + spawnVal4)*spawnVal2,
                        0
                    );
                    
                    
                    float sizeVal3 = Random.value;
                    
                    if (sizeVal3 > sizeVal1 && thisWaveStrength > 16.0f) {
                        thisWaveStrength -= 16.0f;
                        enemies.AddLast(new Enemy(2, spawnPos));
                    } else if (sizeVal3 > sizeVal2 && thisWaveStrength > 4.0f) {
                        thisWaveStrength -= 4.0f;
                        enemies.AddLast(new Enemy(1, spawnPos));
                    } else {
                        thisWaveStrength -= 1.0f;
                        enemies.AddLast(new Enemy(0, spawnPos));
                    } 
                    
                    //spawn grunt
                    
                }
                
                
                waveNumber += 1;
                spawnLocation = (int) Mathf.Floor(Random.value * 4);
                waveStrength += waveStrengthIncrement;
                waveStrength *= waveStrengthMultiplier;
            }
            
            //enemy logic
            foreach (Enemy enemy in enemies) {
                //enemy pathing
                Vector3Int tile = grid.WorldToCell(enemy.pos);
                if (tile.x < 0 || tile.y < 0 || tile.x > worldWidth - 1 || tile.y > worldHeight - 1) {
                    enemy.hp -= 1;
                    continue;
                }
                
                if (buildingMap[tile.x, tile.y] > 1) {
                    enemy.hp -= 1;
                    byte n = buildingMap[tile.x, tile.y];
                    if (enemy.attack + 1 >= buildingMap[tile.x, tile.y]) {
                        buildingMap[tile.x, tile.y] = 0;
                        tilemap.SetTile(tile, dirt);
                        
                        if (tile.x==keepCell.x&&tile.y==keepCell.y) {
                            middleText.text = "You Are Dead - Final Wave: " + waveNumber;
                        }
                    } else {
                        buildingMap[tile.x, tile.y] -= enemy.attack;
                    }
                }
                
                foreach (Enemy enemy2 in enemies) {
                    if(enemy == enemy2) {
                        continue;
                    }
                    
                    Vector3 diff = enemy.pos - enemy2.pos;
                    //multiplied by 4 would be more correct. Using approximations here tho.
                    if (diff.sqrMagnitude < enemy.sizeSquared) {
                        Vector3 a = diff / (diff.sqrMagnitude + 1.0f) * 0.05f;
                        enemy.pos += a;
                        enemy2.pos -= a;
                    }
                }
                
                Vector3 targetv = grid.CellToWorld(new Vector3Int(pathingMap[tile.x,tile.y].nextNode.x,pathingMap[tile.x,tile.y].nextNode.y)) + halfTile;
                Vector3 delta = (enemy.pos - targetv).normalized * enemy.speed;
                enemy.pos -= delta;
                
                
                
            }
            
            //remove dead turrets
            {
                var node = turrets.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (buildingMap[node.Value.pos.x,node.Value.pos.y] == 0) {
                        Destroy(node.Value.turretObj);
                        turrets.Remove(node);
                    }
                    node = next;
                }
            }
            
            //turrets logic
            foreach (Turret turret in turrets) {
                if (turret.target == null) {
                    
                    float low = turret.rangeSquared;
                    foreach (Enemy enemy in enemies) {
                        float rng = (enemy.pos - turret.worldPos).sqrMagnitude;
                        if (rng < low) {
                            turret.target = enemy;
                            low = rng;
                            break;
                        }
                    }
                    
                    
                    turret.fireProgress = 0.0f;
                } else {
                    if (turret.target.hp < 1) {turret.target = null;continue;}
                    
                    turret.fireProgress += turret.rateOfFire;
                    if (turret.fireProgress >= 1.0f) {
                        turret.fireProgress = 0.0f;
                        Vector3 diff = (turret.target.pos - turret.worldPos).normalized;
                        float a = (Mathf.Atan2(diff.y, diff.x) - Mathf.PI*0.5f) / Mathf.PI * 180;
                        turret.turretObj.transform.eulerAngles = new Vector3(0,0,a);
                        
                        projectiles.AddLast(new Projectile(turret.worldPos, diff * turret.projectileVelocity, new Vector3(0,0,a), turret.type));
                    }
                }
                
                
            }
            
            //projectiles logic
            {
                var node = projectiles.First;
                while (node != null)
                {
                    Projectile proj = node.Value;
                    
                    proj.pos += proj.vel;
                    proj.obj.transform.localPosition = proj.pos;
                    
                    bool removeProj = false;
                    
                    if (proj.pos.x < 0 || proj.pos.y < 0 || proj.pos.x > halfTile.x * 2 * (float) worldWidth || proj.pos.y > halfTile.y * 2 * (float) worldHeight) {
                        removeProj = true;
                    } else {
                        foreach(Enemy enemy in enemies) {
                            if ((proj.pos-enemy.pos).sqrMagnitude < enemy.sizeSquared) {
                                removeProj = true;
                                enemy.hp -= proj.damage;
                            }
                        }
                    }
                    
                
                    
                    var next = node.Next;
                    if (removeProj) {
                        Destroy(node.Value.obj);
                        projectiles.Remove(node);
                    }
                    node = next;
                }
            }
            //remove dead enemies
            {
                var node = enemies.First;
                while (node != null)
                {
                    var next = node.Next;
                    if (node.Value.hp < 1) {
                        Destroy(node.Value.obj);
                        enemies.Remove(node);
                    }
                    node = next;
                }
            }
        }
        
        
        
        
        //rendering stuff
        transform.localPosition = camPos;
        
        //middle text
        middleTextTimeElapsed += Time.deltaTime;
        if (middleTextOn && middleTextTimeElapsed > middleTextExpiration) {
            middleTextOn = false;
            middleText.text = "";
        }
        
        //update tile on mouse
        if (mouseCell.x != lastMouseCell.x || mouseCell.y != lastMouseCell.y) {
            foregroundTilemap.SetTile(lastMouseCell, null);
            lastMouseCell.x = mouseCell.x;
            lastMouseCell.y = mouseCell.y;
            foregroundTilemap.SetTile(lastMouseCell, selectedTile);
            
        }
        
        //update resource text
        topLeftText.text = "Wood: " + globalWood + "\nIron: " + globalIron + "\nCoal: " + globalCoal;
        
        //update timer
        {
            int n = enemyFramesNeeded - enemyFrames;
            topRightText.text = spawnLocations[spawnLocation] + " : " + (n / 3600) + ":" + ((n/60)%60) + ":" + (n%60);
        }
        
        //enemy positions
        foreach (Enemy enemy in enemies) {
            enemy.obj.transform.localPosition = enemy.pos;
        }
    }
    
    float middleTextExpiration = 0;
    float middleTextTimeElapsed = 0;
    bool middleTextOn = false;
    
    public void setMiddleText(string text, float expiration) {
        middleText.text = text;
        middleTextExpiration = expiration;
        middleTextTimeElapsed = 0;
        middleTextOn = true;
    }
    
    public static bool buildable(Vector3Int pos) {
        //map check
        
        if (pos.x < 1 || pos.y < 1 | pos.x > worldWidth - 2 || pos.y > worldHeight - 2) {
            return false;
        }
        bool a = heightMap[pos.x,pos.y] > waterThreshold && heightMap[pos.x,pos.y] < mountainThreshold && buildingMap[pos.x,pos.y] == 0;
        
        //forest check
        for (int i = -1; i < 2; i++) {
            for (int j = -1; j < 2; j++) {
                a = a && forestMap[pos.x + i, pos.y + j] == 0;
            }
        }
        return a;
    }
    
    public static int sqrInt(int a) {return a*a;}
    
    public class Sawmill {
        public GameObject droneObject;
        public Vector3 dronePos;
        public Vector3Int pos;
        public Vector3 targetPos;
        public Vector3Int targetCell;
        public Vector3 droneVector;
        public bool droneHasWood;
        public bool droneGoingHome;
        public Vector3 worldPos;
        
        public int woodStockpile;
        
        
        
        public static float droneSpeed = 0.3f;
        public static int numSpawns = 0;
        public static Sprite sprite;
        
        public Sawmill(Vector3Int p) {
            pos = p;
            worldPos = grid.CellToWorld(p);
            dronePos = worldPos + Vector3.zero;
            
            
            droneHasWood = false;
            woodStockpile = 0;
            
            numSpawns+=1;
            droneObject = new GameObject("WoodDrone" + numSpawns.ToString());
            droneObject.AddComponent<SpriteRenderer>();
            droneObject.GetComponent<SpriteRenderer>().sprite = sprite;
            droneObject.GetComponent<SpriteRenderer>().sortingOrder = 5;
            droneObject.transform.localPosition = dronePos;
            
            findTree();
        }
        
        
        public bool findTree() {
            //this is so inefficient
            
            int min = 1000000000; //squared distance
            Vector3Int bestPos = Vector3Int.zero;
            for (int i = 0; i < worldWidth; i++) {
                for (int j = 0; j < worldHeight; j++) {
                    if (forestMap[i,j] > 0) {
                        int a = sqrInt(pos.x-i) + sqrInt(pos.y-j);
                        if (a < min) {
                            min = a;
                            bestPos = new Vector3Int(i,j,0);
                        }
                    }
                }
            }
            
            //out of forest
            if (min == 1000000000) {
                return false;
            }
            
            targetCell = bestPos;
            targetPos = grid.CellToWorld(bestPos);
            droneVector = (targetPos - worldPos).normalized;
            droneObject.transform.eulerAngles = new Vector3(0,0,Mathf.Atan2(droneVector.y, droneVector.x) / Mathf.PI * 180.0f - 90.0f);
            return true;
        }
    }
    
    public class Turret {
        public Vector3Int pos;
        public Vector3 worldPos;
        
        public float rateOfFire;
        public float fireProgress;
        public float rangeSquared;
        public int woodCost;
        public int ironCost;
        public int type;
        
        public float projectileVelocity;
        
        public GameObject turretObj;
        public Sprite turretSprite;
        
        public Enemy target;
        
        public static int numSpawns = 0;
        
        public Turret(int typee, Vector3Int p) {
            switch (typee) {
                case 0: //crossbow
                turretSprite = crossbowSprite;
                rateOfFire = 0.018f;
                rangeSquared = 20 * 20;
                woodCost = 1;
                ironCost = 0;
                projectileVelocity = 0.55f;
                break;
                
                case 1: //machine gun
                turretSprite = machineGunSprite;
                rateOfFire = 0.1f;
                rangeSquared = 20 * 20;
                woodCost = 0;
                ironCost = 1;
                projectileVelocity = 1.0f;
                break;
                
                case 2: //sniper gun
                turretSprite = sniperGunSprite;
                rateOfFire = 0.01f;
                rangeSquared = 80 * 80;
                woodCost = 0;
                ironCost = 2;
                projectileVelocity = 1.6f; //cant go so fast it goes through enemies
                break;
                
                
            }
            type = typee;
            fireProgress = 0;
            pos = p;
            target = null;
            worldPos = grid.CellToWorld(p) + halfTile;
            numSpawns+=1;
            turretObj = new GameObject("Turret" + numSpawns.ToString());
            turretObj.AddComponent<SpriteRenderer>();
            turretObj.GetComponent<SpriteRenderer>().sprite = turretSprite;
            turretObj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            turretObj.transform.localPosition = worldPos;
            
        }
    }
    
    public class Projectile {
        public Vector3 pos;
        public Vector3 vel;
        public GameObject obj;
        public int damage;
        
        public static int numSpawns = 0;
        public Projectile(Vector3 p, Vector3 v, Vector3 a, int type) {
            pos = p;
            vel = v;
            
            numSpawns+=1;
            obj = new GameObject("Projectile" + numSpawns.ToString());
            obj.AddComponent<SpriteRenderer>();
            obj.GetComponent<SpriteRenderer>().sortingOrder = 3;
            obj.transform.localPosition = pos;
            obj.transform.eulerAngles = a;
            
            //crossbow
            obj.GetComponent<SpriteRenderer>().sprite = arrowSprite;
            damage = 1;
            
            switch (type) {
                case 0: //arrow
                break;
                case 1: //mg
                obj.GetComponent<SpriteRenderer>().sprite = bullet1Sprite;
                damage = 1;
                break;
                case 2: 
                obj.GetComponent<SpriteRenderer>().sprite = bullet2Sprite;
                damage = 6;
                break;
            }
        }
    }
    
    public class Enemy {
        public Vector3 pos;
        public Sprite sprite;
        public GameObject obj;
        
        public int hp;
        public float speed;
        public bool kill;
        public byte attack;
        public float sizeSquared;
        
        public static int numSpawns = 0;
        
        public Enemy(int type, Vector3 p) {
            pos = p;
            sprite = gruntSprite;
            hp = 3;
            speed = 0.06f;
            attack = 1;
            sizeSquared = 0.6f * 0.6f;
            switch (type) {
                //grunt
                case 0:
                break;
                case 1://red
                attack = 2;
                hp = 10;
                sprite = grunt2Sprite;
                break;
                case 2://blue
                hp = 40;
                attack = 5;
                sprite = grunt3Sprite;
                break;
                
            }
            
            numSpawns+=1;
            obj = new GameObject("Enemy" + numSpawns.ToString());
            obj.AddComponent<SpriteRenderer>();
            obj.GetComponent<SpriteRenderer>().sprite = sprite;
            obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            obj.transform.localPosition = pos;
        }
    }
}