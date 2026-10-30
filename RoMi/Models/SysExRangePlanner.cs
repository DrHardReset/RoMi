namespace RoMi.Models;

public readonly record struct SysExReadItem<T>(T Tag, int Address, int Size);

public readonly record struct SysExRangeMember<T>(T Tag, int Offset, int Size);

public sealed record SysExReadRange<T>(int StartAddress, int TotalSize, IReadOnlyList<SysExRangeMember<T>> Members);

/// <summary>
/// Groups single parameters (absolute 7-bit address + size) into as few contiguous RQ1 ranges as possible.
/// Gaps (e.g. reserved addresses) up to <c>maxGap</c> bytes are read over and discarded afterwards.
/// </summary>
public static class SysExRangePlanner
{
    public static List<SysExReadRange<T>> Plan<T>(IEnumerable<SysExReadItem<T>> items, int maxGap, int maxRangeSize)
    {
        List<SysExReadRange<T>> result = [];
        List<SysExRangeMember<T>>? members = null;
        int start = 0;
        int end = 0;

        foreach (SysExReadItem<T> item in items.OrderBy(x => x.Address))
        {
            int itemEnd = item.Address + item.Size;

            if (members != null)
            {
                int gap = item.Address - end; // negative or 0 = overlapping / adjacent
                int newEnd = Math.Max(end, itemEnd);

                if (gap <= maxGap && newEnd - start <= maxRangeSize)
                {
                    members.Add(new SysExRangeMember<T>(item.Tag, item.Address - start, item.Size));
                    end = newEnd;
                    continue;
                }

                result.Add(new SysExReadRange<T>(start, end - start, members));
            }

            start = item.Address;
            end = itemEnd;
            members = [new SysExRangeMember<T>(item.Tag, 0, item.Size)];
        }

        if (members != null)
        {
            result.Add(new SysExReadRange<T>(start, end - start, members));
        }

        return result;
    }
}
