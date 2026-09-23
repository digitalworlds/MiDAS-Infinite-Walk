using UnityEngine;

// The three shapes a Block can take. Every block is a Size x Size square tile;
// the type only decides where its exit sits and which way it faces.
public enum BlockType
{
    Straight,
    TurnLeft,
    TurnRight
}

public class Block
{
    // Every block is a fixed-size square tile. All blocks share this same size.
    public const float Size = 1.2f;

    private readonly int id;
    private readonly Vector3 startPosition;
    private readonly Vector2 direction2D; // entry direction, (x, z) in the flat plane
    private readonly BlockType type;

    private LineRenderer[] wallLines;
    private MeshFilter[] wallMeshFilters;

    private static readonly Color DefaultSideLineColor = Color.white;
    private static readonly Color HighlightSideLineColor = Color.yellow;
    private const float WallHeight = 2f;

    public GameObject GameObject { get; }
    public Vector2 Direction => direction2D.normalized;
    public Vector3 StartPosition => startPosition;
    public float Length => Size;
    public int Id => id;
    public Block Next { get; private set; }
    public Block Previous { get; private set; }

    // How this block bends from its entry to its exit. Fixed for the lifetime of the block - set only
    // through the constructor, since it determines the walls built right there.
    public BlockType Type => type;

    // Where the next block in the chain begins.
    public readonly Vector3 ExitPosition;

    // The direction the next block in the chain enters with.
    public readonly Vector2 ExitDirection;

    private readonly bool showDebugSpace;

    public Block(Vector3 start, Vector2 direction, BlockType type, bool showDebugSpace, Transform parent = null, string name = "Block")
        : this(0, start, direction, type, showDebugSpace, parent, name)
    {
    }

    // Chains off "previous": starts where previous's exit is (previous.Type, already fixed, determines where).
    public Block(Block previous, BlockType type, Transform parent = null, string name = "Block")
        : this(previous.id + 1, previous.ExitPosition, previous.ExitDirection, type, previous.showDebugSpace, parent, name)
    {
        previous.Next = this;
        Previous = previous;
    }

    // Prepends a new block before "next" with the given type, solving for the entry position/direction so
    // that its exit lands exactly on next's start. Id is next.Id - 1.
    public static Block Prepend(Block next, BlockType type, Transform parent = null, string name = "Block")
    {
        ComputeEntryFromExit(next.StartPosition, next.Direction, type, out Vector3 start, out Vector2 direction);
        Block block = new Block(next.id - 1, start, direction, type, next.showDebugSpace, parent, name);
        block.Next = next;
        next.Previous = block;
        return block;
    }

    private Block(int id, Vector3 start, Vector2 direction, BlockType type, bool showDebugSpace, Transform parent, string name)
    {
        this.showDebugSpace = showDebugSpace;
    
        this.id = id;
        startPosition = start;
        direction2D = direction;
        this.type = type;

        ExitPosition = ComputeExitPosition(start, direction, type); // just to verify it doesn't throw
        ExitDirection = ComputeExitDirection(direction, type); // just to verify it doesn't throw

        GameObject = new GameObject(name);
        GameObject.SetActive(false);
        GameObject.transform.SetParent(parent);
        GameObject.transform.position = startPosition;
        GameObject.transform.rotation = Quaternion.LookRotation(new Vector3(Direction.x, 0f, Direction.y), Vector3.up);

        if(this.showDebugSpace==false)return;

        LineRenderer lineRenderer = GameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 3;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.forward * Size*0.5f);
        switch (type)
        {
            case BlockType.TurnLeft:
            {
                lineRenderer.SetPosition(2, Vector3.forward * Size*0.5f+Vector3.left * Size*0.5f);
                break;
            }
            case BlockType.TurnRight:
            {
                lineRenderer.SetPosition(2, Vector3.forward * Size*0.5f+Vector3.right * Size*0.5f);
                break;
            }
            default:
                lineRenderer.SetPosition(2, Vector3.forward * Size);
                break;
        }
        
        //Exit position can be set here, because it is determined by the block type and direction, which are fixed at construction time.
  
        BuildWalls();
    }

    // This tile's 4 world-space corners: [start+right, start-right, far+right, far-right], where "far" is
    // Size ahead along the entry direction. Same square regardless of Type - only the exit point differs.
    public Vector3[] GetCorners()
    {
        Vector3 forward = new Vector3(Direction.x, 0f, Direction.y);
        Vector3 right = new Vector3(Direction.y, 0f, -Direction.x) * (Size / 2f);
        Vector3 far = startPosition + forward * Size;
        return new[] { startPosition + right, startPosition - right, far + right, far - right };
    }

    // Builds the walls that belong to this tile's two edges not used as an entry/exit opening.
    // Straight: the two long side edges (a pair of parallel rails). Turn: the two far edges, meeting at
    // the outer corner, forming a single bent (L-shaped) wall. Entirely intrinsic to this block - it
    // never depends on Next or Previous, and is built exactly once since Type is fixed at construction.
    private void BuildWalls()
    {
        Vector3[] c = GetCorners(); // [right0, left0, rightFar, leftFar]
        Color wallColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

        Vector3[] a;
        Vector3[] b;
        switch (type)
        {
            case BlockType.TurnLeft:
                a = new[] { c[0], c[2] }; // right edge
                b = new[] { c[2], c[3] }; // far edge, sharing the outer corner
                break;
            case BlockType.TurnRight:
                a = new[] { c[1], c[3] }; // left edge
                b = new[] { c[3], c[2] }; // far edge, sharing the outer corner
                break;
            default:
                a = new[] { c[1], c[3] }; // left rail
                b = new[] { c[0], c[2] }; // right rail
                break;
        }

        wallLines = new LineRenderer[2];
        wallMeshFilters = new MeshFilter[2];
        wallLines[0] = CreateSideLine("WallLineA", a[0], a[1]);
        wallMeshFilters[0] = CreateWall("WallA", a[0], a[1], wallColor);
        wallLines[1] = CreateSideLine("WallLineB", b[0], b[1]);
        wallMeshFilters[1] = CreateWall("WallB", b[0], b[1], wallColor);
    }

    private LineRenderer CreateSideLine(string name, Vector3 from, Vector3 to)
    {
        GameObject sideLine = new GameObject(name);
        sideLine.transform.SetParent(GameObject.transform);

        LineRenderer sideLineRenderer = sideLine.AddComponent<LineRenderer>();
        sideLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        sideLineRenderer.useWorldSpace = false;
        sideLineRenderer.positionCount = 2;
        sideLineRenderer.startWidth = 0.01f;
        sideLineRenderer.endWidth = 0.01f;
        sideLineRenderer.SetPosition(0, from);
        sideLineRenderer.SetPosition(1, to);
        sideLineRenderer.startColor = DefaultSideLineColor;
        sideLineRenderer.endColor = DefaultSideLineColor;
        return sideLineRenderer;
    }

    // Builds a rectangular wall spanning the two given world-space points, from the ground up to WallHeight.
    private MeshFilter CreateWall(string name, Vector3 from, Vector3 to, Color wallColor)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(GameObject.transform);
        wall.transform.position = Vector3.zero;
        wall.transform.rotation = Quaternion.identity;

        MeshFilter meshFilter = wall.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        UpdateWallMesh(meshFilter.mesh, from, to);

        MeshRenderer meshRenderer = wall.AddComponent<MeshRenderer>();
        // "Standard" is a Built-in Render Pipeline shader; this project uses URP, where it doesn't
        // exist, so Shader.Find would return null and Unity would render the wall magenta.
        Shader wallShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        meshRenderer.material = new Material(wallShader);
        meshRenderer.material.color = wallColor;
        return meshFilter;
    }

    // Builds a wall mesh's vertices/normals for a pair of endpoints.
    private void UpdateWallMesh(Mesh mesh, Vector3 from, Vector3 to)
    {
        Vector3 up = Vector3.up * WallHeight;
        Vector3 faceNormal = Vector3.Cross(to - from, up).normalized;

        // Front/back faces use separate duplicated vertices so each keeps its own (opposite) normal instead of averaging to zero.
        mesh.vertices = new[]
        {
            from, to, to + up, from + up,
            from, to, to + up, from + up
        };
        mesh.normals = new[]
        {
            faceNormal, faceNormal, faceNormal, faceNormal,
            -faceNormal, -faceNormal, -faceNormal, -faceNormal
        };
        mesh.triangles = new[]
        {
            0, 1, 2, 0, 2, 3, // front face
            4, 6, 5, 4, 7, 6  // back face, reversed winding
        };
        mesh.RecalculateBounds();
    }

    // Colors this block's walls to indicate whether the player is currently on it.
    public void SetHighlighted(bool highlighted)
    {
        if (wallLines == null) return;
        Color color = highlighted ? HighlightSideLineColor : DefaultSideLineColor;
        foreach (LineRenderer line in wallLines)
        {
            if (line == null) continue;
            line.startColor = color;
            line.endColor = color;
        }
    }

    // Detaches Next so a new Next can be attached. This block's own walls are untouched - they never
    // depended on Next.
    public void DetachNext()
    {
        if (Next == null) return;
        Next.Previous = null;
        Next = null;
    }

    // Detaches Previous. This block's own walls are untouched - they never depended on Previous.
    public void DetachPrevious()
    {
        Previous = null;
    }

    private static Vector2 RotateLeft(Vector2 d) => new Vector2(-d.y, d.x);
    private static Vector2 RotateRight(Vector2 d) => new Vector2(d.y, -d.x);

    // Where a block starting at "start" facing "direction" with the given type exits.
    public static Vector3 ComputeExitPosition(Vector3 start, Vector2 direction, BlockType type)
    {
        Vector3 forward = new Vector3(direction.x, 0f, direction.y);
        switch (type)
        {
            case BlockType.TurnLeft:
            {
                Vector3 left = new Vector3(-direction.y, 0f, direction.x);
                return start + forward * (Size / 2f) + left * (Size / 2f);
            }
            case BlockType.TurnRight:
            {
                Vector3 right = new Vector3(direction.y, 0f, -direction.x);
                return start + forward * (Size / 2f) + right * (Size / 2f);
            }
            default:
                return start + forward * Size;
        }
    }

    // The direction a block starting with "direction" and the given type exits facing.
    public static Vector2 ComputeExitDirection(Vector2 direction, BlockType type)
    {
        switch (type)
        {
            case BlockType.TurnLeft: return RotateLeft(direction);
            case BlockType.TurnRight: return RotateRight(direction);
            default: return direction;
        }
    }

    // Inverse of ComputeExit/ComputeExitDirection: given where a block of the given type should exit
    // (position and direction), finds the entry position/direction it must have started from.
    public static void ComputeEntryFromExit(Vector3 exitPosition, Vector2 exitDirection, BlockType type, out Vector3 entryStart, out Vector2 entryDirection)
    {
        switch (type)
        {
            case BlockType.TurnLeft:
            {
                entryDirection = RotateRight(exitDirection);
                Vector3 forward = new Vector3(entryDirection.x, 0f, entryDirection.y);
                Vector3 left = new Vector3(-entryDirection.y, 0f, entryDirection.x);
                entryStart = exitPosition - forward * (Size / 2f) - left * (Size / 2f);
                break;
            }
            case BlockType.TurnRight:
            {
                entryDirection = RotateLeft(exitDirection);
                Vector3 forward = new Vector3(entryDirection.x, 0f, entryDirection.y);
                Vector3 right = new Vector3(entryDirection.y, 0f, -entryDirection.x);
                entryStart = exitPosition - forward * (Size / 2f) - right * (Size / 2f);
                break;
            }
            default:
                entryDirection = exitDirection;
                entryStart = exitPosition - new Vector3(exitDirection.x, 0f, exitDirection.y) * Size;
                break;
        }
    }
}
