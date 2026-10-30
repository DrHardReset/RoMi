namespace RoMi.Tests;

[TestFixture]
public class SysExRangePlannerTests
{
    private const int MaxGap = 16;
    private const int MaxRange = 128;

    private static SysExReadItem<string> Item(string tag, int address, int size = 1) => new(tag, address, size);

    [Test]
    public void Plan_WithNoItems_ShouldReturnEmptyList()
    {
        var ranges = SysExRangePlanner.Plan(Array.Empty<SysExReadItem<string>>(), MaxGap, MaxRange);
        Assert.That(ranges, Is.Empty);
    }

    [Test]
    public void Plan_WithSingleItem_ShouldReturnOneRangeWithOffsetZero()
    {
        var ranges = SysExRangePlanner.Plan([Item("a", 0x100, 3)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].StartAddress, Is.EqualTo(0x100));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(3));
        Assert.That(ranges[0].Members, Has.Count.EqualTo(1));
        Assert.That(ranges[0].Members[0].Tag, Is.EqualTo("a"));
        Assert.That(ranges[0].Members[0].Offset, Is.EqualTo(0));
        Assert.That(ranges[0].Members[0].Size, Is.EqualTo(3));
    }

    [Test]
    public void Plan_WithAdjacentItems_ShouldMergeIntoOneRange()
    {
        var ranges = SysExRangePlanner.Plan([Item("a", 0, 2), Item("b", 2, 4), Item("c", 6, 1)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].StartAddress, Is.EqualTo(0));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(7));
        Assert.That(ranges[0].Members.Select(m => (m.Tag, m.Offset, m.Size)),
            Is.EqualTo([("a", 0, 2), ("b", 2, 4), ("c", 6, 1)]));
    }

    [Test]
    public void Plan_WithSingleReservedByteBetweenItems_ShouldReadOverGapAndKeepOffsets()
    {
        // address 1 is "reserved" and not part of any item

        var ranges = SysExRangePlanner.Plan([Item("a", 0), Item("b", 2)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(3));
        Assert.That(ranges[0].Members.Select(m => m.Offset), Is.EqualTo(new[] { 0, 2 }));
    }

    [Test]
    [TestCase(16, 1)] // gap == threshold -> merged
    [TestCase(17, 2)] // gap >  threshold -> split
    public void Plan_WithGapAroundThreshold_ShouldMergeOrSplit(int gap, int expectedRangeCount)
    {
        // item a occupies address 0, the gap follows directly behind it
        int addressOfB = 1 + gap;

        var ranges = SysExRangePlanner.Plan([Item("a", 0), Item("b", addressOfB)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(expectedRangeCount));
    }

    [Test]
    public void Plan_WithGapAboveThreshold_ShouldCreateSeparateRangesWithCorrectStartAddresses()
    {
        var ranges = SysExRangePlanner.Plan([Item("a", 0x10, 2), Item("b", 0x1000, 4)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(2));
        Assert.That(ranges[0].StartAddress, Is.EqualTo(0x10));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(2));
        Assert.That(ranges[1].StartAddress, Is.EqualTo(0x1000));
        Assert.That(ranges[1].TotalSize, Is.EqualTo(4));
        Assert.That(ranges[1].Members[0].Offset, Is.EqualTo(0));
    }

    [Test]
    public void Plan_WithZeroMaxGap_ShouldOnlyMergeAdjacentItems()
    {
        var ranges = SysExRangePlanner.Plan([Item("a", 0), Item("b", 1), Item("c", 3)], maxGap: 0, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(2));
        Assert.That(ranges[0].Members.Select(m => m.Tag), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(ranges[1].Members.Select(m => m.Tag), Is.EqualTo(new[] { "c" }));
    }

    [Test]
    public void Plan_WithUnsortedItems_ShouldSortByAddress()
    {
        var ranges = SysExRangePlanner.Plan([Item("c", 2), Item("a", 0), Item("b", 1)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].StartAddress, Is.EqualTo(0));
        Assert.That(ranges[0].Members.Select(m => m.Tag), Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(ranges[0].Members.Select(m => m.Offset), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    [TestCase(128, 1)] // exactly the maximum -> merged
    [TestCase(127, 1)]
    [TestCase(129, 2)] // one byte too much -> split
    public void Plan_WithMaxRangeSize_ShouldSplitWhenExceeded(int totalSize, int expectedRangeCount)
    {
        // Arrange: first item is 1 byte, second item fills up the rest directly behind it
        var items = new[] { Item("a", 0), Item("b", 1, totalSize - 1) };

        // Act
        var ranges = SysExRangePlanner.Plan(items, MaxGap, maxRangeSize: 128);

        // Assert
        Assert.That(ranges, Has.Count.EqualTo(expectedRangeCount));
    }

    [Test]
    public void Plan_WithManyAdjacentItems_ShouldSplitIntoRangesNotExceedingMaxSize()
    {
        // Arrange: 10 adjacent items with 4 bytes each, max range size = 16 bytes
        var items = Enumerable.Range(0, 10).Select(i => Item($"i{i}", i * 4, 4)).ToList();

        // Act
        var ranges = SysExRangePlanner.Plan(items, MaxGap, maxRangeSize: 16);

        // Assert
        Assert.That(ranges, Has.Count.EqualTo(3)); // 4 + 4 + 2 items
        Assert.That(ranges.All(r => r.TotalSize <= 16), Is.True);
        Assert.That(ranges.Sum(r => r.Members.Count), Is.EqualTo(10));
        Assert.That(ranges.Select(r => r.StartAddress), Is.EqualTo(new[] { 0, 16, 32 }));
    }

    [Test]
    public void Plan_WithSingleItemLargerThanMaxRangeSize_ShouldStillReturnItAsOwnRange()
    {
        // Act
        var ranges = SysExRangePlanner.Plan([Item("big", 0, 200), Item("next", 200)], MaxGap, maxRangeSize: 128);

        // Assert
        Assert.That(ranges, Has.Count.EqualTo(2));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(200));
        Assert.That(ranges[0].Members.Select(m => m.Tag), Is.EqualTo(new[] { "big" }));
        Assert.That(ranges[1].StartAddress, Is.EqualTo(200));
    }

    [Test]
    public void Plan_WithOverlappingItems_ShouldNotShrinkRange()
    {
        // b lies completely inside a

        var ranges = SysExRangePlanner.Plan([Item("a", 0, 10), Item("b", 2, 2), Item("c", 10)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(11));
        Assert.That(ranges[0].Members.Select(m => (m.Tag, m.Offset)),
            Is.EqualTo(new[] { ("a", 0), ("b", 2), ("c", 10) }));
    }

    [Test]
    public void Plan_WithDuplicateAddresses_ShouldMergeBothIntoSameRange()
    {
        var ranges = SysExRangePlanner.Plan([Item("a", 5, 2), Item("b", 5, 2)], MaxGap, MaxRange);

        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(2));
        Assert.That(ranges[0].Members.Select(m => m.Offset), Is.EqualTo(new[] { 0, 0 }));
    }

    [Test]
    public void Plan_WithSevenBitAddresses_ShouldTreatByteBoundaryAsAdjacent()
    {
        // 00 00 00 7F and 00 00 01 00 are neighbors in 7-bit address arithmetic (127 and 128)
        int a = new StartAddress("00 00 00 7F").ToIntegerRepresentation();
        int b = new StartAddress("00 00 01 00").ToIntegerRepresentation();

        var ranges = SysExRangePlanner.Plan([Item("a", a), Item("b", b)], maxGap: 0, MaxRange);

        Assert.That(b - a, Is.EqualTo(1));
        Assert.That(ranges, Has.Count.EqualTo(1));
        Assert.That(ranges[0].TotalSize, Is.EqualTo(2));
    }

    [Test]
    public void Plan_ShouldNotModifyInputOrder()
    {
        var items = new List<SysExReadItem<string>> { Item("b", 10), Item("a", 0) };

        _ = SysExRangePlanner.Plan(items, MaxGap, MaxRange);

        Assert.That(items.Select(i => i.Tag), Is.EqualTo(new[] { "b", "a" }));
    }
}
