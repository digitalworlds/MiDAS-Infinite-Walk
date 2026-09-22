using UnityEngine;

// Generates a chain of Blocks while keeping every corner within a rectangular X/Z area, using ordinary
// recursive backtracking: at each step, shuffle the 3 possible types (straight, left turn, right turn) and
// try them in that order. Trying a type means constructing the block and recursing to place the rest of the
// chain after it; if that recursion can't complete, the block is undone (destroyed and unlinked) and the
// next type in the shuffled list is tried instead. A failure only ever propagates back to the caller, which
// then tries its own next candidate - so backtracking falls straight out of the recursion unwinding, with
// no explicit stack/undo-loop needed, and it can never get stuck: it either finds a valid chain of the
// requested length or exhaustively proves none exists from here.
//
// Two consecutive blocks may never both be the same turn (straight can follow, or be followed by,
// anything) - AllowedTypesAfter enforces this by narrowing the candidate list before it's even shuffled.
public class ConstrainedBlockGenerator
{
    private static readonly BlockType[] AllTypes = { BlockType.Straight, BlockType.TurnLeft, BlockType.TurnRight };
    private static readonly BlockType[] NotTurnLeft = { BlockType.Straight, BlockType.TurnRight };
    private static readonly BlockType[] NotTurnRight = { BlockType.Straight, BlockType.TurnLeft };

    private readonly float minX;
    private readonly float maxX;
    private readonly float minZ;
    private readonly float maxZ;
    private readonly int overlapCheckDepth;
    private readonly Transform parent;

    public ConstrainedBlockGenerator(float minX, float maxX, float minZ, float maxZ, int overlapCheckDepth, Transform parent = null)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minZ = minZ;
        this.maxZ = maxZ;
        this.overlapCheckDepth = overlapCheckDepth;
        this.parent = parent;

        CreateBoundaryLines();
    }

    // Draws a closed rectangle around the [minX, maxX] x [minZ, maxZ] area at ground level.
    private void CreateBoundaryLines()
    {
        GameObject boundary = new GameObject("GenerationAreaBoundary");
        boundary.transform.SetParent(parent);
        boundary.transform.position = Vector3.zero;
        boundary.transform.rotation = Quaternion.identity;

        LineRenderer line = boundary.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 4;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.SetPosition(0, new Vector3(minX, 0f, minZ));
        line.SetPosition(1, new Vector3(maxX, 0f, minZ));
        line.SetPosition(2, new Vector3(maxX, 0f, maxZ));
        line.SetPosition(3, new Vector3(minX, 0f, maxZ));
    }

    // Appends "count" blocks after anchor (following Next). Returns the new tail, or anchor itself
    // (unchanged) if no chain of that length fits in the area at all.
    public Block GenerateForward(Block anchor, int count)
    {
        if (TryGenerateForward(anchor, count, out Block tail)) return tail;
        Debug.LogWarning("ConstrainedBlockGenerator: no forward chain of the requested length fits in the generation area.");
        return anchor;
    }

    // Prepends "count" blocks before anchor (following Previous). Returns the new head, or anchor itself
    // (unchanged) if no chain of that length fits in the area at all.
    public Block GenerateBackward(Block anchor, int count)
    {
        if (TryGenerateBackward(anchor, count, out Block head)) return head;
        Debug.LogWarning("ConstrainedBlockGenerator: no backward chain of the requested length fits in the generation area.");
        return anchor;
    }

    // Recursively extends the chain forward from "origin" by "remaining" more blocks. Origin's exit is
    // already fixed (its own type was decided by whichever call placed it), so a new block's own footprint
    // here is the same regardless of which type we give it - only origin's exit position matters, and it's
    // checked once up front. What DOES depend on the candidate is everything past this new block, so we try
    // each type in a random order: construct it, then recurse. If the recursion can't complete the
    // remaining chain, undo this block and fall through to the next candidate; if none of the 3 work,
    // report failure so the CALLER backtracks by retrying with a different type for its own block.
    private bool TryGenerateForward(Block origin, int remaining, out Block resultTail)
    {
        if (remaining == 0)
        {
            resultTail = origin;
            return true;
        }

        Vector3[] corners = GetCorners(origin.ExitPosition, origin.ExitDirection);
        if (!WithinBounds(corners) || OverlapsNearbyBlocks(origin.Previous, false, corners))
        {
            resultTail = null;
            return false; // no type can fix this - origin's own exit position is invalid
        }

        foreach (BlockType candidate in Shuffled(AllowedTypesAfter(origin.Type)))
        {
            Block newBlock = new Block(origin, candidate, parent);
            if (TryGenerateForward(newBlock, remaining - 1, out resultTail))
            {
                return true;
            }

            origin.DetachNext();
            Object.Destroy(newBlock.GameObject);
        }

        resultTail = null;
        return false;
    }

    // Recursive backward counterpart. Unlike forward, the type we pick directly determines the new block's
    // own entry (solved via ComputeEntryFromExit), so each candidate's own footprint is checked before being
    // tried; if it fits, we recurse to place the rest of the chain before it, undoing and moving to the next
    // candidate on failure exactly as above.
    private bool TryGenerateBackward(Block origin, int remaining, out Block resultHead)
    {
        if (remaining == 0)
        {
            resultHead = origin;
            return true;
        }

        foreach (BlockType candidate in Shuffled(AllowedTypesAfter(origin.Type)))
        {
            Block.ComputeEntryFromExit(origin.StartPosition, origin.Direction, candidate, out Vector3 start, out Vector2 direction);
            Vector3[] corners = GetCorners(start, direction);
            if (!WithinBounds(corners) || OverlapsNearbyBlocks(origin.Next, true, corners))
            {
                continue; // this type doesn't fit here - try the next one
            }

            Block newBlock = Block.Prepend(origin, candidate, parent);
            if (TryGenerateBackward(newBlock, remaining - 1, out resultHead))
            {
                return true;
            }

            origin.DetachPrevious();
            Object.Destroy(newBlock.GameObject);
        }

        resultHead = null;
        return false;
    }

    // The types a block adjacent to one of type "neighborType" is allowed to be: two consecutive blocks may
    // never both be the same turn (straight can follow, or be followed by, anything).
    private static BlockType[] AllowedTypesAfter(BlockType neighborType)
    {
        if (neighborType == BlockType.TurnLeft) return NotTurnLeft;
        if (neighborType == BlockType.TurnRight) return NotTurnRight;
        return AllTypes;
    }

    private static BlockType[] Shuffled(BlockType[] source)
    {
        BlockType[] result = (BlockType[])source.Clone();
        for (int i = result.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    // Checks the candidate against up to overlapCheckDepth blocks starting at "start" and walking
    // Next (backward) or Previous (forward).
    private bool OverlapsNearbyBlocks(Block start, bool backward, Vector3[] candidateCorners)
    {
        Block block = start;
        for (int i = 0; i < overlapCheckDepth && block != null; i++)
        {
            if (RectanglesOverlap(candidateCorners, block.GetCorners())) return true;
            block = backward ? block.Next : block.Previous;
        }
        return false;
    }

    private Vector3[] GetCorners(Vector3 start, Vector2 direction)
    {
        Vector3 forward = new Vector3(direction.x, 0f, direction.y);
        Vector3 end = start + forward * Block.Size;
        Vector3 perpendicular = new Vector3(direction.y, 0f, -direction.x) * (Block.Size / 2f);
        return new[] { start + perpendicular, start - perpendicular, end + perpendicular, end - perpendicular };
    }

    private bool WithinBounds(Vector3[] corners)
    {
        foreach (Vector3 corner in corners)
        {
            if (corner.x < minX || corner.x > maxX || corner.z < minZ || corner.z > maxZ) return false;
        }
        return true;
    }

    // 2D (x/z) separating-axis test between two quadrilaterals given as [start+perp, start-perp, end+perp, end-perp].
    private static bool RectanglesOverlap(Vector3[] cornersA, Vector3[] cornersB)
    {
        const float touchEpsilon = 1e-4f; // blocks merely touching at an edge/corner don't count as overlapping

        Vector2[] a = ToXZ(cornersA);
        Vector2[] b = ToXZ(cornersB);

        Vector2[] axes =
        {
            Perpendicular(a[1] - a[0]), Perpendicular(a[2] - a[0]),
            Perpendicular(b[1] - b[0]), Perpendicular(b[2] - b[0])
        };

        foreach (Vector2 axis in axes)
        {
            if (axis.sqrMagnitude < 1e-8f) continue;
            Vector2 normalizedAxis = axis.normalized;
            GetProjection(a, normalizedAxis, out float minA, out float maxA);
            GetProjection(b, normalizedAxis, out float minB, out float maxB);
            if (maxA <= minB + touchEpsilon || maxB <= minA + touchEpsilon) return false; // found a separating axis
        }

        return true;
    }

    private static Vector2[] ToXZ(Vector3[] corners)
    {
        Vector2[] result = new Vector2[corners.Length];
        for (int i = 0; i < corners.Length; i++) result[i] = new Vector2(corners[i].x, corners[i].z);
        return result;
    }

    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x);

    private static void GetProjection(Vector2[] points, Vector2 axis, out float min, out float max)
    {
        min = max = Vector2.Dot(points[0], axis);
        for (int i = 1; i < points.Length; i++)
        {
            float projection = Vector2.Dot(points[i], axis);
            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }
    }
}
