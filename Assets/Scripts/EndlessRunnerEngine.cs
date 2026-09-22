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

    public int constrainedBlockCount = 50;//The number of blocks to generate in the beginning, in both directions.

    //Constraints for the area in which blocks can be generated:
    public float areaMinX = -5f;
    public float areaMaxX = 5f;
    public float areaMinZ = -5f;
    public float areaMaxZ = 5f;

    public Block CurrentBlock => currentBlock;

    private Transform debugSphere;
    private Block currentBlock;
    private Block lastBlock;
    private Block firstBlock;

    void Awake()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void Start()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DebugSphere";
        sphere.transform.SetParent(transform);
        sphere.transform.localScale = Vector3.one * 0.2f;
        sphere.GetComponent<Renderer>().material.color = Color.red;
        debugSphere = sphere.transform;

        Vector2 direction = Vector2.up;
        Block startBlock = new Block(new Vector3(direction.x,0,direction.y)*Block.Size*(-0.5f), direction, BlockType.Straight, transform);

        ConstrainedBlockGenerator generator = new ConstrainedBlockGenerator(areaMinX, areaMaxX, areaMinZ, areaMaxZ, minBlocksAhead, transform);

        lastBlock = generator.GenerateForward(startBlock, constrainedBlockCount);
        firstBlock = generator.GenerateBackward(startBlock, constrainedBlockCount);

        currentBlock = startBlock;
        currentBlock.SetHighlighted(true);

        Block block=currentBlock;
        block.GameObject.SetActive(true);
        for(int i=0;i<minBlocksAhead;i++)
        {
            block=block.Next;
            if(block!=null) block.GameObject.SetActive(true);
            else break;
        }
        block=currentBlock;
        for(int i=0;i<minBlocksAhead;i++)
        {
            block=block.Previous;
            if(block!=null) block.GameObject.SetActive(true);
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
        while (b.Next != null && count < minBlocksAhead)
        {
            b = b.Next;
            count++;
        }

        while (count < minBlocksAhead)
        {
            lastBlock = new Block(lastBlock, RandomBlockType(lastBlock.Type), transform);
            count++;
        }
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
        while (b.Previous != null && count < minBlocksAhead)
        {
            b = b.Previous;
            count++;
        }

        while (count < minBlocksAhead)
        {
            firstBlock = Block.Prepend(firstBlock, RandomBlockType(firstBlock.Type), transform);
            count++;
        }
    }

    // Keeps only minBlocksAhead blocks active on each side of currentBlock: activates the newly needed edge block
    // and deactivates the one that just fell outside the window on the opposite side.
    private void UpdateActiveWindow(bool movedForward)
    {
        Block toActivate = Advance(currentBlock, minBlocksAhead, movedForward);
        if (toActivate != null) toActivate.GameObject.SetActive(true);

        Block toDeactivate = Advance(currentBlock, minBlocksAhead + 1, !movedForward);
        if (toDeactivate != null) toDeactivate.GameObject.SetActive(false);
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

