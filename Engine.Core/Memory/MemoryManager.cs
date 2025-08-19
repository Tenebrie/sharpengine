using System.Diagnostics;
using System.Numerics;

namespace Engine.Core.Memory;

public enum MemoryDomain
{
    Rendering,
}

public static class MemoryManager
{
    private static int RoundUpToPow2(int size)
    {
        if (size <= 1) return 1;
        // Next power-of-two for 32-bit positive ints
        return 1 << (32 - BitOperations.LeadingZeroCount((uint)(size - 1)));
    }
    
    private static int GetNearestPowerOfTwo(int size)
    {
        if (size <= 1) return 0;
        if (size >= (1 << 30)) return 30;
        return 32 - BitOperations.LeadingZeroCount((uint)(size - 1));
    }
    
    public class ArrayHandleBucket
    {
        private readonly List<Stack<ArrayHandle>> _freePool = [];
        private readonly List<Stack<ArrayHandle>> _activePool = [];
        
        public ArrayHandleBucket()
        {
            const int maxPower = 30;
            for (var i = 0; i <= maxPower; i++)
            {
                _freePool.Add([]);
                _activePool.Add([]);
            }
        }
        
        public void AddToFreePool(ArrayHandle entry)
        {
            _freePool[entry.NearestPower].Push(entry);
            _freeMask |= 1u << entry.NearestPower;
        }

        private void AddToActivePool(ArrayHandle entry)
        {
            _activePool[entry.NearestPower].Push(entry);
        }
        
        private uint _freeMask;
        public ArrayHandle Find<T>(int minimalSize)
        {
            if (minimalSize <= 1) minimalSize = 1;
            if (minimalSize >= 1 << 30) minimalSize = 1 << 30;
            
            var p = GetNearestPowerOfTwo(minimalSize);

            var mask = _freeMask >> p;
            if (mask == 0)
                return Allocate<T>(minimalSize);
            
            var offset = BitOperations.TrailingZeroCount(mask);
            var idx = p + offset;
            var handle = _freePool[idx].Pop();
            if (_freePool[idx].Count == 0) _freeMask &= ~(1u << idx);
            handle.IsBeingUsed = true;
            handle.AccessedAt = Stopwatch.GetTimestamp();
            AddToActivePool(handle);
            return handle;
        }

        private ArrayHandle Allocate<T>(int size)
        {
            var roundedSize = RoundUpToPow2(size);
            var entry = new ArrayHandle
            {
                Array = new T[roundedSize],
                SizeUsed = 0,
                IsBeingUsed = true,
                AccessedAt = Stopwatch.GetTimestamp(),
                NearestPower = GetNearestPowerOfTwo(roundedSize),
                Bucket = this
            };
            AddToActivePool(entry);
            return entry;
        }

        public void FreeAll()
        {
            foreach (var bucket in _activePool)
            {
                foreach (var arrayHandle in bucket)
                {
                    arrayHandle.MarkAsFree();
                }
                bucket.Clear();
            }
        }

        public void Clean(long currentTime)
        {
            var expireDeadline = currentTime - 60 * Stopwatch.Frequency; // 60 seconds in ticks
            for (var i = 0; i < _freePool.Count; i++)
            {
                var bucket = _freePool[i];
                if (bucket.Count == 0)
                {
                    _freeMask &= ~(1u << i);
                    continue;
                }

                var snapshot = bucket.ToArray();
                bucket.Clear();
                foreach (var entry in snapshot)
                    if (entry.AccessedAt >= expireDeadline)
                        bucket.Push(entry);

                if (bucket.Count == 0) _freeMask &= ~(1u << i);
                else _freeMask |= 1u << i;
            }
        }
    }
    
    public class ArrayHandle
    {
        public required Array Array;
        public required int SizeUsed;
        public required bool IsBeingUsed;
        public required long AccessedAt;
        public required int NearestPower;
        public required ArrayHandleBucket Bucket;
        
        public int Capacity => Array.Length;
        
        public void MarkAsFree()
        {
            if (!IsBeingUsed)
                return;
            SizeUsed = 0;
            IsBeingUsed = false;
            AccessedAt = Stopwatch.GetTimestamp();
            Bucket.AddToFreePool(this);
        }
    }

    private static readonly Dictionary<MemoryDomain, Dictionary<Type, ArrayHandleBucket>> ArrayPool = new();

    public static ArrayHandle ProduceArray<T>(MemoryDomain domain, int minimalSize)
    {
        if (!ArrayPool.TryGetValue(domain, out var sharedArraysForType))
            ArrayPool[domain] = sharedArraysForType = [];
        if (!sharedArraysForType.TryGetValue(typeof(T), out var bucket))
            sharedArraysForType[typeof(T)] = bucket = new ArrayHandleBucket();
        
        var rentedArray = bucket.Find<T>(minimalSize);
        CheckForExpiredArrays();
        return rentedArray;
    }
    
    public static ArrayHandle MergeArrays<T>(MemoryDomain domain, ArrayHandle left, int rightSize, T[] right)
    {
        var totalSize = left.SizeUsed + rightSize;
        if (left.Capacity >= totalSize)
        {
            right.AsSpan(0, rightSize).CopyTo(((T[])left.Array).AsSpan(left.SizeUsed, rightSize));
            left.SizeUsed += rightSize;
            left.AccessedAt = Stopwatch.GetTimestamp();
            return left;
        }
        var result = ProduceArray<T>(domain, totalSize);
        
        ((T[])left.Array).AsSpan(0, left.SizeUsed).CopyTo(((T[])result.Array).AsSpan(0, left.SizeUsed));
        right.AsSpan(0, rightSize).CopyTo(((T[])result.Array).AsSpan(left.SizeUsed, rightSize));
        result.SizeUsed = totalSize;
        left.MarkAsFree();

        return result;
    }

    private static long _expiredArraysCheckedAt = 0;
    private static void CheckForExpiredArrays()
    {
        var time = Stopwatch.GetTimestamp();
        if (time - _expiredArraysCheckedAt < Stopwatch.Frequency * 10) // check every 10 seconds
            return;

        _expiredArraysCheckedAt = time;
        
        foreach (var domain in ArrayPool.Values)
        {
            foreach (var sharedArrays in domain.Values)
            {
                sharedArrays.Clean(time);
            }
        }
    }

    public static void FreeDomain(MemoryDomain domain)
    {
        if (!ArrayPool.TryGetValue(domain, out var sharedBuckets))
            return;

        foreach (var bucket in sharedBuckets.Values)
        {
            bucket.FreeAll();
        }
    }
}