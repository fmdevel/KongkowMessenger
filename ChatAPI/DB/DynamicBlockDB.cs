using System;
using System.IO;
using System.Collections.Generic;

public abstract class DynamicBlockDB<T> : FileStream
{
    protected List<T> m_blocks;
    protected List<int> m_blocksSize;

    protected abstract T TransformBlock(byte[] block, int index, int count);
    protected abstract byte[] TransformBlock(T value);

    public DynamicBlockDB(string fileName)
        : base(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
    {
        m_blocks = new List<T>();
        m_blocksSize = new List<int>();
    }

    protected unsafe void LoadBlocks()
    {
        var block = new byte[8 * 1024]; // 8KB initial block
        while (true)
        {
            if (!ReadBlock(block, 0, 4)) // read 4bytes blockSize
                return; // block corrupted

            int blockSize;
            fixed (byte* pBlock = block)
                blockSize = *(int*)(pBlock);

            if (blockSize < 4) // minimum is 4
                return; // block corrupted

            if (block.Length < blockSize) // block size enough?
                block = new byte[Math.Max(blockSize, block.Length * 2)]; // try to alloc twice size of block

            if (!ReadBlock(block, 0, blockSize))
                return; // block corrupted

            int actualSize;
            fixed (byte* pBlock = block)
                actualSize = *(int*)(pBlock);

            if (actualSize < 0 || actualSize + 4 > blockSize)
                return; // block corrupted

            var item = TransformBlock(block, 4, actualSize);

            if (item != null)
            {
                m_blocks.Add(TransformBlock(block, 4, actualSize));
                m_blocksSize.Add(blockSize);
            }
        }
    }

    protected bool ReadBlock(byte[] block, int offset, int count)
    {
        do
        {
            int readCount = base.Read(block, offset, count);
            if (readCount <= 0)
                return false; // End of stream detected, block corrupted!!

            offset += readCount;
            count -= readCount;
        } while (count > 0);
        return true;
    }

    private long CalculateFilePos(int index)
    {
        long pos = 0;
        while (--index >= 0)
            pos += (m_blocksSize[index] + 4);

        return pos;
    }

    private void MoveBlocks(long position, int distance) // PAINFUL SLOW OPERATION
    {
        if (distance != 0)
        {
            base.Position = position;
            var movedBlock = new byte[(int)(base.Length - position)]; // BE CAREFUL, THIS CAN TRIGGER OUT OF MEMORY
            ReadBlock(movedBlock, 0, movedBlock.Length);
            base.Position = position + distance;
            base.Write(movedBlock, 0, movedBlock.Length);

            if (distance < 0) // Shrink file needed?
                base.SetLength(position + distance + movedBlock.Length);

            base.Flush();
        }
    }

    public int Count
    {
        get
        {
            return m_blocks.Count;
        }
    }

    public virtual void RemoveAt(int index)
    {
        if (index < 0 || index >= m_blocks.Count)
            throw new ArgumentException();

        if (index < m_blocks.Count - 1) // index is last block?
            MoveBlocks(CalculateFilePos(index + 1), -(m_blocksSize[index] + 4));
        else
            base.SetLength(CalculateFilePos(index)); // just set length to remove last index

        m_blocks.RemoveAt(index);
        m_blocksSize.RemoveAt(index);
    }

    public void Add(T value)
    {
        var data = TransformBlock(value);
        int actualSize = data.Length;
        m_blocks.Add(value); // Add block into table
        m_blocksSize.Add(actualSize + 4); // Add block size into table
        WriteBlock(m_blocks.Count - 1, data, actualSize + 4, actualSize);
    }

    public void UpdateOrAdd(int index, T value)
    {
        if (index >= 0 && index < this.Count)
            this[index] = value;
        else
            Add(value);
    }

    public virtual T this[int index]
    {
        get
        {
            return m_blocks[index];
        }
        set
        {
            var blockSize = m_blocksSize[index];
            var data = TransformBlock(value);
            int actualSize = data.Length;
            if ((actualSize + 4) > blockSize) // is existing block enough?
            {
                if (index < m_blocks.Count - 1) // Last block does not need expand
                    MoveBlocks(CalculateFilePos(index + 1), (actualSize + 4) - blockSize);

                blockSize = (actualSize + 4); // Update block size
                m_blocksSize[index] = blockSize; // Update block size table
            }
            m_blocks[index] = value; // Update block table
            WriteBlock(index, data, blockSize, actualSize);
        }
    }

    private unsafe void WriteBlock(int index, byte[] data, int blockSize, int actualSize)
    {
        base.Position = CalculateFilePos(index); // Move file position
        var block = new byte[blockSize + 4]; // 4bytes blockSize header
        fixed (byte* pBlock = block)
        {
            *(int*)(pBlock) = blockSize; // write blockSize at first 4bytes block
            *(int*)(pBlock + 4) = actualSize; // write actualSize at next 4bytes block
        }
        Array.Copy(data, 0, block, 8, data.Length);
        base.Write(block, 0, block.Length); // Write block to file
        base.Flush();
    }

    public virtual void Clear()
    {
        if (m_blocks.Count > 0)
        {
            base.SetLength(0);
            m_blocks.Clear();
            m_blocksSize.Clear();
        }
    }

    public bool Remove(T item)
    {
        var index = m_blocks.IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public int IndexOf(T item)
    {
        return m_blocks.IndexOf(item);
    }

    public bool Contains(T item)
    {
        return m_blocks.Contains(item);
    }

    internal List<T> GetList()
    {
        return m_blocks;
    }

}