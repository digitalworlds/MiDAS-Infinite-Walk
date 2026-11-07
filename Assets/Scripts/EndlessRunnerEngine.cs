using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.IO.Compression;
using System;
using UnityEngine.AI;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Networking;

/*
 * EndlessRunnerEngine generates a chain of Blocks in a constrained area, and keeps track of the current block the player is on.
 * It requires references to the left and right ActionBasedControllers, and the Camera representing the player's head.
 * It also requires a reference to a TextMeshProUGUI for debugging purposes.
 */
public class EndlessRunnerEngine : MonoBehaviour
{
    public Camera head;

    public TextMeshProUGUI debugText;

    public int minBlocksAhead = 5;//The number of visible blocks ahead of the player.

    public int constrainedBlockCount = 10;//The number of blocks to generate in the beginning, in both directions.

    //Constraints for the area in which blocks can be generated:
    public float areaMinX = -3.2f;
    public float areaMaxX = 3.2f;
    public float areaMinZ = -3.2f;
    public float areaMaxZ = 3.2f;

    public bool showDebugWalls = true;
    public bool showBounds = true;

    public Block CurrentBlock => currentBlock;

    private Transform debugSphere;
    private Block currentBlock;
    private Block lastBlock;
    private Block firstBlock;

    private ConstrainedBlockGenerator generator;

    private LevelCallback callback;

    void Awake()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void Start()
    {
        LevelCallback c = GetComponent<LevelCallback>(); 
        if(c!=null && c.enabled) callback=c;
        

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DebugSphere";
        sphere.SetActive(false);
        sphere.transform.SetParent(transform);
        sphere.transform.localScale = Vector3.one * 0.2f;
        sphere.GetComponent<Renderer>().material.color = Color.red;
        debugSphere = sphere.transform;

        Vector2 direction = Vector2.up;

        //Move the starting position to better occupy the space.
        Vector3 startPos=new Vector3(0,0,0);
        if(Math.Floor((areaMaxX - areaMinX)/1.2) % 2 == 0)
        {
            startPos.x = Block.Size * (0.5f);
        }
       
        if(Math.Floor((areaMaxZ - areaMinZ)/1.2) % 2 == 1)
        {
            startPos.z = Block.Size * (-0.5f);
        }
        Debug.Log($"Start Position: {startPos}");
        string csvLine = $"{areaMaxX-areaMinX},{areaMaxZ-areaMinZ}";
        System.IO.File.AppendAllText("block_counts.csv", csvLine + "\n");
        
        generator = new ConstrainedBlockGenerator(areaMinX, areaMaxX, areaMinZ, areaMaxZ, minBlocksAhead, showBounds, transform);
        Block startBlock = new Block(startPos, direction, BlockType.Straight, showDebugWalls, transform);
        for(int i=0;i<1;i++){

            startBlock = new Block(startPos, direction, BlockType.Straight, showDebugWalls, transform);
            

        lastBlock = generator.GenerateForward(startBlock, constrainedBlockCount);
        firstBlock = generator.GenerateBackward(startBlock, constrainedBlockCount);

        // count the number of left, right, and straight blocks generated
        int leftCount = 0;
        int rightCount = 0;
        int straightCount = 0;
        int clockwiseCount = 0;
        int counterClockwiseCount = 0;
        int directionSwitches = 0;
        int zigzagCount = 0;

        int dir = 0;
        int lastTurn = 0;

        Block b = firstBlock;
        while (b != null)
        {
            switch (b.Type)
            {
                case BlockType.TurnLeft:
                    leftCount++;
                    if (lastTurn == -1)
                    {
                        if(dir == 1)
                            directionSwitches++;
                        dir=-1;
                    }
                    else if(lastTurn == 1)
                        zigzagCount++;
                    lastTurn = -1;
                    break;
                case BlockType.TurnRight:
                    rightCount++;
                    if (lastTurn == 1)
                    {
                        if(dir == -1)
                            directionSwitches++;
                        dir=1;
                    }
                    else if(lastTurn == -1)
                        zigzagCount++;
                    lastTurn = 1;
                    break;
                case BlockType.Straight:
                    straightCount++;
                    break;
            }
            if(dir == 1)
                clockwiseCount++;
            else if(dir == -1)
                counterClockwiseCount++;
            b = b.Next;
        }
        //print into a CSV file the counts of each block type as numbers and percentages
        csvLine = $"{leftCount},{rightCount},{straightCount},{clockwiseCount},{counterClockwiseCount},{directionSwitches},{zigzagCount}";
        System.IO.File.AppendAllText("block_counts.csv", csvLine + "\n");
        
        Debug.Log($"Left blocks: {leftCount}, Right blocks: {rightCount}, Straight blocks: {straightCount}");
        int totalCount = leftCount + rightCount + straightCount;
        Debug.Log($"Left blocks: {(float)leftCount / totalCount:P2}, Right blocks: {(float)rightCount / totalCount:P2}, Straight blocks: {(float)straightCount / totalCount:P2}");
        Debug.Log($"Clockwise: {clockwiseCount}, Counter-clockwise: {counterClockwiseCount}");
        int totalTurns = clockwiseCount + counterClockwiseCount;
        Debug.Log($"Clockwise: {(float)clockwiseCount / totalTurns:P2}, Counter-clockwise: {(float)counterClockwiseCount / totalTurns:P2}");
        Debug.Log($"Direction switches: {directionSwitches} , Zigzag count: {zigzagCount}");
        }

        Debug.Log("Blocks generated");

        currentBlock = startBlock;
        currentBlock.SetHighlighted(true);

        Block block=currentBlock;
        block.show(callback);
        for(int i=0;i<minBlocksAhead;i++)
        {
            block=block.Next;
            if(block!=null) block.show(callback);
            else break;
        }
        block=currentBlock;
        for(int i=0;i<minBlocksAhead;i++)
        {
            block=block.Previous;
            if(block!=null) block.show(callback);
            else break;
        }

    }


    void Update()
    {
        if (head == null)
            return;

        //Get our current position.
        Vector2 v = new Vector2(head.transform.localPosition.x - transform.localPosition.x, head.transform.localPosition.z - transform.localPosition.z);
        debugSphere.localPosition = new Vector3(v.x, 0f, v.y);

        if (currentBlock != null)
        {
            Vector3 point = new Vector3(v.x, 0f, v.y);
            Vector3 forward = new Vector3(currentBlock.Direction.x, 0f, currentBlock.Direction.y);
            Vector3 next_forward = new Vector3(currentBlock.Next.Direction.x, 0f, currentBlock.Next.Direction.y);

            //Check if we have moved past the start or end of the current block
            bool pastStart = Vector3.Dot(point - currentBlock.StartPosition, forward) >= 0f;
            bool beforeEnd = currentBlock.Next == null || Vector3.Dot(point - currentBlock.Next.StartPosition, next_forward) < 0f;

            //If we have moved past the start of the current block, move to the previous block.
            if (!pastStart && currentBlock.Previous != null)
            {
                currentBlock.SetHighlighted(false);
                currentBlock = currentBlock.Previous;
                currentBlock.SetHighlighted(true);
                EnsureBlocksBehind(currentBlock);
                UpdateActiveWindow(movedForward: false);
                if(debugText!=null)debugText.text = ""+currentBlock.Id;
                Debug.Log("Went back to block "+currentBlock.Id+ " "+currentBlock.Type);
            }
            //If we have moved past the end of the current block, move to the next block. 
            else if (!beforeEnd && currentBlock.Next != null)
            {
                currentBlock.SetHighlighted(false);
                currentBlock = currentBlock.Next;
                currentBlock.SetHighlighted(true);
                EnsureBlocksAhead(currentBlock);
                UpdateActiveWindow(movedForward: true);
                if(debugText!=null)debugText.text = ""+currentBlock.Id;
                Debug.Log("Went forward to block "+currentBlock.Id+ " "+currentBlock.Type);
            }
        }

        //This script sets the center of the wallking area to the position of the left controller when the select button is pressed
      /*  if (leftController.selectAction.action.WasPressedThisFrame())
        {
            Vector3 pos = transform.localPosition;
            pos.x = leftController.transform.localPosition.x;
            pos.z = leftController.transform.localPosition.z;
            transform.localPosition = pos;
        }

        if(rightController.selectAction.action.WasPressedThisFrame())
        {
            //rotate the trnsofrm around the player's head so that forward direction is aligned with the right controller's forward direction
            Vector3 headForward = head.transform.forward;
            Vector3 rightForward = rightController.transform.forward;
            headForward.y = 0f;
            rightForward.y = 0f;
            headForward.Normalize();
            rightForward.Normalize();
            float angle = Vector3.SignedAngle(headForward, rightForward, Vector3.up);
            transform.RotateAround(head.transform.position, Vector3.up, angle); 

        }*/

    }

    // Extends the chain from lastBlock until there are at least minBlocksAhead blocks past "from".
    private void EnsureBlocksAhead(Block from)
    {
        int count = 0;
        Block b = from;
        Block origin = b;
        while (b.Next != null && count < minBlocksAhead+4)
        {
            b = b.Next;
            if (count < minBlocksAhead)
                origin = b;
            count++;
        }
        origin.DetachNext();
        lastBlock = generator.GenerateForward(origin, 4);
    }

    // Picks a random type for a block adjacent to one of type "neighborType". Two consecutive blocks may
    // never both be the same turn (straight can follow, or be followed by, anything).
    private static BlockType RandomBlockType(BlockType neighborType)
    {
        BlockType[] types = neighborType switch
        {
            BlockType.TurnLeft => new[] { BlockType.Straight, BlockType.TurnRight },
            BlockType.TurnRight => new[] { BlockType.Straight, BlockType.TurnLeft },
            _ => new[] { BlockType.Straight, BlockType.TurnLeft, BlockType.TurnRight }
        };
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    // Extends the chain backward from FirstBlock until there are at least minBlocksAhead blocks before "from".
    private void EnsureBlocksBehind(Block from)
    {
        int count = 0;
        Block b = from;
        Block origin = b;
        while (b.Previous != null && count < minBlocksAhead+4)
        {
            b = b.Previous;
            if (count < minBlocksAhead)
                origin = b;
            count++;
        }

        origin.DetachPrevious();
        firstBlock = generator.GenerateBackward(origin, 4);
    }

    // Keeps only minBlocksAhead blocks active on each side of currentBlock: activates the newly needed edge block
    // and deactivates the one that just fell outside the window on the opposite side.
    private void UpdateActiveWindow(bool movedForward)
    {
        Block toActivate = Advance(currentBlock, minBlocksAhead, movedForward);
        if (toActivate != null) toActivate.show(callback);

        Block toDeactivate = Advance(currentBlock, minBlocksAhead + 1, !movedForward);
        if (toDeactivate != null) toDeactivate.hide(callback);
    }

    private Block Advance(Block block, int steps, bool forward)
    {
        for (int i = 0; i < steps && block != null; i++)
        {
            block = forward ? block.Next : block.Previous;
        }
        return block;
    }

}

